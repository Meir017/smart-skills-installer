using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SmartSkills.Core.Scanning;
using System.Diagnostics;

namespace SmartSkills.MSBuild;

/// <summary>
/// MSBuild task that resolves PackageReferences to applicable agent skills from the registry.
/// </summary>
public class ResolveSmartSkills : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private CancellationTokenSource _cts = new();

    [Required]
    public ITaskItem[] PackageReferences { get; set; } = [];

    [Required]
    public string RegistrySources { get; set; } = string.Empty;

    public string CacheDirectory { get; set; } = string.Empty;

    public bool Verbose { get; set; }

    [Output]
    public ITaskItem[] ResolvedSkills { get; set; } = [];

    public override bool Execute()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            LogDiag(MessageImportance.Low, "ResolveSmartSkills: Starting skill resolution");
            LogDiag(MessageImportance.Low, "  RegistrySources: {0}", RegistrySources);
            LogDiag(MessageImportance.Low, "  CacheDirectory: {0}", CacheDirectory);
            LogDiag(MessageImportance.Low, "  Verbose: {0}", Verbose);

            var packages = new List<Core.Models.DetectedPackage>();
            foreach (var item in PackageReferences)
            {
                var name = item.ItemSpec;
                var version = item.GetMetadata("Version");
                packages.Add(new Core.Models.DetectedPackage(name, version));
                LogDiag(MessageImportance.Low, "  Package: {0} v{1}", name, version);
            }

            if (packages.Count == 0)
            {
                LogDiag(MessageImportance.Normal, "SmartSkills: No packages to resolve");
                return true;
            }

            LogDiag(MessageImportance.Normal,
                "SmartSkills: Resolving skills for {0} package(s) from {1}",
                packages.Count, RegistrySources);

            ResolvedSkills = [];

            sw.Stop();
            LogDiag(MessageImportance.Normal,
                "SmartSkills: Resolved {0} skill(s) in {1}ms",
                ResolvedSkills.Length, sw.ElapsedMilliseconds);
            return true;
        }
        catch (OperationCanceledException)
        {
            LogDiag(MessageImportance.High, "SmartSkills: Skill resolution cancelled after {0}ms", sw.ElapsedMilliseconds);
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
            Log.LogError(subcategory: "SmartSkills", errorCode: "SMSK001",
                helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0,
                endLineNumber: 0, endColumnNumber: 0,
                message: "Failed to resolve skills: {0}", ex.Message);
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
