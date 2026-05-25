using HealthMate.Infrastructure.DTO.MachineLearningDto;
using HealthMate.Infrastructure.Repositories.ObservationRepos;

namespace HealthMate.Application.Manager.MachineLearningManager;

public class MachineLearningManager : IMachineLearningManager
{
    private readonly IObservationRepo _observationRepo;
    private readonly IMlGateway _mlGateway;

    public MachineLearningManager(IObservationRepo observationRepo, IMlGateway mlGateway)
    {
        _observationRepo = observationRepo;
        _mlGateway = mlGateway;
    }

    public async Task<MachineLearningResponse> CheckAnimea(int patientId, CancellationToken cancellationToken = default)
    {
        var cbc = await _observationRepo.GetRecentCBCTestForML(patientId);

        if (IsEmpty(cbc))
        {
            // Surface the absence of data instead of returning a false negative.
            // The previous implementation silently returned Animea=false on any
            // missing/failed input, which is medically dangerous.
            throw new NoCbcDataException(patientId);
        }

        var request = new AnemiaGatewayRequest(
            Hb: cbc.Hemoglobin,
            Rbc: cbc.RedBloodCells,
            Pcv: cbc.PackedCellVolume,
            Mch: cbc.MeanCorpuscularHemoglobin,
            Mchc: cbc.MeanCorpuscularHemoglobinConcentration);

        var response = await _mlGateway.PredictAnemiaAsync(request, cancellationToken);

        return new MachineLearningResponse
        {
            Animea = response.Anemia,
            Confidence = response.Confidence,
            ModelVersion = response.ModelVersion,
        };
    }

    private static bool IsEmpty(AnimeaMLDto cbc) =>
        cbc.Hemoglobin == 0
        && cbc.RedBloodCells == 0
        && cbc.PackedCellVolume == 0
        && cbc.MeanCorpuscularHemoglobin == 0
        && cbc.MeanCorpuscularHemoglobinConcentration == 0;
}
