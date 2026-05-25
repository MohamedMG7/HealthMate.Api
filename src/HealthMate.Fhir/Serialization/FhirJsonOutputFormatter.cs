using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace HealthMate.Fhir.Serialization;

public sealed class FhirJsonOutputFormatter : TextOutputFormatter
{
    private readonly FhirJsonSerializer serializer = new();

    public FhirJsonOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/fhir+json"));
        SupportedEncodings.Add(Encoding.UTF8);
    }

    protected override bool CanWriteType(Type? type)
    {
        return type is not null && typeof(Resource).IsAssignableFrom(type);
    }

    public override async System.Threading.Tasks.Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        if (context.Object is not Resource resource)
        {
            return;
        }

        var json = serializer.SerializeToString(resource);
        await context.HttpContext.Response.WriteAsync(json, selectedEncoding);
    }
}
