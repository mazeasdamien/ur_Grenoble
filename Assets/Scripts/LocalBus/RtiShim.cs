// Local replacement for the subset of RTI Connext DDS used by the 2023 scripts.
// The RTI research licence expired on 21 June 2023 and the native library exits at start-up,
// so this file keeps the same type and method names and carries the samples over TCP on
// 127.0.0.1 instead. One port per topic: the reader listens, writers connect and send.
// The 2023 scripts that publish and subscribe are left unchanged.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Rti.Types.Dynamic
{
    public enum MemberKind { Bool, Byte, Int, Float, Double, ByteSequence }

    public class DynamicType
    {
        public string Name { get; internal set; } = "";
        public MemberKind Kind { get; internal set; }
        public readonly List<StructMember> Members = new List<StructMember>();
    }

    public class StructType : DynamicType { }

    public class StructMember
    {
        public string Name { get; }
        public DynamicType Type { get; }
        public StructMember(string name, DynamicType type) { Name = name; Type = type; }
    }

    public class StructTypeBuilder
    {
        private readonly StructType type = new StructType();
        public StructTypeBuilder WithName(string name) { type.Name = name; return this; }
        public StructTypeBuilder AddMember(StructMember member) { type.Members.Add(member); return this; }
        public StructType Create() => type;
    }

    public class DynamicTypeFactory
    {
        public static DynamicTypeFactory Instance { get; } = new DynamicTypeFactory();

        public StructTypeBuilder BuildStruct() => new StructTypeBuilder();

        public DynamicType GetPrimitiveType<T>()
        {
            var t = typeof(T);
            MemberKind k;
            if (t == typeof(bool)) k = MemberKind.Bool;
            else if (t == typeof(byte)) k = MemberKind.Byte;
            else if (t == typeof(int)) k = MemberKind.Int;
            else if (t == typeof(float)) k = MemberKind.Float;
            else if (t == typeof(double)) k = MemberKind.Double;
            else throw new NotSupportedException("LocalBus: primitive type " + t.Name);
            return new DynamicType { Name = t.Name, Kind = k };
        }

        public DynamicType CreateSequence(DynamicType element, int maxLength)
        {
            if (element.Kind != MemberKind.Byte) throw new NotSupportedException("LocalBus: only byte sequences");
            return new DynamicType { Name = "sequence<byte>", Kind = MemberKind.ByteSequence };
        }
    }

    public class DynamicData
    {
        public DynamicType Type { get; }
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public DynamicData(DynamicType type) { Type = type; }

        private StructMember Member(string name)
        {
            foreach (var m in Type.Members) if (m.Name == name) return m;
            throw new KeyNotFoundException("LocalBus: no member '" + name + "' in " + Type.Name);
        }

        public void SetValue<T>(string name, T value)
        {
            var m = Member(name);
            values[name] = m.Type.Kind == MemberKind.ByteSequence ? (object)(value as byte[] ?? new byte[0]) : Coerce(m.Type.Kind, value);
        }

        public T GetValue<T>(string name)
        {
            var m = Member(name);
            if (!values.TryGetValue(name, out var v)) v = Default(m.Type.Kind);
            if (v is T same) return same;
            return (T)Convert.ChangeType(v, typeof(T));
        }

        private static object Default(MemberKind k)
        {
            switch (k)
            {
                case MemberKind.Bool: return false;
                case MemberKind.Byte: return (byte)0;
                case MemberKind.Int: return 0;
                case MemberKind.Float: return 0f;
                case MemberKind.Double: return 0.0;
                default: return new byte[0];
            }
        }

        private static object Coerce(MemberKind k, object v)
        {
            switch (k)
            {
                case MemberKind.Bool: return Convert.ToBoolean(v);
                case MemberKind.Byte: return Convert.ToByte(v);
                case MemberKind.Int: return Convert.ToInt32(v);
                case MemberKind.Float: return Convert.ToSingle(v);
                case MemberKind.Double: return Convert.ToDouble(v);
                default: return v;
            }
        }

        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                foreach (var m in Type.Members)
                {
                    if (!values.TryGetValue(m.Name, out var v)) v = Default(m.Type.Kind);
                    switch (m.Type.Kind)
                    {
                        case MemberKind.Bool: w.Write((bool)v); break;
                        case MemberKind.Byte: w.Write((byte)v); break;
                        case MemberKind.Int: w.Write((int)v); break;
                        case MemberKind.Float: w.Write((float)v); break;
                        case MemberKind.Double: w.Write((double)v); break;
                        default: var b = (byte[])v; w.Write(b.Length); w.Write(b); break;
                    }
                }
                return ms.ToArray();
            }
        }

        public static DynamicData Deserialize(DynamicType type, byte[] payload)
        {
            var d = new DynamicData(type);
            using (var r = new BinaryReader(new MemoryStream(payload)))
            {
                foreach (var m in type.Members)
                {
                    switch (m.Type.Kind)
                    {
                        case MemberKind.Bool: d.values[m.Name] = r.ReadBoolean(); break;
                        case MemberKind.Byte: d.values[m.Name] = r.ReadByte(); break;
                        case MemberKind.Int: d.values[m.Name] = r.ReadInt32(); break;
                        case MemberKind.Float: d.values[m.Name] = r.ReadSingle(); break;
                        case MemberKind.Double: d.values[m.Name] = r.ReadDouble(); break;
                        default: d.values[m.Name] = r.ReadBytes(r.ReadInt32()); break;
                    }
                }
            }
            return d;
        }
    }
}

