using HealthMate.Fhir.Extensions;
using Hl7.Fhir.Model;

namespace HealthMate.Fhir.Mapping;

public sealed class CapabilityStatementFactory
{
    public CapabilityStatement Build()
    {
        return new CapabilityStatement
        {
            Id = "healthmate-fhir-r4",
            Url = "http://healthmate.app/fhir/CapabilityStatement/healthmate-fhir-r4",
            Version = "1.0.0",
            Name = "HealthMateFhirR4",
            Title = "HealthMate FHIR R4 Patient Facade",
            Status = PublicationStatus.Active,
            Date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            Kind = CapabilityStatementKind.Instance,
            FhirVersion = FHIRVersion.N4_0_1,
            Format = ["json", "application/fhir+json"],
            Rest =
            [
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Resource =
                    [
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "Patient",
                            Profile = "http://hl7.org/fhir/StructureDefinition/Patient",
                            SupportedProfile = [HealthMateExtensionUrls.Governorate],
                            Interaction =
                            [
                                new CapabilityStatement.ResourceInteractionComponent { Code = CapabilityStatement.TypeRestfulInteraction.Read },
                                new CapabilityStatement.ResourceInteractionComponent { Code = CapabilityStatement.TypeRestfulInteraction.Vread },
                                new CapabilityStatement.ResourceInteractionComponent { Code = CapabilityStatement.TypeRestfulInteraction.SearchType },
                                new CapabilityStatement.ResourceInteractionComponent { Code = CapabilityStatement.TypeRestfulInteraction.Create },
                                new CapabilityStatement.ResourceInteractionComponent { Code = CapabilityStatement.TypeRestfulInteraction.Update },
                                new CapabilityStatement.ResourceInteractionComponent { Code = CapabilityStatement.TypeRestfulInteraction.Delete },
                                new CapabilityStatement.ResourceInteractionComponent { Code = CapabilityStatement.TypeRestfulInteraction.HistoryInstance }
                            ],
                            SearchParam =
                            [
                                SearchParam("_id", SearchParamType.Token),
                                SearchParam("_lastUpdated", SearchParamType.Date),
                                SearchParam("name", SearchParamType.String),
                                SearchParam("identifier", SearchParamType.Token),
                                SearchParam("birthdate", SearchParamType.Date),
                                SearchParam("gender", SearchParamType.Token)
                            ],
                            Operation =
                            [
                                new CapabilityStatement.OperationComponent
                                {
                                    Name = "validate",
                                    Definition = "http://hl7.org/fhir/OperationDefinition/Resource-validate"
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    private static CapabilityStatement.SearchParamComponent SearchParam(string name, SearchParamType type)
    {
        return new CapabilityStatement.SearchParamComponent
        {
            Name = name,
            Type = type
        };
    }
}
