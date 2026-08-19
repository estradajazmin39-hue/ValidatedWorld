using ValidatedWorld.Core;
using ValidatedWorld.Validation;

namespace ValidatedWorld.Validation.Tests;

public sealed class GraphProjectionTests
{
    [Fact]
    public void Batch_contains_one_sorted_final_operation_per_entity()
    {
        var node = Node("new-node", "New node");
        var edge = new GraphEdge(
            new EntityId("new-edge"),
            new EntityId("battery-assumption"),
            new EntityId("runtime-test"),
            "supports",
            ReviewDirection.Both);
        var batch = new GraphOperationBatch(
        [
            GraphOperation.AddEdge(edge),
            GraphOperation.RemoveNode(new EntityId("retention-policy")),
            GraphOperation.AddNode(node),
        ]);

        Assert.Equal(new[] { "new-edge", "new-node", "retention-policy" },
            batch.Operations.Select(operation => operation.EntityId.Value));
        Assert.Throws<ArgumentException>(() => new GraphOperationBatch(
        [
            GraphOperation.ReplaceNode(node),
            GraphOperation.RemoveNode(node.Id),
        ]));
    }

    [Fact]
    public void Projection_supports_all_operations_and_does_not_mutate_base()
    {
        var baseGraph = ValidationGraphBuilder.CreateTechnicalProject();
        var replacement = Node("battery-assumption", "The battery lasts for the revised duty cycle", "assumption");
        var added = Node("new-requirement", "The new requirement is explicit", "requirement");
        var addedParent = new GraphEdge(
            new EntityId("new-requirement-parent"),
            added.Id,
            new EntityId("scope-privacy"),
            "scope-parent",
            ReviewDirection.None);
        var redirected = new GraphEdge(
            new EntityId("battery-requires-test"),
            new EntityId("battery-assumption"),
            new EntityId("new-requirement"),
            "requires",
            ReviewDirection.SourceToTarget);
        var batch = new GraphOperationBatch(
        [
            GraphOperation.ReplaceNode(replacement),
            GraphOperation.AddNode(added),
            GraphOperation.AddEdge(addedParent),
            GraphOperation.ReplaceEdge(redirected),
            GraphOperation.RemoveEdge(new EntityId("retention-informs-design")),
        ]);

        var result = new GraphProjector().Project(baseGraph, batch);

        Assert.True(result.IsValid);
        Assert.Equal("The battery lasts for the revised duty cycle",
            result.Graph.Nodes.Single(node => node.Id == replacement.Id).Text);
        Assert.Contains(result.Graph.Nodes, node => node.Id == added.Id);
        Assert.Equal(new EntityId("new-requirement"),
            result.Graph.Edges.Single(edge => edge.Id == redirected.Id).Target);
        Assert.Contains(baseGraph.Nodes, node => node.Id == new EntityId("battery-assumption") &&
            node.Text == "Battery lasts");
        Assert.Contains(baseGraph.Edges, edge => edge.Id == new EntityId("retention-informs-design"));
        Assert.DoesNotContain(baseGraph.Nodes, node => node.Id == added.Id);
    }

    [Fact]
    public void Projection_requires_add_replace_remove_preconditions_and_preserves_incident_edges()
    {
        var graph = ValidationGraphBuilder.CreateTechnicalProject();
        var projector = new GraphProjector();

        var missingAdd = Assert.Throws<GraphOperationException>(() => projector.Project(
            graph,
            [GraphOperation.AddNode(Node("battery-assumption", "duplicate"))]));
        Assert.Equal("add-precondition", missingAdd.Code);
        var wrongKind = GraphOperation.RemoveNode(new EntityId("battery-requires-test"));
        var wrongKindException = Assert.Throws<GraphOperationException>(() => projector.Project(graph, [wrongKind]));
        Assert.Equal("wrong-entity-kind", wrongKindException.Code);

        var missingReplace = Assert.Throws<GraphOperationException>(() => projector.Project(
            graph,
            [GraphOperation.ReplaceNode(Node("missing", "Missing"))]));
        Assert.Equal("replace-precondition", missingReplace.Code);

        var missingRemove = Assert.Throws<GraphOperationException>(() => projector.Project(
            graph,
            [GraphOperation.RemoveEdge(new EntityId("missing"))]));
        Assert.Equal("remove-precondition", missingRemove.Code);

        var result = projector.Project(graph, [GraphOperation.RemoveNode(new EntityId("battery-assumption"))]);
        Assert.False(result.IsValid);
        Assert.Contains(result.Graph.Edges, edge => edge.Id == new EntityId("battery-requires-test"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "missing-edge-source");
    }

    [Fact]
    public void Projection_returns_structural_validation_for_an_invalid_proposal()
    {
        var graph = ValidationGraphBuilder.CreateTechnicalProject();
        var brokenEdge = new GraphEdge(
            new EntityId("broken-edge"),
            new EntityId("battery-assumption"),
            new EntityId("missing-node"),
            "supports",
            ReviewDirection.SourceToTarget);

        var result = new GraphProjector().Project(graph, [GraphOperation.AddEdge(brokenEdge)]);

        Assert.True(result.IsInvalid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "missing-edge-target" && diagnostic.EntityId == brokenEdge.Id);
    }

    [Fact]
    public void Focus_expansion_adds_only_an_explicit_scope_parent()
    {
        var graph = ValidationGraphBuilder.CreateTechnicalProject();
        var node = Node("new-requirement", "A focused requirement", "requirement");
        var initial = new GraphOperationBatch([GraphOperation.AddNode(node)]);

        var expanded = GraphOperationFocus.ExpandScopeParents(
            graph,
            initial,
            [new ScopeParentSelection(node.Id, new EntityId("scope-privacy"), new EntityId("new-scope-parent"))]);
        var result = new GraphProjector().Project(graph, expanded);

        Assert.True(result.IsValid);
        Assert.Contains(result.Graph.Edges, edge => edge.Id == new EntityId("new-scope-parent") &&
            edge.Relationship == "scope-parent" && edge.Source == node.Id &&
            edge.Target == new EntityId("scope-privacy") && edge.ReviewDirection == ReviewDirection.None);
        Assert.DoesNotContain(result.Graph.Edges, edge => edge.Source == node.Id && edge.Relationship != "scope-parent");

        Assert.Throws<ArgumentException>(() => GraphOperationFocus.ExpandScopeParents(
            graph,
            initial,
            [
                new ScopeParentSelection(node.Id, new EntityId("scope-power"), new EntityId("parent-a")),
                new ScopeParentSelection(node.Id, new EntityId("scope-privacy"), new EntityId("parent-b")),
            ]));
    }

    [Fact]
    public void Focus_accepts_a_supplied_scope_parent_without_inventing_a_semantic_edge()
    {
        var graph = ValidationGraphBuilder.CreateTechnicalProject();
        var node = Node("new-requirement", "A focused requirement", "requirement");
        var parentEdge = new GraphEdge(
            new EntityId("supplied-parent"),
            node.Id,
            new EntityId("scope-privacy"),
            "scope-parent",
            ReviewDirection.None);

        var expanded = GraphOperationFocus.ExpandScopeParents(
            graph,
            [GraphOperation.AddNode(node), GraphOperation.AddEdge(parentEdge)],
            []);
        var result = new GraphProjector().Project(graph, expanded);

        Assert.True(result.IsValid);
        Assert.Single(result.Graph.Edges, edge => edge.Source == node.Id);
    }

    private static GraphNode Node(string id, string text, string? kind = null) =>
        new(new EntityId(id), text, kind);
}
