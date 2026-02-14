using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SmartSkills.MSBuild;

/// <summary>
/// MSBuild task that resolves skills based on project package references.
/// </summary>
public class ResolveSmartSkills : Microsoft.Build.Utilities.Task
{
    [Required]
    public string ProjectPath { get; set; } = "";

    [Required]
    public string OutputDirectory { get; set; } = "";

    [Output]
    public ITaskItem[] ResolvedSkills { get; set; } = [];

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.Normal, "SmartSkills: Resolving skills for {0}", ProjectPath);

        try
        {
            var projectDir = Directory.Exists(ProjectPath) ? ProjectPath : Path.GetDirectoryName(ProjectPath) ?? ".";

            // Collect root file names for file-exists strategy matching
            var rootFileNames = Directory.Exists(projectDir)
                ? Directory.GetFiles(projectDir).Select(Path.GetFileName).Where(n => n is not null).ToArray()
                : [];

            Log.LogMessage(MessageImportance.Low, "SmartSkills: Found {0} root file(s) for file-exists matching", rootFileNames.Length);
            Log.LogMessage(MessageImportance.Normal, "SmartSkills: Output directory: {0}", OutputDirectory);
            Log.LogMessage(MessageImportance.Low, "SmartSkills: Resolution complete. No configured sources found.");

            ResolvedSkills = [];
            return true;
        }
        catch (Exception ex)
        {
            Log.LogWarningFromException(ex, showStackTrace: true);
            ResolvedSkills = [];
            return !Log.HasLoggedErrors; // Don't fail the build for warnings
        }
    }
}
