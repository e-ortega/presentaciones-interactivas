using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Server.Features.AdminAccess;

public class AdminAccessOptions
{
    public const string SectionName = "AdminAccess";

    /// <summary>
    /// Hex SHA-256 of the access key. Lives in user-secrets locally and in a Container Apps
    /// secret in Azure. Never in the repository (D-45).
    /// </summary>
    public string? KeySha256 { get; set; }
}

/// <summary>Checks the teacher access key against its configured hash in constant time.</summary>
public class AdminKeyVerifier
{
    private readonly byte[]? _expectedHash;

    public AdminKeyVerifier(IOptions<AdminAccessOptions> options)
    {
        _expectedHash = ParseHash(options.Value.KeySha256);
    }

    /// <summary>False when no valid hash is configured: the teacher area stays closed.</summary>
    public bool IsConfigured => _expectedHash is not null;

    public bool Verify(string? key)
    {
        if (_expectedHash is null || string.IsNullOrEmpty(key))
        {
            return false;
        }

        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return CryptographicOperations.FixedTimeEquals(actualHash, _expectedHash);
    }

    private static byte[]? ParseHash(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromHexString(hex.Trim());
            return bytes.Length == SHA256.HashSizeInBytes ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
