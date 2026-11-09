using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DDS_protocol;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// 2026: agent-study server of the twin instance (IK, collisions, immersive scene). Started only
// when the player is launched with  -agentport <port>.
//
// Commands (JSON line, field "cmd"):
//   ping | state | reset
//   press {button, duration}      2D condition: hold one 2023 button (DirectTeleop field name:
//                                 Left Right Forward Backward zoomin zoomout t1 t2 t3 t4 Rleft Lleft)
//   move_target {dx,dy,dz,droll,dpitch,dyaw}   immersive condition: move the grabbed target
//                                 (metres / degrees, target-parent frame; <= 0.3 m and 45 deg)
//   rail {dx}                     immersive condition: the two VR push buttons of the linear rail
//   step {n: 0..4}                show the green part of step n on this engine (VR scenario)
// Optional  -scenario 1|2 : inspected parts of scenario 1 (2023 2D) or 2 (2023 VR, default).
//   viewpoint {n: 1..5}  eye {x,y,z,lx,ly,lz}   view {w,h}   first-person view
public class AgentServer : AgentLink
{
    SIMU_DirectTeleop_SUB teleop;
    UR_target urTarget;
    UR_Joints_Publisher joints;
    MoveRobot_PUB track;
    CollisionManager collisions;
    inspection_cam inspectionCam;
    ScenarioManager sm;
    Camera eye;

    Vector3 targetPos0, ikPos0, railPos0;
    Quaternion targetRot0, ikRot0;
    readonly Dictionary<string, int> segmentHits = new Dictionary<string, int>();
    readonly Dictionary<string, bool> segmentWas = new Dictionary<string, bool>();
    string lastPressed = "";
    readonly List<Pose> viewpoints = new List<Pose>();
    int currentViewpoint = 1;

    static readonly Dictionary<string, string> Opposite = new Dictionary<string, string>
    {
        ["Left"] = "Right", ["Right"] = "Left", ["Forward"] = "Backward", ["Backward"] = "Forward",
        ["zoomin"] = "zoomout", ["zoomout"] = "zoomin", ["t1"] = "t4", ["t4"] = "t1", ["t2"] = "t3", ["t3"] = "t2"
    };
    static readonly string[] Buttons = { "Left", "Right", "Forward", "Backward", "zoomin", "zoomout", "t1", "t2", "t3", "t4", "Rleft", "Lleft" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        int port = ArgInt("-agentport", 0);
        if (port == 0) return;
        var go = new GameObject("AgentServer");
        go.AddComponent<AgentServer>().Listen(port);
    }

