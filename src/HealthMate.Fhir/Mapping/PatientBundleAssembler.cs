using HealthMate.Fhir.Ports.Dtos;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.WebUtilities;

namespace HealthMate.Fhir.Mapping;

public sealed class PatientBundleAssembler(PatientResourceMapper mapper)
{
    public Bundle BuildSearchset(FhirPatientSearchResult result, string selfUrl, string resourceBaseUrl)
    {
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = result.Total,
            Link = [new Bundle.LinkComponent { Relation = "self", Url = selfUrl }]
        };

        if (result.Offset > 0)
        {
            var previousOffset = Math.Max(0, result.Offset - result.Count);
            bundle.Link.Add(new Bundle.LinkComponent
            {
                Relation = "prev",
                Url = WithPaging(selfUrl, previousOffset, result.Count)
            });
        }

        if (result.Offset + result.Matches.Count < result.Total)
        {
            bundle.Link.Add(new Bundle.LinkComponent
            {
                Relation = "next",
                Url = WithPaging(selfUrl, result.Offset + result.Count, result.Count)
            });
        }

        foreach (var snapshot in result.Matches)
        {
            var resource = mapper.ToResource(snapshot);
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{resourceBaseUrl}/{snapshot.FhirId}",
                Resource = resource,
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            });
        }

        return bundle;
    }

    public Bundle BuildHistory(FhirPatientHistoryResult result, string selfUrl, string resourceBaseUrl)
    {
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.History,
            Total = result.Total,
            Link = [new Bundle.LinkComponent { Relation = "self", Url = selfUrl }]
        };

        foreach (var entry in result.Entries)
        {
            var resource = mapper.ToResource(entry.Snapshot);
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{resourceBaseUrl}/{entry.Snapshot.FhirId}/_history/{entry.Snapshot.VersionId}",
                Resource = resource,
                Request = new Bundle.RequestComponent
                {
                    Method = ToHttpVerb(entry.Operation),
                    Url = $"Patient/{entry.Snapshot.FhirId}"
                },
                Response = new Bundle.ResponseComponent
                {
                    Status = entry.Operation == FhirHistoryOperation.Create
                        ? "201"
                        : entry.Operation == FhirHistoryOperation.Delete ? "204" : "200",
                    LastModified = entry.Snapshot.LastUpdated,
                    Etag = $"W/\"{entry.Snapshot.VersionId}\""
                }
            });
        }

        return bundle;
    }

    private static Bundle.HTTPVerb ToHttpVerb(FhirHistoryOperation operation)
    {
        return operation switch
        {
            FhirHistoryOperation.Create => Bundle.HTTPVerb.POST,
            FhirHistoryOperation.Delete => Bundle.HTTPVerb.DELETE,
            _ => Bundle.HTTPVerb.PUT
        };
    }

    private static string WithPaging(string selfUrl, int offset, int count)
    {
        var uri = new Uri(selfUrl);
        var query = QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(static p => p.Key, static p => p.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        query["_offset"] = offset.ToString();
        query["_count"] = count.ToString();

        var baseUrl = uri.GetLeftPart(UriPartial.Path);
        return QueryHelpers.AddQueryString(baseUrl, query!);
    }
}
