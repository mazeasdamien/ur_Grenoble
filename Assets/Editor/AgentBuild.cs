using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 2026: command-line build of the twin for the agent study.
public static class AgentBuild
{
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scene/main.unity" },
            locationPathName = "Build/ur_Grenoble.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("AgentBuild: " + report.summary.result + " in " + report.summary.totalTime + ", " + report.summary.totalErrors + " error(s)");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
