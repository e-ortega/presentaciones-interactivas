using System.Reflection;
using NetArchTest.Rules;
using Server.Domain;

namespace Server.Tests.Architecture;

/// <summary>
/// Decisions from docs/arquitectura.md written as tests: breaking one fails the build
/// instead of waiting for someone to notice in a review.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly ServerAssembly = typeof(Presentation).Assembly;

    private const string Domain = "Server.Domain";
    private const string Features = "Server.Features";
    private const string Infrastructure = "Server.Infrastructure";
    private const string Storage = "Server.Infrastructure.Storage";

    [Fact]
    public void Domain_depends_on_nothing_else_in_the_app_and_on_no_framework()
    {
        // Entities hold the rules; they must be testable and readable without a database or a web server.
        var result = Types.InAssembly(ServerAssembly)
            .That().ResideInNamespace(Domain)
            .ShouldNot().HaveDependencyOnAny(Features, Infrastructure, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_features()
    {
        // Features use the infrastructure, never the other way around: storage and data access
        // stay reusable by every feature.
        var result = Types.InAssembly(ServerAssembly)
            .That().ResideInNamespace(Infrastructure)
            .ShouldNot().HaveDependencyOnAny(Features)
            .GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Only_the_storage_implementation_knows_about_azure_blob_storage()
    {
        // Everything else talks to IFileStorage, so local disk and Blob Storage stay interchangeable.
        var result = Types.InAssembly(ServerAssembly)
            .That().DoNotResideInNamespace(Storage)
            .ShouldNot().HaveDependencyOnAny("Azure.Storage")
            .GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void The_rules_above_would_notice_a_violation()
    {
        // Guards against a rule that passes only because it matches no types or detects nothing:
        // the storage implementation does depend on Azure, and the detector must see it.
        var result = Types.InAssembly(ServerAssembly)
            .That().ResideInNamespace(Storage)
            .ShouldNot().HaveDependencyOnAny("Azure.Storage")
            .GetResult();

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.FailingTypes, type => type.Name == "BlobFileStorage");
    }

    [Fact]
    public void Entities_cannot_be_changed_from_outside_except_through_their_methods()
    {
        // D-48: an entity protects its own rules. A public setter would let any endpoint skip them.
        var publicSetters = ServerAssembly.GetTypes()
            .Where(type => type.Namespace == Domain && type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        Assert.Empty(publicSetters);
    }

    [Fact]
    public void There_are_entities_to_check()
    {
        var entities = ServerAssembly.GetTypes().Where(type => type.Namespace == Domain && type.IsClass).ToList();

        Assert.Contains(typeof(Presentation), entities);
        Assert.True(entities.Count >= 9, $"Expected the phase 1 entities in {Domain}, found {entities.Count}.");
    }

    private static void AssertPasses(NetArchTest.Rules.TestResult result)
    {
        var failures = (result.FailingTypes ?? []).Select(type => $"{type.FullName}: {type.Explanation}");
        Assert.True(result.IsSuccessful, "Types that break the rule:\n" + string.Join("\n", failures));
    }
}
