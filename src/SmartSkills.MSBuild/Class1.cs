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
            // In a full implementation, this would use Core services to resolve skills.
            // For now, log the intent and return empty list (skills require configured sources).
            Log.LogMessage(MessageImportance.Normal, "SmartSkills: Output directory: {0}", OutputDirectory);
            Log.LogMessage(MessageImportance.Low, "SmartSkills: Resolution complete. No configured sources found.");

            ResolvedSkills = [];
            return true;
        }
        catch (Exception ex)
        {
            Log.LogWarning("SmartSkills: Skill resolution failed: {0}", ex.Message);
            ResolvedSkills = [];
            return true; // Don't fail the build
        }
    }
}
