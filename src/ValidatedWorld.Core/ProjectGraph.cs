using System.Collections.ObjectModel;

namespace ValidatedWorld.Core;

/// <summary>An immutable snapshot of the current or proposed graph.</summary>
public sealed class ProjectGraph : IEquatable<ProjectGraph>
{
    public ProjectGraph(
        ProjectId projectId,
        string title,
        EntityId purposeNodeId,
        IEnumerable<GraphNode> nodes,
        IEnumerable<GraphEdge> edges)
    {
        if (!projectId.IsInitialized || !purposeNodeId.IsInitialized)
        {
            throw new ArgumentException("Project and purpose IDs must be initialized.");
        }

        ProjectId = projectId;
        Title = GraphTextValidation.Validate(title, nameof(title), allowEmpty: false);
        PurposeNodeId = purposeNodeId;
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        Nodes = new ReadOnlyCollection<GraphNode>(nodes.OrderBy(node => node.Id).ToArray());
        Edges = new ReadOnlyCollection<GraphEdge>(edges.OrderBy(edge => edge.Id).ToArray());
    }

    public ProjectId ProjectId { get; }

    public string Title { get; }

    public EntityId PurposeNodeId { get; }

    public IReadOnlyList<GraphNode> Nodes { get; }

    public IReadOnlyList<GraphEdge> Edges { get; }

    public bool Equals(ProjectGraph? other)
    {
        return other is not null && ProjectId == other.ProjectId &&
            StringComparer.Ordinal.Equals(Title, other.Title) && PurposeNodeId == other.PurposeNodeId &&
            Nodes.SequenceEqual(other.Nodes) && Edges.SequenceEqual(other.Edges);
    }

    public override bool Equals(object? obj) => Equals(obj as ProjectGraph);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ProjectId);
        hash.Add(Title, StringComparer.Ordinal);
        hash.Add(PurposeNodeId);
        foreach (var node in Nodes) hash.Add(node);
        foreach (var edge in Edges) hash.Add(edge);
        return hash.ToHashCode();
    }
}
