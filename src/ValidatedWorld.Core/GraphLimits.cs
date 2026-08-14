namespace ValidatedWorld.Core;

/// <summary>Conservative bounds used by the in-memory graph model.</summary>
public static class GraphLimits
{
    public const int IdentifierMaxLength = 256;
    public const int TextMaxLength = 16_384;
    public const int RelationshipLabelMaxLength = 1_024;
    public const int MetadataNameMaxLength = 256;
    public const int DecimalMaxLength = 256;
}
