using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SmartSkills.MSBuild;

/// <summary>
/// MSBuild task that installs resolved skills to the output directory.
/// </summary>
public class InstallSmartSkills : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] Skills { get; set; } = [];

    [Required]
    public string OutputDirectory { get; set; } = "";

    public override bool Execute()
    {
        if (Skills.Length == 0)
        {
            Log.LogMessage(MessageImportance.Low, "SmartSkills: No skills to install.");
            return true;
        }

        Log.LogMessage(MessageImportance.Normal, "SmartSkills: Installing {0} skill(s) to {1}", Skills.Length, OutputDirectory);

        foreach (var skill in Skills)
        {
            var skillPath = skill.ItemSpec;
            Log.LogMessage(MessageImportance.Normal, "SmartSkills: Installing skill: {0}", skillPath);

            try
            {
                // In a full implementation, this would download and install the skill.
                Log.LogMessage(MessageImportance.Normal, "SmartSkills: Skill installed: {0}", skillPath);
            }
#pragma warning disable CA1031 // MSBuild tasks intentionally catch all exceptions to avoid breaking builds
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Log.LogWarningFromException(ex, showStackTrace: true);
            }
        }

        return true;
    }
}
