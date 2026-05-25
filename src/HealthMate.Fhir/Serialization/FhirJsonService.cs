using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace HealthMate.Fhir.Serialization;

public sealed class FhirJsonService
{
    private readonly FhirJsonParser parser = new(new ParserSettings
    {
        AcceptUnknownMembers = false,
        AllowUnrecognizedEnums = false
    });

    private readonly FhirJsonSerializer serializer = new();

    public string Serialize(Resource resource) => serializer.SerializeToString(resource);

    public T Parse<T>(string json)
        where T : Resource
    {
        return parser.Parse<T>(json);
    }

    public Resource Parse(string json) => parser.Parse<Resource>(json);
}
