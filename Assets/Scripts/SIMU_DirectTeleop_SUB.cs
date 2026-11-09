using DDS_protocol;
using K4os.Compression.LZ4;
using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;


public class SIMU_DirectTeleop_SUB : MonoBehaviour
{
    private protected DataReader<DynamicData> Reader { get; private set; }
    private bool init = false;

    public Transform target;
    public float speed;
    public float speedR;
    public Transform robot;

    public bool isVR;

    private void Update()
    {
        if (!init)
        {
            init = true;

            var typeFactory = DynamicTypeFactory.Instance;
            var Teleop = typeFactory.BuildStruct()
                .WithName("DirectTeleop")
                .AddMember(new StructMember("Rleft", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Lleft", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Left", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Right", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Forward", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Backward", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("zoomin", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("zoomout", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("t1", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("t2", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("t3", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("t4", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("isVR", typeFactory.GetPrimitiveType<bool>()))
                .Create();

            Reader = DDSHandler.SetupDataReader("DirectTeleopTopic", Teleop);
        }

        ProcessData(Reader);
        ApplyHeldButtons();
    }

    // 2026: frame-rate-independent teleoperation. In 2023 every received sample moved the target by
    // speed * Time.deltaTime of THIS instance, so the real speed was speed x (2D interface fps /
    // twin fps); the 2023 pilot logs show the rail at ~0.7 unit/s instead of 1.5. Samples now only
    // update the held-button state, and the motion is applied once per frame here.
    // Effective speeds = speed and speedR x Scale (command line: -teleopscale <factor>, default 1).
    public static float Scale = ReadScale();
    static float ReadScale()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-teleopscale");
        return i >= 0 && i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 1f;
    }

    readonly Dictionary<string, bool> held = new Dictionary<string, bool>();
    float lastSampleTime = -1f;
    static readonly string[] ButtonNames = { "Rleft", "Lleft", "Left", "Right", "Forward", "Backward", "zoomin", "zoomout", "t1", "t2", "t3", "t4" };

    bool Held(string name) => held.TryGetValue(name, out bool b) && b;

    void ProcessData(AnyDataReader anyReader)
    {
        var reader = (DataReader<DynamicData>)anyReader;
        using var samples = reader.Take();

        foreach (var sample in samples)
        {
            if (sample.Info.ValidData)
            {
                DynamicData data = sample.Data;
                foreach (var n in ButtonNames) held[n] = data.GetValue<bool>(n);
                isVR = data.GetValue<bool>("isVR");
                lastSampleTime = Time.unscaledTime;
            }
        }
    }

    void ApplyHeldButtons()
    {
        // release everything if the 2D interface stops sending (closed or frozen)
        if (lastSampleTime < 0 || Time.unscaledTime - lastSampleTime > 0.25f) return;
        float s = speed * Scale * Time.deltaTime;
        float sr = speedR * Scale * Time.deltaTime;

        if (Held("Rleft") && robot.localPosition.x <= -3) robot.localPosition += new Vector3(sr, 0, 0);
        if (Held("Lleft") && robot.localPosition.x >= -18) robot.localPosition += new Vector3(-sr, 0, 0);
        if (Held("Left")) target.localPosition += new Vector3(s, 0, 0);
        if (Held("Right")) target.localPosition += new Vector3(-s, 0, 0);
        if (Held("t1")) target.localPosition += new Vector3(s, s, 0);
        if (Held("t2")) target.localPosition += new Vector3(-s, s, 0);
        if (Held("t3")) target.localPosition += new Vector3(s, -s, 0);
        if (Held("t4")) target.localPosition += new Vector3(-s, -s, 0);
        if (Held("Forward")) target.localPosition += new Vector3(0, s, 0);
        if (Held("Backward")) target.localPosition += new Vector3(0, -s, 0);
        if (Held("zoomin")) target.localPosition += new Vector3(0, 0, s);
        if (Held("zoomout")) target.localPosition += new Vector3(0, 0, -s);
    }
}
