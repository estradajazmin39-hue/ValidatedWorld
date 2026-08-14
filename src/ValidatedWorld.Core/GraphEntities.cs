using System.Collections.ObjectModel;

namespace ValidatedWorld.Core;

public enum ReviewDirection
{
    None,
    SourceToTarget,
    TargetToSource,
    Both,
}

public readonly struct GraphAttribute : IEquatable<GraphAttribute>
{
    public GraphAttribute(string name, GraphValue value)
    {
        Name = GraphTextValidation.ValidateMetadata(name, nameof(name));
        if (!value.IsInitialized)
        {
            throw new ArgumentException("The graph value is uninitialized.", nameof(value));
        }

        Value = value;
    }

    public string Name { get; }

    public GraphValue Value { get; }

    public bool Equals(GraphAttribute other) =>
        StringComparer.Ordinal.Equals(Name, other.Name) && Value == other.Value;

    public override bool Equals(object? obj) => obj is GraphAttribute other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        StringComparer.Ordinal.GetHashCode(Name),
        Value);

    public static bool operator ==(GraphAttribute left, GraphAttribute right) => left.Equals(right);

    public static bool operator !=(GraphAttribute left, GraphAttribute right) => !left.Equals(right);
}

public sealed class GraphNode : IEquatable<GraphNode>
{
    public GraphNode(
        EntityId id,
        string text,
        string? kind = null,
        IEnumerable<string>? tags = null,
        IEnumerable<KeyValuePair<string, GraphValue>>? attributes = null)
    {
        if (!id.IsInitialized)
        {
            throw new ArgumentException("The node ID is uninitialized.", nameof(id));
        }

        Id = id;
        Text = GraphTextValidation.Validate(text, nameof(text), allowEmpty: false);
        Kind = kind is null ? null : GraphTextValidation.ValidateMetadata(kind, nameof(kind));
        Tags = new ReadOnlyCollection<string>(GraphCollections.CanonicalTags(tags));
        Attributes = new ReadOnlyCollection<GraphAttribute>(GraphCollections.CanonicalAttributes(attributes));
    }

    public EntityId Id { get; }

    public string Text { get; }

    public string? Kind { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<GraphAttribute> Attributes { get; }

    public bool TryGetAttribute(string name, out GraphValue value)
    {
        foreach (var attribute in Attributes)
        {
            if (StringComparer.Ordinal.Equals(attribute.Name, name))
            {
                value = attribute.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public bool Equals(GraphNode? other)
    {
        return other is not null && Id == other.Id && StringComparer.Ordinal.Equals(Text, other.Text) &&
            StringComparer.Ordinal.Equals(Kind, other.Kind) && Tags.SequenceEqual(other.Tags, StringComparer.Ordinal) &&
            Attributes.SequenceEqual(other.Attributes);
    }

    public override bool Equals(object? obj) => Equals(obj as GraphNode);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Text, StringComparer.Ordinal);
        hash.Add(Kind, StringComparer.Ordinal);
        foreach (var tag in Tags) hash.Add(tag, StringComparer.Ordinal);
        foreach (var attribute in Attributes) hash.Add(attribute);
        return hash.ToHashCode();
    }
}

public sealed class GraphEdge : IEquatable<GraphEdge>
{
    public GraphEdge(
        EntityId id,
        EntityId source,
        EntityId target,
        string relationship,
        ReviewDirection reviewDirection,
        string? rationale = null,
        IEnumerable<string>? tags = null,
        IEnumerable<KeyValuePair<string, GraphValue>>? attributes = null)
    {
        if (!id.IsInitialized || !source.IsInitialized || !target.IsInitialized)
        {
            throw new ArgumentException("Edge IDs and endpoints must be initialized.");
        }

        Id = id;
        Source = source;
        Target = target;
        Relationship = GraphTextValidation.ValidateRelationship(relationship, nameof(relationship));
        if (!Enum.IsDefined(reviewDirection))
        {
            throw new ArgumentOutOfRangeException(nameof(reviewDirection));
        }

        ReviewDirection = reviewDirection;
        Rationale = rationale is null ? null : GraphTextValidation.Validate(rationale, nameof(rationale));
        Tags = new ReadOnlyCollection<string>(GraphCollections.CanonicalTags(tags));
        Attributes = new ReadOnlyCollection<GraphAttribute>(GraphCollections.CanonicalAttributes(attributes));
    }

    public EntityId Id { get; }

    public EntityId Source { get; }

    public EntityId Target { get; }

    public string Relationship { get; }

    public ReviewDirection ReviewDirection { get; }

    public string? Rationale { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<GraphAttribute> Attributes { get; }

    public bool Equals(GraphEdge? other)
    {
        return other is not null && Id == other.Id && Source == other.Source && Target == other.Target &&
            StringComparer.Ordinal.Equals(Relationship, other.Relationship) &&
            ReviewDirection == other.ReviewDirection && StringComparer.Ordinal.Equals(Rationale, other.Rationale) &&
            Tags.SequenceEqual(other.Tags, StringComparer.Ordinal) && Attributes.SequenceEqual(other.Attributes);
    }

    public override bool Equals(object? obj) => Equals(obj as GraphEdge);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Source);
        hash.Add(Target);
        hash.Add(Relationship, StringComparer.Ordinal);
        hash.Add(ReviewDirection);
        hash.Add(Rationale, StringComparer.Ordinal);
        foreach (var tag in Tags) hash.Add(tag, StringComparer.Ordinal);
        foreach (var attribute in Attributes) hash.Add(attribute);
        return hash.ToHashCode();
    }
}