namespace Rti.Dds.Core { public sealed class LocalBusCore { } }
namespace Rti.Dds.Domain { public sealed class LocalBusDomain { } }
namespace Rti.Dds.Topics { public sealed class LocalBusTopics { } }

namespace Rti.Dds.Publication
{
    using Rti.Types.Dynamic;

    /// Write() only serializes and queues the sample; a background thread connects and sends,
    /// so a topic nobody listens to never stalls the Unity main thread.
    public class DataWriter<T> where T : DynamicData
    {
        private const int MaxQueue = 8; // keep the most recent samples only
        private readonly string topic;
        private readonly int port;
        private readonly ConcurrentQueue<byte[]> pending = new ConcurrentQueue<byte[]>();
        private readonly AutoResetEvent signal = new AutoResetEvent(false);

        public DataWriter(string topic)
        {
            this.topic = topic;
            port = LocalBus.PortFor(topic);
            var t = new Thread(SendLoop) { IsBackground = true, Name = "LocalBus writer " + topic };
            t.Start();
        }

        public void Write(T sample)
        {
            pending.Enqueue(sample.Serialize());
            while (pending.Count > MaxQueue && pending.TryDequeue(out _)) { }
            signal.Set();
        }

        private void SendLoop()
        {
            TcpClient client = null;
            NetworkStream stream = null;
            while (!LocalBus.Stopping)
            {
                signal.WaitOne(500);
                if (pending.IsEmpty) continue;
                if (stream == null)
                {
                    try
                    {
                        client = new TcpClient { NoDelay = true };
                        client.Connect(IPAddress.Loopback, port);
                        stream = client.GetStream();
                    }
                    catch (Exception)
                    {
                        client?.Close(); client = null; stream = null;
                        while (pending.TryDequeue(out _)) { } // nobody reads this topic yet: drop
                        Thread.Sleep(1000);
                        continue;
                    }
                }
                try
                {
                    while (pending.TryDequeue(out var payload))
                    {
                        var header = BitConverter.GetBytes(payload.Length);
                        stream.Write(header, 0, 4);
                        stream.Write(payload, 0, payload.Length);
                    }
                }
                catch (Exception)
                {
                    client?.Close(); client = null; stream = null;
                }
            }
            client?.Close();
        }

        public override string ToString() => "LocalBus writer " + topic + " :" + port;
    }
}

namespace Rti.Dds.Subscription
{
    using Rti.Types.Dynamic;

    public interface AnyDataReader { }

    public class SampleInfo { public bool ValidData { get; internal set; } = true; }

    public class LoanedSample<T>
    {
        public T Data { get; internal set; }
        public SampleInfo Info { get; } = new SampleInfo();
    }

