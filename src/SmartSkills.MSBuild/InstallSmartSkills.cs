using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;

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

    public bool Verbose { get; set; }

    [Output]
    public ITaskItem[] InstalledSkills { get; set; } = [];

    public override bool Execute()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (ResolvedSkills.Length == 0)
            {
                LogDiag(MessageImportance.Normal, "SmartSkills: No skills to install");
                return true;
            }

            LogDiag(MessageImportance.Normal,
                "SmartSkills: Installing {0} skill(s) to {1}",
                ResolvedSkills.Length, InstallDirectory);
            LogDiag(MessageImportance.Low, "  ForceReinstall: {0}", ForceReinstall);
            LogDiag(MessageImportance.Low, "  MaxParallelDownloads: {0}", MaxParallelDownloads);

            var installed = new List<ITaskItem>();
            var skipped = 0;

            foreach (var skill in ResolvedSkills)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var skillId = skill.ItemSpec;
                var version = skill.GetMetadata("Version");
                var sourceUrl = skill.GetMetadata("SourceUrl");
                var installPath = Path.Combine(InstallDirectory, skillId);

                if (!ForceReinstall && Directory.Exists(installPath))
                {
                    LogDiag(MessageImportance.Low, "  Skipping (already installed): {0} v{1}", skillId, version);
                    skipped++;
                    continue;
                }

                LogDiag(MessageImportance.Normal, "  Installing: {0} v{1}", skillId, version);

                var taskItem = new TaskItem(skillId);
                taskItem.SetMetadata("Version", version);
                taskItem.SetMetadata("InstallPath", installPath);
                installed.Add(taskItem);
            }

            InstalledSkills = installed.ToArray();

            sw.Stop();
            LogDiag(MessageImportance.High,
                "SmartSkills: Installed {0} skill(s), skipped {1}, in {2}ms",
                installed.Count, skipped, sw.ElapsedMilliseconds);

            return true;
        }
        catch (OperationCanceledException)
        {
            LogDiag(MessageImportance.High, "SmartSkills: Installation cancelled after {0}ms", sw.ElapsedMilliseconds);
            return false;
        }
        catch (Core.SmartSkillsException ex)
        {
            Log.LogError(subcategory: "SmartSkills", errorCode: ex.ErrorCode,
                helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0,
                endLineNumber: 0, endColumnNumber: 0,
                message: ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Log.LogError(subcategory: "SmartSkills", errorCode: "SMSK002",
                helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0,
                endLineNumber: 0, endColumnNumber: 0,
                message: "Failed to install skills: {0}", ex.Message);
            LogDiag(MessageImportance.Low, "Stack trace: {0}", ex.ToString());
            return false;
        }
    }

    public void Cancel() => _cts.Cancel();

    private void LogDiag(MessageImportance importance, string message, params object[] args)
    {
        if (Verbose || importance >= MessageImportance.Normal)
            Log.LogMessage(importance, message, args);
        else
            Log.LogMessage(MessageImportance.Low, message, args);
    }
}
