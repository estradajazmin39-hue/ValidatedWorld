using System.Collections.ObjectModel;
using ValidatedWorld.Core;

namespace ValidatedWorld.Validation;

/// <summary>A review arc expanded from an edge's declared review direction.</summary>
public sealed record ReviewArc(EntityId EdgeId, EntityId From, EntityId To);

/// <summary>
/// Deterministic indexes over an immutable graph. The index remains usable for
/// malformed graphs so that validation can report the actual structural errors.
/// </summary>
public sealed class GraphIndex
{
    private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<GraphNode>> _nodesByIdAll;
    private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> _edgesByIdAll;
    private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> _edgesBySource;
    private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> _edgesByTarget;
    private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> _scopeParentsByChild;
    private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<EntityId>> _scopeChildrenByParent;
    private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<ReviewArc>> _reviewArcsBySource;

    public GraphIndex(ProjectGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        Graph = graph;

        _nodesByIdAll = GroupById(graph.Nodes, node => node.Id);
        _edgesByIdAll = GroupById(graph.Edges, edge => edge.Id);
        NodesById = UniqueEntries(_nodesByIdAll);
        EdgesById = UniqueEntries(_edgesByIdAll);
        _edgesBySource = GroupById(graph.Edges, edge => edge.Source);
        _edgesByTarget = GroupById(graph.Edges, edge => edge.Target);
        _scopeParentsByChild = GroupById(
            graph.Edges.Where(IsScopeParent),
            edge => edge.Source);
        _scopeChildrenByParent = BuildScopeChildren(_scopeParentsByChild);

        var arcs = graph.Edges
            .Where(edge => !IsScopeParent(edge) && edge.ReviewDirection != ReviewDirection.None)
            .SelectMany(ExpandReviewArcs)
            .OrderBy(arc => arc.From)
            .ThenBy(arc => arc.To)
            .ThenBy(arc => arc.EdgeId)
            .ToArray();
        _reviewArcsBySource = GroupById(arcs, arc => arc.From);
        ReviewArcs = new ReadOnlyCollection<ReviewArc>(arcs);
    }

    public ProjectGraph Graph { get; }

    /// <summary>Unique node IDs only. Duplicate IDs are omitted from this map.</summary>
    public IReadOnlyDictionary<EntityId, GraphNode> NodesById { get; }

    /// <summary>Unique edge IDs only. Duplicate IDs are omitted from this map.</summary>
    public IReadOnlyDictionary<EntityId, GraphEdge> EdgesById { get; }

    /// <summary>All node entries grouped by ID, including duplicate entries.</summary>
    public IReadOnlyDictionary<EntityId, IReadOnlyList<GraphNode>> NodesByIdIncludingDuplicates => _nodesByIdAll;

    /// <summary>All edge entries grouped by ID, including duplicate entries.</summary>
    public IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> EdgesByIdIncludingDuplicates => _edgesByIdAll;

    public IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> EdgesBySource => _edgesBySource;

    public IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> EdgesByTarget => _edgesByTarget;

    /// <summary>Reserved scope-parent edges grouped by child node.</summary>
    public IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> ScopeParentsByChild => _scopeParentsByChild;

    public IReadOnlyDictionary<EntityId, IReadOnlyList<EntityId>> ScopeChildrenByParent => _scopeChildrenByParent;

    /// <summary>All non-scope review arcs in deterministic order.</summary>
    public IReadOnlyList<ReviewArc> ReviewArcs { get; }

    public IReadOnlyList<GraphEdge> GetEdgesFrom(EntityId source) => GetOrEmpty(_edgesBySource, source);

    public IReadOnlyList<GraphEdge> GetEdgesTo(EntityId target) => GetOrEmpty(_edgesByTarget, target);

    public IReadOnlyList<GraphEdge> GetScopeParentEdges(EntityId child) =>
        GetOrEmpty(_scopeParentsByChild, child);

    public IReadOnlyList<EntityId> GetScopeChildren(EntityId parent) =>
        GetOrEmpty(_scopeChildrenByParent, parent);