    IEnumerator Start()
    {
        yield return null;
        teleop = FindFirstObjectByType<SIMU_DirectTeleop_SUB>(FindObjectsInactive.Include);
        urTarget = FindFirstObjectByType<UR_target>(FindObjectsInactive.Include);
        joints = FindFirstObjectByType<UR_Joints_Publisher>(FindObjectsInactive.Include);
        track = FindFirstObjectByType<MoveRobot_PUB>(FindObjectsInactive.Include);
        collisions = FindFirstObjectByType<CollisionManager>(FindObjectsInactive.Include);
        inspectionCam = FindFirstObjectByType<inspection_cam>(FindObjectsInactive.Include);
        sm = FindFirstObjectByType<ScenarioManager>(FindObjectsInactive.Include);
        Debug.Log($"AgentServer: teleop {teleop != null}, target {urTarget != null}, joints {joints != null}, track {track != null}, collisions {collisions != null}, cam {inspectionCam != null}, scenario {sm != null}; " +
                  $"same target object for buttons and grab: {teleop != null && urTarget != null && teleop.target == urTarget.target}");

        targetPos0 = Target.localPosition; targetRot0 = Target.localRotation;
        ikPos0 = urTarget.ik.position; ikRot0 = urTarget.ik.rotation;
        railPos0 = Rail.localPosition;
        scenario = ArgInt("-scenario", 2);
        if (scenario == 1 && sm != null) PlaceScenario1Parts();
        foreach (var s in SegmentFlags()) { segmentHits[s.Key] = 0; segmentWas[s.Key] = s.Value; }

        // First-person camera: same lighting/HDRP settings as the participant's head camera.
        var head = Camera.main;
        if (head != null)
        {
            headPos0 = head.transform.position;
            headFwd0 = head.transform.forward;
            floorY = head.transform.root.position.y;
        }
        var go = new GameObject("AgentEye");
        eye = go.AddComponent<Camera>();
        if (head != null)
        {
            eye.CopyFrom(head);
            var src = head.GetComponent<HDAdditionalCameraData>();
            var dst = go.GetComponent<HDAdditionalCameraData>() ?? go.AddComponent<HDAdditionalCameraData>();
            if (src != null) src.CopyTo(dst);
        }
        eye.stereoTargetEye = StereoTargetEyeMask.None;
        eye.fieldOfView = 60f;
        eye.nearClipPlane = 0.02f;
        eye.enabled = false;
        BuildViewpoints(head);
        ApplyViewpoint(1);

        // Nobody watches this window in agent mode (the first-person view is rendered on demand):
        // stop drawing the head camera every frame unless -showtwin is given. Saves ~1/3 of an
        // integrated GPU; the IK, collisions and relay keep running.
        if (head != null && !HasArg("-showtwin"))
        {
            head.enabled = false;
            Debug.Log("AgentServer: head camera not drawn (start with -showtwin to see the twin window)");
        }
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c.enabled) Debug.Log($"AgentServer: camera still drawn every frame: {c.name} -> {(c.targetTexture != null ? c.targetTexture.width + "x" + c.targetTexture.height : "screen")}");
    }

    Transform Target => urTarget != null ? urTarget.target : teleop.target;
    Transform Rail => teleop.robot;

    // Five standing viewpoints of the immersive condition (simplification of the free walk in the
    // 4 x 4 m area of 2023), placed around the green part of the current step, eye height 1.65 m:
    //   1 robot side, left of the part     2 robot side, right of the part
    //   3 opposite side, across the engine 4 nearest end of the engine
    //   5 leaning over the part from the robot side
    // Scene (world, metres): engine x -0.75..1.26, z -1.64..-0.19, top ~1.34; robot base z -0.07,
    // rail along x; floor y 0; the 2023 head start (0, 1.6, 0) is right behind the robot.
    const float EyeY = 1.65f;
    void BuildViewpoints(Camera head)
    {
        viewpoints.Clear();
        Vector3 c = EngineCenter();
        Vector3 p = ActivePart() != null ? ActivePart().position : new Vector3(c.x, 1.1f, -0.65f);
        viewpoints.Add(new Pose(new Vector3(p.x - 0.9f, EyeY, 0.45f), Quaternion.identity));
        viewpoints.Add(new Pose(new Vector3(p.x + 0.9f, EyeY, 0.45f), Quaternion.identity));
        viewpoints.Add(new Pose(new Vector3(p.x, EyeY + 0.1f, -2.35f), Quaternion.identity));
        // right end: the floating VR panel hangs above it, so stand 0.6 m towards the far side
        viewpoints.Add(new Pose(p.x < c.x ? new Vector3(-1.55f, EyeY, p.z) : new Vector3(2.05f, EyeY, p.z - 0.6f), Quaternion.identity));
        viewpoints.Add(new Pose(new Vector3(p.x, 1.9f, p.z + 0.75f), Quaternion.identity));
    }

    Vector3 engineCenter;
    bool engineCenterKnown;
    Vector3 EngineCenter()
    {
        if (engineCenterKnown) return engineCenter;
        engineCenter = new Vector3(0.2555f, 0.6395f, -0.9139f);
        if (sm != null && sm.engine != null)
        {
            var rs = sm.engine.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) { var bb = rs[0].bounds; foreach (var x in rs) bb.Encapsulate(x.bounds); engineCenter = bb.center; }
        }
        engineCenterKnown = true;
        return engineCenter;
    }

    void ApplyViewpoint(int n)
    {
        currentViewpoint = n;
        BuildViewpoints(null); // follows the green part of the current step
        var vp = viewpoints[n - 1];
        eye.transform.position = vp.position;
        var part = ActivePart();
        eye.transform.LookAt(part != null ? part.position : EngineCenter()); // the participant looks at the green part
    }

    // First-person render for the agent. The floating panels of the VR interface (instructions,
    // step, labels, camera feed, distance label) are hidden: they occlude the robot and the part
    // from several viewpoints, and their content reaches the agent as text. value "ui" keeps them
    // (figures of what the human participants saw).
    IEnumerator EyeRender(AgentRequest req, Reply reply)
    {
        var hidden = new List<Canvas>();
        if (req.value != "ui")
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.enabled && c.renderMode == RenderMode.WorldSpace) { c.enabled = false; hidden.Add(c); }
        reply["ui_hidden"] = hidden.Count;
        yield return RenderCamera(eye, req.w > 0 ? req.w : 1280, req.h > 0 ? req.h : 720, reply);
        foreach (var c in hidden) if (c != null) c.enabled = true;
    }

    int scenario = 2;

    // 2026 (agent study, scenario crossed with the interface): the green parts of this scene are
    // those of scenario 2. For scenario 1 they move to the parts of the 2D scenario, whose positions
    // are the local positions of s1p1..s1p4 in EngineRoomScene's "environment" frame, i.e. this
    // scene's world frame. Part shapes stay those of scenario 2; only their positions change.
    static readonly Vector3[] Scenario1Parts =
    {
        new Vector3(0.0171f, 1.0209f, -0.6997f),
        new Vector3(0.6501f, 1.0494f, -0.7294f),
        new Vector3(0.377f, 1.0219f, -0.6984f),
        new Vector3(1.0726f, 1.0162f, -0.7033f),
    };

    void PlaceScenario1Parts()
    {
        var parts = new[] { sm.s1p1, sm.s1p2, sm.s1p3, sm.s1p4 };
        for (int i = 0; i < 4; i++) parts[i].transform.position = Scenario1Parts[i];
    }

    Transform ActivePart()
    {
        if (sm == null) return null;
        switch ((int)sm.parts)
        {
            case 1: return sm.s1p1.transform;
            case 2: return sm.s1p2.transform;
            case 3: return sm.s1p3.transform;
            case 4: return sm.s1p4.transform;
            default: return null;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (collisions == null) return;
        foreach (var s in SegmentFlags())
        {
            if (s.Value && !segmentWas[s.Key]) segmentHits[s.Key]++;
            segmentWas[s.Key] = s.Value;
        }
    }

    IEnumerable<KeyValuePair<string, bool>> SegmentFlags()
    {
        yield return new KeyValuePair<string, bool>("base", collisions.isBaseColliding);
        yield return new KeyValuePair<string, bool>("shoulder", collisions.isShoulderColliding);
        yield return new KeyValuePair<string, bool>("elbow", collisions.isElbowColliding);
        yield return new KeyValuePair<string, bool>("wrist1", collisions.isWrist1Colliding);
        yield return new KeyValuePair<string, bool>("wrist2", collisions.isWrist2Colliding);
        yield return new KeyValuePair<string, bool>("wrist3", collisions.isWrist3Colliding);
        yield return new KeyValuePair<string, bool>("cam", collisions.isCamColliding);
    }

    protected override IEnumerator Handle(AgentRequest req, Reply reply)
    {
        switch (req.cmd)
        {
            case "ping":
                reply["instance"] = "ur_Grenoble";
                break;
            case "info":
                FillInfo(reply);
                break;
            case "state":
                FillState(reply);
                break;
            case "press":
                yield return Press(req.button, req.duration, reply);
                FillState(reply);
                break;
            case "move_target":
                MoveTarget(req, reply);
                yield return null;
                FillState(reply);
                break;
            case "rail":
                {
                    // VR push buttons: 1 unit/s between -18 and -3 (MoveRobot_PUB)
                    float x = Mathf.Clamp(Rail.localPosition.x + req.dx, -18f, -3f);
                    Rail.localPosition = new Vector3(x, Rail.localPosition.y, Rail.localPosition.z);
                    yield return null;
                    FillState(reply);
                    break;
                }
            case "step":
                if (req.n < 0 || req.n > 4) { reply.Fail("n must be 0..4"); break; }
                sm.parts = (ScenarioManager.scenario1_parts)req.n;
                yield return null;
                ApplyViewpoint(currentViewpoint);
                FillState(reply);
                break;
            case "viewpoint":
                if (req.n < 1 || req.n > viewpoints.Count) { reply.Fail($"n must be 1..{viewpoints.Count}"); break; }
                ApplyViewpoint(req.n);
                yield return EyeRender(req, reply);
                break;
            case "eye":
                eye.transform.position = new Vector3(req.x, req.y, req.z);
                eye.transform.LookAt(new Vector3(req.lx, req.ly, req.lz));
                yield return EyeRender(req, reply);
                break;
            case "view":
                yield return EyeRender(req, reply);
                break;
            case "reset":
                Target.localPosition = targetPos0; Target.localRotation = targetRot0;
                urTarget.ik.SetPositionAndRotation(ikPos0, ikRot0);
                Rail.localPosition = railPos0;
                yield return null;
                yield return null;
                collisions.count = 0;
                foreach (var k in segmentHits.Keys.ToList()) segmentHits[k] = 0;
                lastPressed = "";
                FillState(reply);
                break;
            default:
                reply.Fail($"unknown command '{req.cmd}'");
                break;
        }
    }

    // Same motion as SIMU_DirectTeleop_SUB while a 2023 button is held, including the
    // OutOfRqngeHelper rule: when out of range, only the opposite of the last button works.
    IEnumerator Press(string button, float duration, Reply reply)
    {
        if (!Buttons.Contains(button)) { reply.Fail($"unknown button '{button}'"); reply["buttons"] = Buttons; yield break; }
        duration = Mathf.Clamp(duration, 0.02f, 2f);
        bool isRail = button == "Rleft" || button == "Lleft";
        if (!isRail && joints.robotstate == 3 && !(Opposite.TryGetValue(lastPressed, out var opp) && opp == button))
        {
            reply["blocked"] = true;
            reply["note"] = "out of range: only the opposite of the last button is available";
            yield break;
        }
        if (!isRail) lastPressed = button;
        float speed = teleop.speed * SIMU_DirectTeleop_SUB.Scale, speedR = teleop.speedR * SIMU_DirectTeleop_SUB.Scale, t = 0f;
        while (t < duration)
        {
            float dt = Mathf.Min(Time.deltaTime, duration - t);
            t += Time.deltaTime;
            float s = speed * dt;
            var T = teleop.target;
            switch (button)
            {
                case "Rleft": if (Rail.localPosition.x <= -3) Rail.localPosition += new Vector3(speedR * dt, 0, 0); break;
                case "Lleft": if (Rail.localPosition.x >= -18) Rail.localPosition += new Vector3(-speedR * dt, 0, 0); break;
                case "Left": T.localPosition += new Vector3(s, 0, 0); break;
                case "Right": T.localPosition += new Vector3(-s, 0, 0); break;
                case "t1": T.localPosition += new Vector3(s, s, 0); break;
                case "t2": T.localPosition += new Vector3(-s, s, 0); break;
                case "t3": T.localPosition += new Vector3(s, -s, 0); break;
                case "t4": T.localPosition += new Vector3(-s, -s, 0); break;
                case "Forward": T.localPosition += new Vector3(0, s, 0); break;
                case "Backward": T.localPosition += new Vector3(0, -s, 0); break;
                case "zoomin": T.localPosition += new Vector3(0, 0, s); break;
                case "zoomout": T.localPosition += new Vector3(0, 0, -s); break;
            }
            yield return null;
            if (!isRail && joints.robotstate == 3 && !(Opposite.TryGetValue(lastPressed, out var o2) && o2 == button) && Opposite.ContainsKey(button))
            {
                // as in 2023, the other buttons are released as soon as the robot goes out of range
                reply["released_out_of_range"] = true;
                break;
            }
        }
        reply["held_s"] = Math.Round(Mathf.Min(t, duration), 3);
    }

    void MoveTarget(AgentRequest r, Reply reply)
    {
        var d = new Vector3(r.dx, r.dy, r.dz);
        if (d.magnitude > 0.3f) { d = d.normalized * 0.3f; reply["clamped"] = true; }
        var e = new Vector3(Mathf.Clamp(r.dpitch, -45, 45), Mathf.Clamp(r.dyaw, -45, 45), Mathf.Clamp(r.droll, -45, 45));
        Target.localPosition += d;
        Target.localRotation = Target.localRotation * Quaternion.Euler(e);
    }

    Vector3 headPos0, headFwd0;
    float floorY;

    // Scene geometry for placing the viewpoints (world coordinates).
    void FillInfo(Reply r)
    {
        r["head_start_position"] = V3(headPos0);
        r["head_start_forward"] = V3(headFwd0);
        r["floor_y"] = R(floorY);
        if (sm != null)
        {
            var parts = new List<object>();
            foreach (var go in new[] { sm.s1p1, sm.s1p2, sm.s1p3, sm.s1p4 }) parts.Add(go != null ? V3(go.transform.position) : null);
            r["parts"] = parts;
            if (sm.engine != null)
            {
                var rs = sm.engine.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds;
                    foreach (var x in rs) b.Encapsulate(x.bounds);
                    r["engine_center"] = V3(b.center);
                    r["engine_size"] = V3(b.size);
                }
            }
        }
        r["robot_base"] = V3(Rail.position);
        r["rail_axis"] = V3(Rail.parent != null ? Rail.parent.TransformDirection(Vector3.right).normalized : Vector3.right);
        r["target_world"] = V3(Target.position);
        r["viewpoints"] = viewpoints.Select(v => (object)new Dictionary<string, object> { ["position"] = V3(v.position), ["forward"] = V3(v.rotation * Vector3.forward) }).ToList();
    }

    void FillState(Reply r)
    {
        r["scenario"] = scenario;
        var T = Target;
        var ik = urTarget.ik;
        float posErr = Vector3.Distance(ik.position, T.position);
        float rotErr = Quaternion.Angle(ik.rotation, T.rotation);
        r["target_local_position"] = V3(T.localPosition);
        r["target_local_euler"] = V3(T.localEulerAngles);
        r["ik_world_position"] = V3(ik.position);
        r["ik_lag_cm"] = Math.Round(posErr * 100f, 2);
        r["ik_lag_deg"] = Math.Round(rotErr, 2);
        r["robot_state"] = joints.robotstate; // 1 in range, 2 near limit, 3 out of range (robot stops)
        r["settled"] = posErr < 0.001f && rotErr < 0.5f;
        r["rail_x"] = R(Rail.localPosition.x);
        r["teleop_scale"] = SIMU_DirectTeleop_SUB.Scale;
        r["displayed_distance_cm"] = track.distance;
        r["step"] = sm != null ? (int)sm.parts : -1;
        r["viewpoint"] = currentViewpoint;
        r["collisions_total"] = collisions.count;
        r["collisions_by_segment"] = new Dictionary<string, object>(segmentHits.ToDictionary(k => k.Key, k => (object)k.Value));
        r["colliding_now"] = SegmentFlags().Where(s => s.Value).Select(s => s.Key).ToList();
        if (inspectionCam != null)
        {
            var c = inspectionCam.transform;
            if (Physics.Linecast(c.position, c.position + c.forward * inspectionCam.maxLength, out var hit))
            {
                r["ray_hit"] = hit.transform.name;
                r["ray_hit_layer"] = hit.transform.gameObject.layer;
                r["ray_distance_cm"] = Math.Round(hit.distance * 100f, 1);
            }
            else r["ray_hit"] = null;
        }
    }
}
