namespace ValidatedWorld.Core;

internal static class GraphTextValidation
{
    public static string Validate(string? value, string parameterName, bool allowEmpty = true)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if ((!allowEmpty && string.IsNullOrWhiteSpace(value)) || value.Length > GraphLimits.TextMaxLength)
        {
            throw new ArgumentException(
                allowEmpty
                    ? $"Text cannot exceed {GraphLimits.TextMaxLength} characters."
                    : $"Text must be non-empty and cannot exceed {GraphLimits.TextMaxLength} characters.",
                parameterName);
        }

        return value;
    }

    public static string ValidateMetadata(string? value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value) || value.Length > GraphLimits.MetadataNameMaxLength ||
            value.Any(char.IsControl))
        {
            throw new ArgumentException(
                "Metadata text must be non-empty, bounded, and free of control characters.",
                parameterName);
        }

        return value;
    }

    public static string ValidateRelationship(string? value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value) || value.Length > GraphLimits.RelationshipLabelMaxLength ||
            value.Any(char.IsControl))
        {
            throw new ArgumentException("A relationship label must be non-empty and bounded.", parameterName);
        }

        return value;
    }
}

internal static class GraphCollections
{
    public static string[] CanonicalTags(IEnumerable<string>? tags)
    {
        if (tags is null) return [];

        var values = tags.Select((tag, index) => GraphTextValidation.ValidateMetadata(tag, $"tags[{index}]"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        for (var i = 1; i < values.Length; i++)
        {
            if (StringComparer.Ordinal.Equals(values[i - 1], values[i]))
            {
                throw new ArgumentException($"Duplicate tag '{values[i]}'.", nameof(tags));
            }
        }

        return values;
    }

    public static GraphAttribute[] CanonicalAttributes(
        IEnumerable<KeyValuePair<string, GraphValue>>? attributes)
    {
        if (attributes is null) return [];

        var values = attributes.Select(pair => new GraphAttribute(pair.Key, pair.Value))
            .OrderBy(attribute => attribute.Name, StringComparer.Ordinal)
            .ToArray();
        for (var i = 1; i < values.Length; i++)
        {
            if (StringComparer.Ordinal.Equals(values[i - 1].Name, values[i].Name))
            {
                throw new ArgumentException($"Duplicate attribute '{values[i].Name}'.", nameof(attributes));
            }
        }

        return values;
    }
}
