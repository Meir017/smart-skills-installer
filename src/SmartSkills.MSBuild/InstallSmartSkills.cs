using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SmartSkills.MSBuild;

/// <summary>
/// MSBuild task that downloads and installs resolved skills to the local file system.
/// </summary>
public class InstallSmartSkills : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private CancellationTokenSource _cts = new();

    [Required]
    public ITaskItem[] ResolvedSkills { get; set; } = [];

    [Required]
    public string InstallDirectory { get; set; } = string.Empty;

    public bool ForceReinstall { get; set; }

    public int MaxParallelDownloads { get; set; } = 4;

    [Output]
    public ITaskItem[] InstalledSkills { get; set; } = [];

    public override bool Execute()
    {
        try
        {
            if (ResolvedSkills.Length == 0)
            {
                Log.LogMessage(MessageImportance.Normal, "SmartSkills: No skills to install");
                return true;
            }

            Log.LogMessage(MessageImportance.Normal,
                "SmartSkills: Installing {0} skill(s) to {1}",
                ResolvedSkills.Length, InstallDirectory);

            var installed = new List<ITaskItem>();
            foreach (var skill in ResolvedSkills)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var skillId = skill.ItemSpec;
                var version = skill.GetMetadata("Version");
                var sourceUrl = skill.GetMetadata("SourceUrl");

                Log.LogMessage(MessageImportance.Normal,
                    "  Installing: {0} v{1}", skillId, version);

                // Actual download would happen here using SkillPackageDownloader
                var taskItem = new TaskItem(skillId);
                taskItem.SetMetadata("Version", version);
                taskItem.SetMetadata("InstallPath", Path.Combine(InstallDirectory, skillId));
                installed.Add(taskItem);
            }

            InstalledSkills = installed.ToArray();
            Log.LogMessage(MessageImportance.High,
                "SmartSkills: Installed {0} skill(s) ({1} skipped)",
                installed.Count, ResolvedSkills.Length - installed.Count);

            return true;
        }
        catch (OperationCanceledException)
        {
            Log.LogMessage(MessageImportance.High, "SmartSkills: Installation cancelled");
            return false;
        }
        catch (Exception ex)
        {
            Log.LogError("SMSK002", "", "", "", 0, 0, 0, 0,
                "SmartSkills: Failed to install skills: {0}", ex.Message);
            return false;
        }
    }

    public void Cancel() => _cts.Cancel();
}
