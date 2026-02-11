namespace SmartSkills.Core.Scanning;

public static class ProjectDiscovery
{
    /// <summary>
    /// Validates the path exists and contains a .NET project (.csproj) or solution (.sln).
    /// Returns the resolved path to the project/solution file found.
    /// </summary>
    public static string ResolvePath(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            throw new DirectoryNotFoundException($"Path not found: {path}");
        }

        if (File.Exists(path))
        {
            var ext = Path.GetExtension(path);
            if (ext is ".sln" or ".csproj")
            {
                return Path.GetFullPath(path);
            }
            throw new InvalidOperationException($"File is not a .NET project or solution: {path}");
        }

        // Directory: look for .sln first, then .csproj
        var slnFiles = Directory.GetFiles(path, "*.sln");
        if (slnFiles.Length > 0)
        {
            return Path.GetFullPath(slnFiles[0]);
        }

        var csprojFiles = Directory.GetFiles(path, "*.csproj");
        if (csprojFiles.Length > 0)
        {
            return Path.GetFullPath(csprojFiles[0]);
        }

        throw new InvalidOperationException($"No .NET project (.csproj) or solution (.sln) found in: {path}");
    }
}
