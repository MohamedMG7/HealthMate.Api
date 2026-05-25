using FluentAssertions;
using HealthMate.Sina.Sessions;

namespace HealthMate.Tests.Sina;

public sealed class SinaSafetyFilterTests
{
    [Fact]
    public void ApplyAssistantGuards_appends_warning_when_clinical_text_has_no_citation()
    {
        var sut = new SinaSafetyFilter();

        var guarded = sut.ApplyAssistantGuards("What is the glucose?", "The glucose is high.");

        guarded.Should().Contain("did not cite a source");
    }

    [Fact]
    public void ApplyAssistantGuards_keeps_cited_text_without_warning()
    {
        var sut = new SinaSafetyFilter();

        var guarded = sut.ApplyAssistantGuards("What is the glucose?", "The glucose is high [#O-1].");

        guarded.Should().NotContain("did not cite a source");
    }

    [Fact]
    public void TryBuildNonClinicalResponse_short_circuits_obvious_non_clinical_input()
    {
        var sut = new SinaSafetyFilter();

        sut.TryBuildNonClinicalResponse("What is the weather today?", out var response).Should().BeTrue();
        response.Should().Contain("clinical decision-support");
    }
}
