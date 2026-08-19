using ValidatedWorld.Core;
using ValidatedWorld.Validation;

namespace ValidatedWorld.Validation.Tests;

public sealed class GraphValidationTests
{
    [Fact]
    public void Technical_project_validates_and_indexes_scope_and_review_relationships()
    {
        var graph = ValidationGraphBuilder.CreateTechnicalProject();
        var result = new GraphValidator().Validate(graph);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(graph.Nodes.Count, result.Index.NodesById.Count);
        Assert.Equal(graph.Edges.Count, result.Index.EdgesById.Count);
        Assert.Equal(
            new[] { new EntityId("scope-power"), new EntityId("scope-privacy") },
            result.Index.GetScopeChildren(new EntityId("purpose")));
        Assert.Equal(
            new[]
            {
                new EntityId("scope-power"),
                new EntityId("scope-privacy"),
                new EntityId("battery-assumption"),
                new EntityId("design-anchor"),
                new EntityId("runtime-test"),
                new EntityId("retention-policy"),
            },
            result.Index.GetScopeDescendants(new EntityId("purpose")));
        Assert.Equal(
            new[]
            {
                new EntityId("retention-policy"),
                new EntityId("scope-privacy"),
                new EntityId("purpose"),
            },
            result.Index.GetScopeUpstreamPath(new EntityId("retention-policy")));
        Assert.Contains(
            result.Index.ReviewArcs,
            arc => arc.EdgeId == new EntityId("battery-requires-test") &&
                arc.From == new EntityId("battery-assumption") && arc.To == new EntityId("runtime-test"));
        Assert.Contains(
            result.Index.ReviewArcs,
            arc => arc.EdgeId == new EntityId("retention-informs-design") &&
                arc.From == new EntityId("design-anchor") && arc.To == new EntityId("retention-policy"));
        Assert.DoesNotContain(result.Index.ReviewArcs, arc => arc.EdgeId == new EntityId("scope-power-parent"));
    }

    [Fact]
    public void Validator_reports_identity_endpoints_and_scope_tree_violations()
    {
        var purpose = Node("purpose", "Purpose");
        var child = Node("child", "Child");
        var duplicateChild = Node("child", "Duplicate child");
        var orphan = Node("orphan", "Orphan");
        var nodes = new[] { purpose, child, duplicateChild, orphan };
        var edges = new[]
        {
            Edge("child-parent", child.Id, purpose.Id),
            Edge("child-parent-2", child.Id, purpose.Id),
            Edge("purpose-parent", purpose.Id, child.Id),
            new GraphEdge(
                new EntityId("broken"),
                orphan.Id,
                new EntityId("missing"),
                "unknown-link",
                ReviewDirection.None),
            new GraphEdge(
                new EntityId("purpose-parent"),
                child.Id,
                purpose.Id,
                "unknown-link",
                ReviewDirection.None),
        };
        var graph = new ProjectGraph(new ProjectId("invalid"), "Invalid", purpose.Id, nodes, edges);

        var result = new GraphValidator().Validate(graph);
        var codes = result.Diagnostics.Select(diagnostic => diagnostic.Code).ToHashSet(StringComparer.Ordinal);

        Assert.True(result.IsInvalid);
        Assert.Contains("duplicate-node-id", codes);
        Assert.Contains("duplicate-edge-id", codes);
        Assert.Contains("missing-edge-target", codes);
        Assert.Contains("multiple-scope-parents", codes);
        Assert.Contains("purpose-has-scope-parent", codes);
        Assert.Contains("missing-scope-parent", codes);
    }