    public IReadOnlyList<ReviewArc> GetReviewArcsFrom(EntityId source) =>
        GetOrEmpty(_reviewArcsBySource, source);

    /// <summary>Returns descendants in breadth-first, ordinal ID order.</summary>
    public IReadOnlyList<EntityId> GetScopeDescendants(EntityId ancestor)
    {
        var result = new List<EntityId>();
        var seen = new HashSet<EntityId>();
        var queue = new Queue<EntityId>(GetScopeChildren(ancestor));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!seen.Add(current)) continue;

            result.Add(current);
            foreach (var child in GetScopeChildren(current)) queue.Enqueue(child);
        }

        return new ReadOnlyCollection<EntityId>(result);
    }

    /// <summary>
    /// Returns a node followed by its scope parents. On malformed graphs the
    /// path stops at a missing/ambiguous parent or a repeated node.
    /// </summary>
    public IReadOnlyList<EntityId> GetScopeUpstreamPath(EntityId node)
    {
        var path = new List<EntityId>();
        var seen = new HashSet<EntityId>();
        var current = node;
        while (seen.Add(current))
        {
            path.Add(current);
            var parents = GetScopeParentEdges(current);
            if (parents.Count != 1) break;
            current = parents[0].Target;
        }

        return new ReadOnlyCollection<EntityId>(path);
    }

    internal static bool IsScopeParent(GraphEdge edge) =>
        StringComparer.Ordinal.Equals(edge.Relationship, "scope-parent");

    private static IEnumerable<ReviewArc> ExpandReviewArcs(GraphEdge edge)
    {
        return edge.ReviewDirection switch
        {
            ReviewDirection.SourceToTarget => [new(edge.Id, edge.Source, edge.Target)],
            ReviewDirection.TargetToSource => [new(edge.Id, edge.Target, edge.Source)],
            ReviewDirection.Both =>
            [
                new(edge.Id, edge.Source, edge.Target),
                new(edge.Id, edge.Target, edge.Source),
            ],
            _ => [],
        };
    }

    private static IReadOnlyDictionary<EntityId, IReadOnlyList<T>> GroupById<T>(
        IEnumerable<T> values,
        Func<T, EntityId> idSelector)
    {
        var groups = values
            .GroupBy(idSelector)
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<T>)new ReadOnlyCollection<T>(group.ToArray()));
        return new ReadOnlyDictionary<EntityId, IReadOnlyList<T>>(groups);
    }

    private static IReadOnlyDictionary<EntityId, T> UniqueEntries<T>(
        IReadOnlyDictionary<EntityId, IReadOnlyList<T>> groups)
    {
        var unique = groups
            .Where(pair => pair.Value.Count == 1)
            .ToDictionary(pair => pair.Key, pair => pair.Value[0]);
        return new ReadOnlyDictionary<EntityId, T>(unique);
    }

    private static IReadOnlyDictionary<EntityId, IReadOnlyList<EntityId>> BuildScopeChildren(
        IReadOnlyDictionary<EntityId, IReadOnlyList<GraphEdge>> parentsByChild)
    {
        var pairs = parentsByChild
            .SelectMany(pair => pair.Value.Select(edge => new { Parent = edge.Target, Child = pair.Key }))
            .GroupBy(pair => pair.Parent)
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<EntityId>)new ReadOnlyCollection<EntityId>(
                    group.Select(pair => pair.Child).OrderBy(id => id).ToArray()));
        return new ReadOnlyDictionary<EntityId, IReadOnlyList<EntityId>>(pairs);
    }

    private static IReadOnlyList<T> GetOrEmpty<T>(
        IReadOnlyDictionary<EntityId, IReadOnlyList<T>> map,
        EntityId id) => map.TryGetValue(id, out var values) ? values : [];

    private static IReadOnlyList<EntityId> GetOrEmpty(
        IReadOnlyDictionary<EntityId, IReadOnlyList<EntityId>> map,
        EntityId id) => map.TryGetValue(id, out var values) ? values : [];
}
