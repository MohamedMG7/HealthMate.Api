using FluentAssertions;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Encounters.Handlers;
using HealthMate.Application.Encounters.Queries;
using HealthMate.Application.Encounters.Services;
using HealthMate.Domain.Aggregates.Encounter;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HealthMate.Tests.Application.Encounters;

public sealed class ListPatientEncountersQueryHandlerTests
{
    private static readonly EncounterHistoryPage EmptyPage =
        new([], 1, 20, false);

    private ListPatientEncountersQueryHandler BuildHandler(Mock<IEncounterHistoryReader> readerMock)
        => new(readerMock.Object, NullLogger<ListPatientEncountersQueryHandler>.Instance);

    [Fact]
    public async Task Handler_clamps_page_below_1_to_1()
    {
        var reader = new Mock<IEncounterHistoryReader>();
        reader.Setup(r => r.ListForPatientAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(EmptyPage);

        await BuildHandler(reader).HandleAsync(new ListPatientEncountersQuery(1, 0, 20), default);

        reader.Verify(r => r.ListForPatientAsync(1, 1, 20, default), Times.Once);
    }

    [Fact]
    public async Task Handler_clamps_page_size_above_50_to_50()
    {
        var reader = new Mock<IEncounterHistoryReader>();
        reader.Setup(r => r.ListForPatientAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(EmptyPage);

        await BuildHandler(reader).HandleAsync(new ListPatientEncountersQuery(1, 1, 200), default);

        reader.Verify(r => r.ListForPatientAsync(1, 1, 50, default), Times.Once);
    }

    [Fact]
    public async Task Handler_clamps_page_size_below_1_to_1()
    {
        var reader = new Mock<IEncounterHistoryReader>();
        reader.Setup(r => r.ListForPatientAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(EmptyPage);

        await BuildHandler(reader).HandleAsync(new ListPatientEncountersQuery(1, 1, 0), default);

        reader.Verify(r => r.ListForPatientAsync(1, 1, 1, default), Times.Once);
    }

    [Fact]
    public async Task Handler_passes_clamped_values_to_reader()
    {
        var reader = new Mock<IEncounterHistoryReader>();
        reader.Setup(r => r.ListForPatientAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(EmptyPage);

        await BuildHandler(reader).HandleAsync(new ListPatientEncountersQuery(42, -5, 999), default);

        reader.Verify(r => r.ListForPatientAsync(42, 1, 50, default), Times.Once);
    }

    [Fact]
    public async Task Handler_returns_reader_result_unchanged()
    {
        var item = new EncounterHistoryItem(7, DateTime.UtcNow, DateTime.UtcNow, EncounterStatus.Finished, "Headache", 3);
        var expected = new EncounterHistoryPage([item], 2, 10, false);
        var reader = new Mock<IEncounterHistoryReader>();
        reader.Setup(r => r.ListForPatientAsync(1, 2, 10, default))
              .ReturnsAsync(expected);

        var result = await BuildHandler(reader).HandleAsync(new ListPatientEncountersQuery(1, 2, 10), default);

        result.Should().BeSameAs(expected);
    }
}
