using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// 2026: command link for the agent study. Newline-delimited JSON over TCP on 127.0.0.1.
// One request per line, one JSON response per line. Requests are executed on Unity's main
// thread, in order; a handler is a coroutine, so it may wait (button held, end of frame).
// Identical copy in ur_Grenoble and EngineRoomScene.

[Serializable]
public class AgentRequest
{
    public string cmd = "";
    public string button = "";
    public float duration;
    public float dx, dy, dz, droll, dpitch, dyaw;
    public int n;
    public string label = "";
    public string value = "";
    public int w, h;
    public float x, y, z, lx, ly, lz;
}

public abstract class AgentLink : MonoBehaviour
{
    TcpListener listener;
    Thread acceptThread;
    volatile bool running;
    readonly ConcurrentQueue<(AgentRequest req, TaskCompletionSource<string> done)> inbox =
        new ConcurrentQueue<(AgentRequest, TaskCompletionSource<string>)>();
    bool busy;

    protected static int ArgInt(string name, int fallback)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int v) ? v : fallback;
    }

    protected static string ArgString(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
    }

    protected static bool HasArg(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

    protected void Listen(int port)
    {
        Application.runInBackground = true;
        listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        running = true;
        acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "AgentLink" };
        acceptThread.Start();
        Debug.Log($"AgentLink: listening on 127.0.0.1:{port}");
    }

    void AcceptLoop()
    {
        while (running)
        {
            TcpClient client;
            try { client = listener.AcceptTcpClient(); }
            catch { break; }
            var t = new Thread(() => Serve(client)) { IsBackground = true, Name = "AgentLinkClient" };
            t.Start();
        }
    }

    void Serve(TcpClient client)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" })
        {
            client.NoDelay = true;
            while (running)
            {
                string line;
                try { line = reader.ReadLine(); } catch { break; }
                if (line == null) break;
                if (line.Trim().Length == 0) continue;
                string reply;
                try
                {
                    var req = JsonUtility.FromJson<AgentRequest>(line);
                    var done = new TaskCompletionSource<string>();
                    inbox.Enqueue((req, done));
                    reply = done.Task.Result;
                }
                catch (Exception e)
                {
                    reply = Json.Obj("ok", false, "error", "bad request: " + e.Message);
                }
                try { writer.WriteLine(reply); } catch { break; }
            }
        }
    }

    protected virtual void Update()
    {
        if (busy || !inbox.TryDequeue(out var item)) return;
        busy = true;
        StartCoroutine(Run(item.req, item.done));
    }

    IEnumerator Run(AgentRequest req, TaskCompletionSource<string> done)
    {
        var result = new Reply();
        IEnumerator handler = null;
        try { handler = Handle(req, result); }
        catch (Exception e) { result.Fail(e.Message); }
        while (handler != null)
        {
            object current;
            try
            {
                if (!handler.MoveNext()) break;
                current = handler.Current;
            }
            catch (Exception e) { result.Fail(e.GetType().Name + ": " + e.Message); break; }
            yield return current;
        }
        done.TrySetResult(result.ToJson());
        busy = false;
    }

    // Fill `reply`; yield to wait frames.
    protected abstract IEnumerator Handle(AgentRequest req, Reply reply);

    protected virtual void OnDestroy()
    {
        running = false;
        try { listener?.Stop(); } catch { }
    }

    // ---- helpers ----

    protected static IEnumerator CaptureScreen(Reply reply, string key = "image")
    {
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        reply["width"] = tex.width;
        reply["height"] = tex.height;
        reply[key] = Convert.ToBase64String(tex.EncodeToPNG());
        Destroy(tex);
    }

    // Render a camera off-screen (HDRP needs a few frames for exposure/history to settle).
    protected static IEnumerator RenderCamera(Camera cam, int w, int h, Reply reply, string key = "image", int settleFrames = 6)
    {
        var rt = RenderTexture.GetTemporary(w, h, 24);
        var old = cam.targetTexture;
        bool wasEnabled = cam.enabled;
        cam.targetTexture = rt;
        cam.enabled = false;
        for (int f = 0; f < settleFrames; f++) { yield return new WaitForEndOfFrame(); cam.Render(); }
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = old;
        cam.enabled = wasEnabled;
        RenderTexture.ReleaseTemporary(rt);
        reply["width"] = w;
        reply["height"] = h;
        reply[key] = Convert.ToBase64String(tex.EncodeToPNG());
        Destroy(tex);
    }

    protected static object V3(Vector3 v) => new[] { R(v.x), R(v.y), R(v.z) };
    protected static double R(float f, int digits = 4) => Math.Round(f, digits);
}

public class Reply : Dictionary<string, object>
{
    public Reply() { this["ok"] = true; }
    public void Fail(string error) { this["ok"] = false; this["error"] = error; }
    public string ToJson() => Json.Write(this);
}

public static class Json
{
    public static string Obj(params object[] kv)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i + 1 < kv.Length; i += 2) d[(string)kv[i]] = kv[i + 1];
        return Write(d);
    }

    public static string Write(object o)
    {
        var sb = new StringBuilder();
        WriteValue(sb, o);
        return sb.ToString();
    }

    static void WriteValue(StringBuilder sb, object o)
    {
        switch (o)
        {
            case null: sb.Append("null"); break;
            case string s: WriteString(sb, s); break;
            case bool b: sb.Append(b ? "true" : "false"); break;
            case float f: sb.Append(float.IsFinite(f) ? f.ToString("R", CultureInfo.InvariantCulture) : "null"); break;
            case double d: sb.Append(double.IsFinite(d) ? d.ToString("R", CultureInfo.InvariantCulture) : "null"); break;
            case int or long or short or byte: sb.Append(Convert.ToString(o, CultureInfo.InvariantCulture)); break;
            case IDictionary<string, object> dict:
                sb.Append('{');
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    WriteValue(sb, kv.Value);
                }
                sb.Append('}');
                break;
            case IEnumerable list:
                sb.Append('[');
                bool f1 = true;
                foreach (var item in list)
                {
                    if (!f1) sb.Append(',');
                    f1 = false;
                    WriteValue(sb, item);
                }
                sb.Append(']');
                break;
            default: WriteString(sb, o.ToString()); break;
        }
    }

    static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}
