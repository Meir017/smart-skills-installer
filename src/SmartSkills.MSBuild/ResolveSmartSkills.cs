using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SmartSkills.Core.Scanning;

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
        try
        {
            Log.LogMessage(MessageImportance.Low, "ResolveSmartSkills: Starting skill resolution");

            var packages = new List<Core.Models.DetectedPackage>();
            foreach (var item in PackageReferences)
            {
                var name = item.ItemSpec;
                var version = item.GetMetadata("Version");
                packages.Add(new Core.Models.DetectedPackage(name, version));
                Log.LogMessage(MessageImportance.Low, "  Package: {0} v{1}", name, version);
            }

            if (packages.Count == 0)
            {
                Log.LogMessage(MessageImportance.Normal, "SmartSkills: No packages to resolve");
                return true;
            }

            Log.LogMessage(MessageImportance.Normal,
                "SmartSkills: Resolving skills for {0} package(s) from {1}",
                packages.Count, RegistrySources);

            // Actual resolution would fetch registry and match here
            // For now, output placeholder items
            ResolvedSkills = [];
            return true;
        }
        catch (OperationCanceledException)
        {
            Log.LogMessage(MessageImportance.High, "SmartSkills: Skill resolution cancelled");
            return false;
        }
        catch (Exception ex)
        {
            Log.LogError("SMSK001", "", "", "", 0, 0, 0, 0,
                "SmartSkills: Failed to resolve skills: {0}", ex.Message);
            return false;
        }
    }

    public void Cancel() => _cts.Cancel();
}
