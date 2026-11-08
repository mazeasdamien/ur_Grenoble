using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2026: dumps the VR scene's four inspection targets and engine transform, to map them onto
// EngineRoomScene's engine (whose camera fed the VR participant's inspection panel).
public static class GroundTruthVR
{
    static string V(Vector3 v) => $"[{v.x:F4},{v.y:F4},{v.z:F4}]";
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    static void Line(StringBuilder sb, string label, GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        Bounds? b = null; foreach (var r in rs) { if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; } }
        sb.AppendLine($"{label}\t{Path(go.transform)}\tpos={V(go.transform.position)}\trot={V(go.transform.eulerAngles)}\tlscale={V(go.transform.lossyScale)}\tbc={(b.HasValue ? V(b.Value.center) : "-")}\tbs={(b.HasValue ? V(b.Value.size) : "-")}");
    }
    public static void Dump()
    {
        EditorSceneManager.OpenScene("Assets/Scene/main.unity");
        var sm = Object.FindFirstObjectByType<ScenarioManager>(FindObjectsInactive.Include);
        var sb = new StringBuilder();
        Line(sb, "s1p1", sm.s1p1); Line(sb, "s1p2", sm.s1p2); Line(sb, "s1p3", sm.s1p3); Line(sb, "s1p4", sm.s1p4);
        Line(sb, "engine", sm.engine);
        foreach (Transform c in sm.engine.transform) Line(sb, "  child", c.gameObject);
        File.WriteAllText(System.Environment.GetEnvironmentVariable("GROUND_TRUTH_OUT") ?? "gt_vr.txt", sb.ToString());
        EditorApplication.Exit(0);
    }
}