    [Fact]
    public void Validator_reports_cycles_disconnected_lineages_and_scope_review_direction()
    {
        var purpose = Node("purpose", "Purpose");
        var first = Node("first", "First");
        var second = Node("second", "Second");
        var edges = new[]
        {
            Edge("first-parent", first.Id, second.Id, ReviewDirection.SourceToTarget),
            Edge("second-parent", second.Id, first.Id),
        };

        var result = new GraphValidator().Validate(
            new ProjectGraph(new ProjectId("cycle"), "Cycle", purpose.Id, [purpose, first, second], edges));

        Assert.True(result.IsInvalid);
        Assert.Contains(result.Diagnostics, d => d.Code == "scope-cycle" && d.EntityId == first.Id);
        Assert.Contains(result.Diagnostics, d => d.Code == "scope-does-not-reach-purpose");
        Assert.Contains(result.Diagnostics, d => d.Code == "scope-parent-review-direction");
    }

    [Fact]
    public void Validation_is_deterministic_and_bounds_are_inconclusive()
    {
        var graph = ValidationGraphBuilder.CreateTechnicalProject();
        var reversed = new ProjectGraph(
            graph.ProjectId,
            graph.Title,
            graph.PurposeNodeId,
            graph.Nodes.Reverse(),
            graph.Edges.Reverse());
        var first = new GraphValidator().Validate(graph);
        var second = new GraphValidator().Validate(reversed);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.Diagnostics, second.Diagnostics);
        Assert.Equal(
            ValidationStatus.Inconclusive,
            new GraphValidator().Validate(
                graph,
                new GraphValidationOptions { MaxTraversalDepth = 1 }).Status);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = new GraphValidator().Validate(
            graph,
            new GraphValidationOptions { CancellationToken = cancellation.Token });
        Assert.True(cancelled.IsInconclusive);
        Assert.Contains(cancelled.Diagnostics, d => d.Code == "validation-cancelled");
    }

    private static GraphNode Node(string id, string text) => new(new EntityId(id), text, "unknown-kind");

    private static GraphEdge Edge(
        string id,
        EntityId source,
        EntityId target,
        ReviewDirection direction = ReviewDirection.None) =>
        new(new EntityId(id), source, target, "scope-parent", direction);
}

internal static class ValidationGraphBuilder
{
    public static ProjectGraph CreateTechnicalProject()
    {
        var purpose = new GraphNode(new EntityId("purpose"), "An offline privacy-preserving sensor");
        var power = new GraphNode(new EntityId("scope-power"), "Power behavior", "scope");
        var privacy = new GraphNode(new EntityId("scope-privacy"), "Privacy behavior", "scope");
        var battery = new GraphNode(new EntityId("battery-assumption"), "Battery lasts", "assumption");
        var retention = new GraphNode(new EntityId("retention-policy"), "Data is retained briefly", "requirement");
        var test = new GraphNode(new EntityId("runtime-test"), "Runtime behavior is verified", "verification");
        var document = new GraphNode(
            new EntityId("design-anchor"),
            "Design record",
            "external-anchor",
            ["artifact"]);

        var edges = new[]
        {
            new GraphEdge(new EntityId("scope-power-parent"), power.Id, purpose.Id, "scope-parent", ReviewDirection.None),
            new GraphEdge(new EntityId("scope-privacy-parent"), privacy.Id, purpose.Id, "scope-parent", ReviewDirection.None),
            new GraphEdge(new EntityId("battery-scope-parent"), battery.Id, power.Id, "scope-parent", ReviewDirection.None),
            new GraphEdge(new EntityId("retention-scope-parent"), retention.Id, privacy.Id, "scope-parent", ReviewDirection.None),
            new GraphEdge(new EntityId("runtime-scope-parent"), test.Id, power.Id, "scope-parent", ReviewDirection.None),
            new GraphEdge(new EntityId("anchor-scope-parent"), document.Id, power.Id, "scope-parent", ReviewDirection.None),
            new GraphEdge(new EntityId("battery-requires-test"), battery.Id, test.Id, "requires", ReviewDirection.SourceToTarget),
            new GraphEdge(new EntityId("retention-informs-design"), retention.Id, document.Id, "informs", ReviewDirection.TargetToSource),
        };

        return new ProjectGraph(
            new ProjectId("technical-project"),
            "Technical Project",
            purpose.Id,
            [purpose, power, privacy, battery, retention, test, document],
            edges);
    }
}
