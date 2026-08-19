using System.Collections.ObjectModel;

namespace ValidatedWorld.Core;

public enum GraphEntityKind
{
    Node,
    Edge,
}

public enum GraphOperationKind
{
    Add,
    Replace,
    Remove,
}

/// <summary>A complete in-memory operation over one graph entity.</summary>
public sealed class GraphOperation : IEquatable<GraphOperation>
{
    public GraphOperation(GraphOperationKind kind, GraphNode entity)
        : this(kind, GraphEntityKind.Node, entity?.Id ?? default, entity, null)
    {
    }

    public GraphOperation(GraphOperationKind kind, GraphEdge entity)
        : this(kind, GraphEntityKind.Edge, entity?.Id ?? default, null, entity)
    {
    }

    public GraphOperation(GraphOperationKind kind, GraphEntityKind entityKind, EntityId entityId)
        : this(kind, entityKind, entityId, null, null)
    {
    }

    private GraphOperation(
        GraphOperationKind kind,
        GraphEntityKind entityKind,
        EntityId entityId,
        GraphNode? node,
        GraphEdge? edge)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(entityKind)) throw new ArgumentOutOfRangeException(nameof(entityKind));
        if (!entityId.IsInitialized) throw new ArgumentException("The entity ID must be initialized.", nameof(entityId));

        if (kind == GraphOperationKind.Remove)
        {
            if (node is not null || edge is not null)
            {
                throw new ArgumentException("Remove operations cannot contain an entity.");
            }
        }
        else
        {
            if (entityKind == GraphEntityKind.Node && node is null ||
                entityKind == GraphEntityKind.Edge && edge is null)
            {
                throw new ArgumentException("Add and replace operations require a matching entity.");
            }

            if (node is not null && node.Id != entityId || edge is not null && edge.Id != entityId)
            {
                throw new ArgumentException("The operation ID must match the entity ID.");
            }
        }

        Kind = kind;
        EntityKind = entityKind;
        EntityId = entityId;
        Node = node;
        Edge = edge;
    }

    public GraphOperationKind Kind { get; }

    public GraphEntityKind EntityKind { get; }

    public EntityId EntityId { get; }

    public GraphNode? Node { get; }

    public GraphEdge? Edge { get; }

    public bool IsRemoval => Kind == GraphOperationKind.Remove;

    public static GraphOperation AddNode(GraphNode node) => new(GraphOperationKind.Add, node);

    public static GraphOperation ReplaceNode(GraphNode node) => new(GraphOperationKind.Replace, node);

    public static GraphOperation RemoveNode(EntityId id) =>
        new(GraphOperationKind.Remove, GraphEntityKind.Node, id);

    public static GraphOperation AddEdge(GraphEdge edge) => new(GraphOperationKind.Add, edge);

    public static GraphOperation ReplaceEdge(GraphEdge edge) => new(GraphOperationKind.Replace, edge);

    public static GraphOperation RemoveEdge(EntityId id) =>
        new(GraphOperationKind.Remove, GraphEntityKind.Edge, id);

    public bool Equals(GraphOperation? other) => other is not null &&
        Kind == other.Kind && EntityKind == other.EntityKind && EntityId == other.EntityId &&
        Equals(Node, other.Node) && Equals(Edge, other.Edge);

    public override bool Equals(object? obj) => Equals(obj as GraphOperation);

    public override int GetHashCode() => HashCode.Combine(Kind, EntityKind, EntityId, Node, Edge);
}

/// <summary>
/// The final operation for each entity in one proposed change. Entity IDs share
/// one namespace, so a node/edge collision is also a batch conflict.
/// </summary>
public sealed class GraphOperationBatch : IEquatable<GraphOperationBatch>
{
    public GraphOperationBatch(IEnumerable<GraphOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var values = operations.ToArray();
        if (values.Any(operation => operation is null))
        {
            throw new ArgumentException("An operation batch cannot contain null operations.", nameof(operations));
        }

        var duplicate = values
            .GroupBy(operation => operation.EntityId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Entity '{duplicate.Key.Value}' has more than one final operation.",
                nameof(operations));
        }

        Operations = new ReadOnlyCollection<GraphOperation>(values
            .OrderBy(operation => operation.EntityId)
            .ThenBy(operation => operation.EntityKind)
            .ToArray());
    }

    public IReadOnlyList<GraphOperation> Operations { get; }

    public static GraphOperationBatch Empty { get; } = new([]);

    public bool Equals(GraphOperationBatch? other) => other is not null &&
        Operations.SequenceEqual(other.Operations);

    public override bool Equals(object? obj) => Equals(obj as GraphOperationBatch);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var operation in Operations) hash.Add(operation);
        return hash.ToHashCode();
    }
}
