using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Configuration;

/// <summary>
/// Resolves credentials from environment variables and secure storage.
/// Credentials are never stored in plaintext config files.
/// </summary>
public sealed class CredentialResolver
{
    private readonly ILogger<CredentialResolver> _logger;

    public CredentialResolver(ILogger<CredentialResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolve a credential key to an actual token value.
    /// Supports:
    ///   - Environment variable: "env:VARIABLE_NAME"
    ///   - Direct environment variable lookup by key name
    /// </summary>
    public string? Resolve(string? credentialKey)
    {
        if (string.IsNullOrEmpty(credentialKey))
            return null;

        // env: prefix for explicit environment variable reference
        if (credentialKey.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var envVar = credentialKey[4..];
            var value = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogWarning("Environment variable '{EnvVar}' not set for credential key '{Key}'", envVar, credentialKey);
            }
            return value;
        }

        // Try as environment variable name directly
        var envValue = Environment.GetEnvironmentVariable($"SMARTSKILLS_{credentialKey.ToUpperInvariant().Replace('-', '_')}");
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        _logger.LogWarning("Could not resolve credential for key '{Key}'. Set environment variable 'SMARTSKILLS_{EnvKey}'.",
            credentialKey, credentialKey.ToUpperInvariant().Replace('-', '_'));

        return null;
    }
}
