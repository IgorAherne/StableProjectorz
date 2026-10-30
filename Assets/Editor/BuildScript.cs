using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace spz.BuildTools {

	// Entry point used by .github/workflows/build-windows.yml (GameCI).
	public static class BuildScript {

	    [MenuItem("Build/Windows (GameCI)")]
	    public static void BuildWindows() {
	        string[] scenes = EditorBuildSettings.scenes
	            .Where(s => s.enabled)
	            .Select(s => s.path)
	            .ToArray();

	        if (scenes.Length == 0) {
	            throw new BuildFailedException("No enabled scenes in EditorBuildSettings.");
	        }

	        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
	        string outputDir = Path.Combine(projectRoot, "build", "Windows");
	        Directory.CreateDirectory(outputDir);

	        string outputPath = Path.Combine(outputDir, "StableProjectorz.exe");
	        var options = new BuildPlayerOptions {
	            scenes = scenes,
	            locationPathName = outputPath,
	            target = BuildTarget.StandaloneWindows64,
	            targetGroup = BuildTargetGroup.Standalone,
	            options = BuildOptions.None,
	        };

	        BuildReport report = BuildPipeline.BuildPlayer(options);
	        BuildSummary summary = report.summary;

	        Debug.Log($"Windows build: {summary.result}, size={summary.totalSize} bytes, output={outputPath}");
	        if (summary.result != BuildResult.Succeeded) {
	            throw new BuildFailedException($"Windows build failed: {summary.result} ({summary.totalErrors} errors)");
	        }
	    }
	}
}
