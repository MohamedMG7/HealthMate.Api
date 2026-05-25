using System.Globalization;
using HealthMate.Fhir.Extensions;
using HealthMate.Fhir.Ports;
using HealthMate.Fhir.Ports.Dtos;
using Microsoft.AspNetCore.Http;

namespace HealthMate.Fhir.Search;

public sealed class PatientSearchParser
{
    private static readonly HashSet<string> SupportedParams =
    [
        "_id",
        "_lastUpdated",
        "name",
        "identifier",
        "birthdate",
        "gender",
        "_sort",
        "_count",
        "_offset",
        "_format"
    ];

    public FhirPatientSearchQuery Parse(IQueryCollection query)
    {
        foreach (var key in query.Keys)
        {
            var baseKey = key.Split(':', 2)[0];
            if (!SupportedParams.Contains(baseKey))
            {
                throw new FhirSearchParseException($"Unsupported Patient search parameter '{key}'.", key);
            }
        }

        var ids = ParseCsv(query, "_id");
        var lastUpdated = ParseDateTimeFilters(query, "_lastUpdated");
        var name = ParseName(query);
        var identifier = ParseIdentifier(query);
        var birthDate = ParseDateFilters(query, "birthdate");
        var gender = ParseToken(query, "gender")?.ToLowerInvariant();
        var sorts = ParseSort(query);
        var count = ParseCount(query);
        var offset = ParseOffset(query);

        return new FhirPatientSearchQuery(
            ids,
            lastUpdated,
            name,
            identifier,
            birthDate,
            gender,
            sorts,
            count,
            offset);
    }

    private static IReadOnlyList<string> ParseCsv(IQueryCollection query, string key)
    {
        if (!query.TryGetValue(key, out var values))
        {
            return [];
        }

        return values
            .SelectMany(static value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToArray();
    }

    private static FhirStringSearch? ParseName(IQueryCollection query)
    {
        if (query.Keys.Any(static k => k.StartsWith("name:", StringComparison.OrdinalIgnoreCase) && k != "name:exact"))
        {
            throw new FhirSearchParseException("Only the name:exact modifier is supported.", "name");
        }

        if (query.TryGetValue("name:exact", out var exactValues))
        {
            var value = exactValues.FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? null : new FhirStringSearch(value, Exact: true);
        }

        if (query.TryGetValue("name", out var values))
        {
            var value = values.FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? null : new FhirStringSearch(value, Exact: false);
        }

        return null;
    }

    private static FhirTokenSearch? ParseIdentifier(IQueryCollection query)
    {
        var token = ParseToken(query, "identifier");
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('|', 2);
        if (parts.Length == 1)
        {
            return new FhirTokenSearch(null, parts[0]);
        }

        if (!string.IsNullOrWhiteSpace(parts[0]) && parts[0] != HealthMateExtensionUrls.EgyptianNationalIdSystem)
        {
            throw new FhirSearchParseException("Unsupported identifier system for Patient.identifier.", "identifier");
        }

        if (string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new FhirSearchParseException("Patient.identifier token value is required.", "identifier");
        }

        return new FhirTokenSearch(string.IsNullOrWhiteSpace(parts[0]) ? null : parts[0], parts[1]);
    }

    private static string? ParseToken(IQueryCollection query, string key)
    {
        if (query.Keys.Any(k => k.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase)))
        {
            throw new FhirSearchParseException($"Search parameter '{key}' does not support modifiers.", key);
        }

        return query.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;
    }

    private static IReadOnlyList<FhirDateTimeFilter> ParseDateTimeFilters(IQueryCollection query, string key)
    {
        if (!query.TryGetValue(key, out var values))
        {
            return [];
        }

        return values.Select(value =>
        {
            var (prefix, literal) = SplitPrefix(value);
            if (!DateTimeOffset.TryParse(literal, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                throw new FhirSearchParseException($"Search parameter '{key}' must be a valid dateTime.", key);
            }

            return new FhirDateTimeFilter(prefix, parsed.ToUniversalTime());
        }).ToArray();
    }

    private static IReadOnlyList<FhirDateFilter> ParseDateFilters(IQueryCollection query, string key)
    {
        if (!query.TryGetValue(key, out var values))
        {
            return [];
        }

        return values.Select(value =>
        {
            var (prefix, literal) = SplitPrefix(value);
            if (!DateOnly.TryParseExact(literal, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                throw new FhirSearchParseException($"Search parameter '{key}' must be a yyyy-MM-dd date.", key);
            }

            return new FhirDateFilter(prefix, parsed);
        }).ToArray();
    }

    private static IReadOnlyList<FhirSort> ParseSort(IQueryCollection query)
    {
        if (!query.TryGetValue("_sort", out var values))
        {
            return FhirPatientSearchQuery.Empty.Sorts;
        }

        var sorts = values
            .SelectMany(static value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Select(sort =>
            {
                var descending = sort.StartsWith("-", StringComparison.Ordinal);
                var field = descending ? sort[1..] : sort;
                if (field is not ("name" or "birthdate" or "_lastUpdated"))
                {
                    throw new FhirSearchParseException($"Unsupported Patient _sort field '{field}'.", "_sort");
                }

                return new FhirSort(field, descending);
            })
            .ToArray();

        return sorts.Length == 0 ? FhirPatientSearchQuery.Empty.Sorts : sorts;
    }

    private static int ParseCount(IQueryCollection query)
    {
        if (!query.TryGetValue("_count", out var values) || !int.TryParse(values.FirstOrDefault(), out var count))
        {
            return FhirPatientSearchQuery.Empty.Count;
        }

        return Math.Clamp(count, 1, 100);
    }

    private static int ParseOffset(IQueryCollection query)
    {
        if (!query.TryGetValue("_offset", out var values) || !int.TryParse(values.FirstOrDefault(), out var offset))
        {
            return 0;
        }

        if (offset < 0)
        {
            throw new FhirSearchParseException("_offset must be non-negative.", "_offset");
        }

        return offset;
    }

    private static (FhirSearchPrefix Prefix, string Literal) SplitPrefix(string value)
    {
        if (value.Length >= 2)
        {
            var candidate = value[..2].ToLowerInvariant();
            if (candidate is "gt" or "lt" or "ge" or "le" or "eq")
            {
                var prefix = candidate switch
                {
                    "gt" => FhirSearchPrefix.Gt,
                    "lt" => FhirSearchPrefix.Lt,
                    "ge" => FhirSearchPrefix.Ge,
                    "le" => FhirSearchPrefix.Le,
                    _ => FhirSearchPrefix.Eq
                };
                return (prefix, value[2..]);
            }
        }

        return (FhirSearchPrefix.Eq, value);
    }
}