    public sealed class LoanedSamples<T> : IEnumerable<LoanedSample<T>>, IDisposable
    {
        private readonly List<LoanedSample<T>> items;
        internal LoanedSamples(List<LoanedSample<T>> items) { this.items = items; }
        public int Count => items.Count;
        public IEnumerator<LoanedSample<T>> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        public void Dispose() { }
    }

    public class DataReader<T> : AnyDataReader where T : DynamicData
    {
        private readonly DynamicType type;
        private readonly ConcurrentQueue<byte[]> queue;
        private readonly string topic;
        private long received;

        public DataReader(string topic, DynamicType type)
        {
            this.type = type;
            this.topic = topic;
            queue = LocalBus.Listen(topic);
        }

        private void Trace()
        {
            received++;
            if (received == 1 || received == 1000)
            {
#if UNITY_5_3_OR_NEWER
                UnityEngine.Debug.Log($"LocalBus: {received} sample(s) received on '{topic}'");
#else
                Console.WriteLine($"LocalBus: {received} sample(s) received on '{topic}'");
#endif
            }
        }

        public LoanedSamples<T> Take()
        {
            var list = new List<LoanedSample<T>>();
            while (queue.TryDequeue(out var payload))
            {
                try { list.Add(new LoanedSample<T> { Data = (T)DynamicData.Deserialize(type, payload) }); Trace(); }
                catch (Exception) { /* malformed sample: dropped */ }
            }
            return new LoanedSamples<T>(list);
        }
    }
}

namespace Rti.Dds
{
    /// One TCP listener per topic on 127.0.0.1; keeps at most 64 pending samples (oldest dropped).
    public static class LocalBus
    {
        public const int BasePort = 47000;
        private const int MaxQueue = 64;
        private static readonly Dictionary<string, ConcurrentQueue<byte[]>> queues = new Dictionary<string, ConcurrentQueue<byte[]>>();
        private static readonly List<TcpListener> listeners = new List<TcpListener>();

        public static volatile bool Stopping;

        public static int PortFor(string topic)
        {
            uint h = 2166136261;
            foreach (var c in Encoding.UTF8.GetBytes(topic)) { h ^= c; h *= 16777619; }
            return BasePort + (int)(h % 1000);
        }

        public static ConcurrentQueue<byte[]> Listen(string topic)
        {
            lock (queues)
            {
                if (queues.TryGetValue(topic, out var existing)) return existing;
                var q = new ConcurrentQueue<byte[]>();
                queues[topic] = q;
                var listener = new TcpListener(IPAddress.Loopback, PortFor(topic));
                listener.Start();
                listeners.Add(listener);
                var t = new Thread(() => AcceptLoop(listener, q)) { IsBackground = true, Name = "LocalBus " + topic };
                t.Start();
                return q;
            }
        }

        private static void AcceptLoop(TcpListener listener, ConcurrentQueue<byte[]> q)
        {
            while (true)
            {
                TcpClient c;
                try { c = listener.AcceptTcpClient(); } catch (Exception) { return; }
                var t = new Thread(() => ReadLoop(c, q)) { IsBackground = true };
                t.Start();
            }
        }

        private static void ReadLoop(TcpClient c, ConcurrentQueue<byte[]> q)
        {
            try
            {
                using (c)
                using (var s = c.GetStream())
                {
                    var header = new byte[4];
                    while (ReadExactly(s, header, 4))
                    {
                        int n = BitConverter.ToInt32(header, 0);
                        if (n < 0 || n > 64 * 1024 * 1024) return;
                        var payload = new byte[n];
                        if (!ReadExactly(s, payload, n)) return;
                        q.Enqueue(payload);
                        while (q.Count > MaxQueue && q.TryDequeue(out _)) { }
                    }
                }
            }
            catch (Exception) { }
        }

        private static bool ReadExactly(Stream s, byte[] buf, int n)
        {
            int off = 0;
            while (off < n)
            {
                int r = s.Read(buf, off, n - off);
                if (r <= 0) return false;
                off += r;
            }
            return true;
        }

        public static void Shutdown()
        {
            Stopping = true;
            lock (queues)
            {
                foreach (var l in listeners) { try { l.Stop(); } catch (Exception) { } }
                listeners.Clear();
                queues.Clear();
            }
        }
    }
}
