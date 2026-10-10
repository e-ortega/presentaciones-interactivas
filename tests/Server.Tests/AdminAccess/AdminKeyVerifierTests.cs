using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Server.Features.AdminAccess;

namespace Server.Tests.AdminAccess;

public class AdminKeyVerifierTests
{
    private const string Key = "correct horse battery staple";

    private static AdminKeyVerifier CreateVerifier(string? configuredHash) =>
        new(Options.Create(new AdminAccessOptions { KeySha256 = configuredHash }));

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    [Fact]
    public void Accepts_the_key_that_matches_the_configured_hash()
    {
        Assert.True(CreateVerifier(Sha256Hex(Key)).Verify(Key));
    }

    [Fact]
    public void Accepts_a_lower_case_hash()
    {
        Assert.True(CreateVerifier(Sha256Hex(Key).ToLowerInvariant()).Verify(Key));
    }

    [Theory]
    [InlineData("wrong key")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_any_other_key(string? key)
    {
        Assert.False(CreateVerifier(Sha256Hex(Key)).Verify(key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not hex")]
    [InlineData("ABCD")] // valid hex, wrong length
    public void Rejects_every_key_when_the_hash_is_missing_or_invalid(string? configuredHash)
    {
        var verifier = CreateVerifier(configuredHash);

        Assert.False(verifier.IsConfigured);
        Assert.False(verifier.Verify(Key));
    }
}
