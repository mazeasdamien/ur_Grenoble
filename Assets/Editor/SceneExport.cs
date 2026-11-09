using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DDS_protocol;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2026: export of the twin scene for the Blender figures (zones of inspection, reach envelope).
// Writes <out>/twin_scene.fbx (engine, robot on its rail, VR green parts, at their world positions)
// and <out>/twin_scene.json (reference points in Unity world coordinates).
// Batch:  Unity -batchmode -projectPath ur_Grenoble -executeMethod SceneExport.Export -exportDir <out>
public static class SceneExport
{
    static string F(float v) => v.ToString("0.#####", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => $"[{F(v.x)}, {F(v.y)}, {F(v.z)}]";

    public static void Export()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-exportDir");
        string dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : "SceneExport";
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene("Assets/Scene/main.unity");

        var sm = Object.FindFirstObjectByType<ScenarioManager>(FindObjectsInactive.Include);
        var pub = Object.FindFirstObjectByType<UR_Joints_Publisher>(FindObjectsInactive.Include);
        var teleop = Object.FindFirstObjectByType<SIMU_DirectTeleop_SUB>(FindObjectsInactive.Include);
        var robotRoot = pub.baseRobot.root;
        var parts = new[] { sm.s1p1, sm.s1p2, sm.s1p3, sm.s1p4 };
        foreach (var p in parts) p.SetActive(true);

        var objects = new List<Object> { sm.engine, robotRoot.gameObject };
        objects.AddRange(parts);
        if (teleop.robot.root != robotRoot) objects.Add(teleop.robot.root.gameObject);
        var options = new ExportModelOptions
        {
            ExportFormat = ExportFormat.Binary,
            ObjectPosition = ObjectPosition.WorldAbsolute,
            ExportUnrendered = false,
        };
        ModelExporter.ExportObjects(Path.Combine(dir, "twin_scene.fbx"), objects.Distinct().ToArray(), options);

        // reach envelope exactly as UR_Joints_Publisher tests it (2023 code):
        // |baseRobot.localPosition - forDistance.localPosition| <= 0.8, both read as vectors of the
        // forDistance parent's frame; TCP_sender.localPosition.z >= limitHauteur; .y >= limitProfondeur
        var frame = pub.forDistance.parent;
        Vector3 c = frame.TransformPoint(pub.baseRobot.localPosition);
        float r = 0.8f * frame.lossyScale.x;
        var tcpFrame = pub.TCP_sender.parent;
        var rail = teleop.robot;
        var railParent = rail.parent;
        var sb = new StringBuilder("{\n");
        sb.Append($"  \"engine_name\": \"{sm.engine.name}\", \"robot_root\": \"{robotRoot.name}\",\n");
        sb.Append($"  \"vr_parts\": [{string.Join(", ", parts.Select(p => V(p.transform.position)))}],\n");
        sb.Append($"  \"vr_part_names\": [{string.Join(", ", parts.Select(p => $"\"{p.name}\""))}],\n");
        sb.Append($"  \"reach_sphere\": {{\"center\": {V(c)}, \"radius\": {F(r)}}},\n");
        sb.Append($"  \"height_limit\": {{\"point\": {V(tcpFrame.TransformPoint(new Vector3(0, 0, pub.limitHauteur)))}, \"normal\": {V(tcpFrame.TransformDirection(Vector3.forward).normalized)}, \"value\": {F(pub.limitHauteur)}}},\n");
        sb.Append($"  \"depth_limit\": {{\"point\": {V(tcpFrame.TransformPoint(new Vector3(0, pub.limitProfondeur, 0)))}, \"normal\": {V(tcpFrame.TransformDirection(Vector3.up).normalized)}, \"value\": {F(pub.limitProfondeur)}}},\n");
        sb.Append($"  \"robot_base\": {V(pub.baseRobot.position)},\n");
        sb.Append($"  \"rail_ends\": [{V(railParent.TransformPoint(new Vector3(-18f, rail.localPosition.y, rail.localPosition.z)))}, {V(railParent.TransformPoint(new Vector3(-3f, rail.localPosition.y, rail.localPosition.z)))}],\n");
        sb.Append($"  \"rail_start\": {V(rail.position)},\n");
        sb.Append($"  \"target\": {V(pub.forDistance.position)}\n}}\n");
        File.WriteAllText(Path.Combine(dir, "twin_scene.json"), sb.ToString());
        Debug.Log("SceneExport: done -> " + dir);
        EditorApplication.Exit(0);
    }
}
