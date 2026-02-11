using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Fetching;

/// <summary>
/// Resolves ADO credentials using priority: explicit token > env var > Azure CLI.
/// </summary>
public class AdoCredentialProvider
{
    private readonly ILogger _logger;

    public AdoCredentialProvider(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolves a Bearer token for ADO API calls.
    /// Priority: explicitToken > ADO_TOKEN env var > Azure CLI.
    /// </summary>
    public string ResolveToken(string? explicitToken = null)
    {
        // 1. Explicit token
        if (!string.IsNullOrWhiteSpace(explicitToken))
        {
            _logger.LogDebug("Using explicitly provided ADO token");
            return explicitToken;
        }

        // 2. Environment variable
        var envToken = Environment.GetEnvironmentVariable("ADO_TOKEN");
        if (!string.IsNullOrWhiteSpace(envToken))
        {
            _logger.LogDebug("Using ADO token from ADO_TOKEN environment variable");
            return envToken;
        }

        // 3. Azure CLI
        var cliToken = TryGetAzureCliToken();
        if (cliToken is not null)
        {
            _logger.LogDebug("Using ADO token from Azure CLI credential");
            return cliToken;
        }

        throw new InvalidOperationException(
            "No ADO credentials found. Provide a token via --ado-token, ADO_TOKEN environment variable, or log in with 'az login'.");
    }

    private string? TryGetAzureCliToken()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "az",
                Arguments = "account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
                return null;

            var token = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(TimeSpan.FromSeconds(10));

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(token) ? token : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Azure CLI not available: {Error}", ex.Message);
            return null;
        }
    }
}
