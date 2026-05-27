using FluentAssertions;
using HealthMate.Api;
using HealthMate.Application;
using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Fhir.Serialization;
using HealthMate.Sina.Sessions;
using NetArchTest.Rules;

namespace HealthMate.Tests.Architecture;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_does_not_reference_any_other_project()
    {
        AssertDoesNotReferenceAssemblies(
            typeof(Patient).Assembly,
            "HealthMate.Application",
            "HealthMate.Application.Abstractions",
            "HealthMate.Api",
            "HealthMate.Infrastructure",
            "HealthMate.Fhir",
            "HealthMate.Sina");
    }

    [Fact]
    public void ApplicationAbstractions_does_not_reference_any_other_project()
    {
        AssertDoesNotReferenceAssemblies(
            typeof(IEmailService).Assembly,
            "HealthMate.Application",
            "HealthMate.Api",
            "HealthMate.Infrastructure",
            "HealthMate.Fhir",
            "HealthMate.Sina");
    }

    [Fact]
    public void Sina_does_not_reference_Application_or_Infrastructure()
    {
        AssertNoDependency(typeof(SinaManager).Assembly, "HealthMate.Application", "HealthMate.Infrastructure");
    }

    [Fact]
    public void Fhir_does_not_reference_Application_or_Infrastructure()
    {
        AssertNoDependency(typeof(FhirJsonService).Assembly, "HealthMate.Application", "HealthMate.Infrastructure");
    }

    [Fact]
    public void Application_does_not_reference_Infrastructure()
    {
        AssertNoDependency(typeof(DependencyInjection).Assembly, "HealthMate.Infrastructure");
    }

    [Fact]
    public void Api_does_not_reference_Infrastructure_outside_composition_root()
    {
        var assembly = typeof(Program).Assembly;

        AssertNoDependency(
            Types.InAssembly(assembly).That().ResideInNamespace("HealthMate.Api.Controllers"),
            "HealthMate.Infrastructure");
        AssertNoDependency(
            Types.InAssembly(assembly).That().ResideInNamespace("HealthMate.Api.Middleware"),
            "HealthMate.Infrastructure");
    }

    private static void AssertNoDependency(System.Reflection.Assembly assembly, params string[] forbiddenNamespaces)
    {
        foreach (var forbiddenNamespace in forbiddenNamespaces)
        {
            AssertNoDependency(Types.InAssembly(assembly), forbiddenNamespace);
        }
    }

    private static void AssertDoesNotReferenceAssemblies(System.Reflection.Assembly assembly, params string[] forbiddenAssemblyNames)
    {
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var forbiddenAssemblyName in forbiddenAssemblyNames)
        {
            references.Should().NotContain(forbiddenAssemblyName, $"{assembly.GetName().Name} must not reference {forbiddenAssemblyName}");
        }
    }

    private static void AssertNoDependency(PredicateList types, string forbiddenNamespace)
    {
        var result = types.Should().NotHaveDependencyOn(forbiddenNamespace).GetResult();
        result.IsSuccessful.Should().BeTrue($"types must not depend on {forbiddenNamespace}");
    }

    private static void AssertNoDependency(Types types, string forbiddenNamespace)
    {
        var result = types.Should().NotHaveDependencyOn(forbiddenNamespace).GetResult();
        result.IsSuccessful.Should().BeTrue($"types must not depend on {forbiddenNamespace}");
    }
}
