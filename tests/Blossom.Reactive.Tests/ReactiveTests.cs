using System;
using System.Collections.Generic;
using Blossom.Core.Visual;
using Blossom.Reactive;
using SkiaSharp;
using Xunit;

namespace Blossom.Reactive.Tests;

public class ReactiveTests
{
    [Fact]
    public void Signal_BasicReadWrite_TriggersEffect()
    {
        var count = ReactiveEngine.CreateSignal(10);
        int runs = 0;
        int observed = 0;

        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            runs++;
            observed = count.Value;
        });

        Assert.Equal(1, runs);
        Assert.Equal(10, observed);

        count.Value = 20;
        Assert.Equal(2, runs);
        Assert.Equal(20, observed);

        // Setting same value should NOT trigger effect
        count.Value = 20;
        Assert.Equal(2, runs);
    }

    [Fact]
    public void Signal_DynamicBranching_CleansUpInactiveDependencies()
    {
        var toggle = ReactiveEngine.CreateSignal(true);
        var a = ReactiveEngine.CreateSignal("A");
        var b = ReactiveEngine.CreateSignal("B");

        int runs = 0;
        string observed = "";

        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            runs++;
            observed = toggle.Value ? a.Value : b.Value;
        });

        Assert.Equal(1, runs);
        Assert.Equal("A", observed);

        // Mutating 'b' should NOT trigger effect since toggle is true
        b.Value = "B2";
        Assert.Equal(1, runs);

        // Switch branch
        toggle.Value = false;
        Assert.Equal(2, runs);
        Assert.Equal("B2", observed);

        // Now mutating 'a' should NOT trigger effect because 'a' is no longer a dependency
        a.Value = "A2";
        Assert.Equal(2, runs);

        // Mutating 'b' DOES trigger effect
        b.Value = "B3";
        Assert.Equal(3, runs);
        Assert.Equal("B3", observed);
    }

    [Fact]
    public void Memo_CachesAndUpdatesWhenUpstreamChanges()
    {
        var first = ReactiveEngine.CreateSignal("John");
        var last = ReactiveEngine.CreateSignal("Doe");

        int computeRuns = 0;
        using var fullName = ReactiveEngine.CreateMemo(() =>
        {
            computeRuns++;
            return $"{first.Value} {last.Value}";
        });

        Assert.Equal(1, computeRuns);
        Assert.Equal("John Doe", fullName.Value);
        Assert.Equal(1, computeRuns); // Cached

        int effectRuns = 0;
        string observed = "";
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            effectRuns++;
            observed = fullName.Value;
        });

        Assert.Equal(1, effectRuns);
        Assert.Equal("John Doe", observed);

        // Update upstream
        first.Value = "Jane";
        Assert.Equal("Jane Doe", fullName.Value);
        Assert.Equal(2, effectRuns);
        Assert.Equal("Jane Doe", observed);
    }

    [Fact]
    public void Batch_GroupsMultipleSignalWritesIntoSingleEffectExecution()
    {
        var x = ReactiveEngine.CreateSignal(0);
        var y = ReactiveEngine.CreateSignal(0);

        int effectRuns = 0;
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            effectRuns++;
            _ = x.Value + y.Value;
        });

        Assert.Equal(1, effectRuns);

        ReactiveEngine.Batch(() =>
        {
            x.Value = 10;
            y.Value = 20;
            x.Value = 30;
        });

        // Effect should have run only ONCE for the whole batch
        Assert.Equal(2, effectRuns);
        Assert.Equal(30, x.Peek());
        Assert.Equal(20, y.Peek());
    }

    [Fact]
    public void Untrack_PreventsDependencyRegistration()
    {
        var a = ReactiveEngine.CreateSignal(1);
        var b = ReactiveEngine.CreateSignal(100);

        int runs = 0;
        int sum = 0;

        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            runs++;
            sum = a.Value + ReactiveEngine.Untrack(() => b.Value);
        });

        Assert.Equal(1, runs);
        Assert.Equal(101, sum);

        // Changing untracked 'b' should not run effect
        b.Value = 200;
        Assert.Equal(1, runs);

        // Changing tracked 'a' runs effect and picks up new 'b'
        a.Value = 2;
        Assert.Equal(2, runs);
        Assert.Equal(202, sum);
    }

    [Fact]
    public void VisualElement_BindText_UpdatesElementDirectly()
    {
        var title = ReactiveEngine.CreateSignal("Initial");
        var element = new VisualElement().BindText(() => title.Value);

        Assert.Equal("Initial", element.Text);

        title.Value = "Updated";
        Assert.Equal("Updated", element.Text);
    }

    [Fact]
    public void VisualElement_Disposal_CleansUpReactiveEffects()
    {
        var signal = ReactiveEngine.CreateSignal("Active");
        int runs = 0;

        var element = new VisualElement();
        element.Bind(el =>
        {
            runs++;
            el.Text = signal.Value;
        });

        Assert.Equal(1, runs);

        signal.Value = "Active 2";
        Assert.Equal(2, runs);

        // Disposing element disposes its reactive scope
        element.Dispose();

        signal.Value = "After Dispose";
        Assert.Equal(2, runs); // Should not run again
    }

    [Fact]
    public void Show_MountsAndUnmountsComponents()
    {
        var parent = new VisualElement();
        var isVisible = ReactiveEngine.CreateSignal(false);

        using var flow = Show.When(
            parent,
            condition: () => isVisible.Value,
            contentFactory: () => new VisualElement { Name = "ContentNode" },
            fallbackFactory: () => new VisualElement { Name = "FallbackNode" }
        );

        Assert.Single(parent.Children);
        Assert.Equal("FallbackNode", parent.Children[0].Name);

        isVisible.Value = true;
        Assert.Single(parent.Children);
        Assert.Equal("ContentNode", parent.Children[0].Name);

        isVisible.Value = false;
        Assert.Single(parent.Children);
        Assert.Equal("FallbackNode", parent.Children[0].Name);
    }

    [Fact]
    public void Memo_DoesNotNotifyWhenComputedValueUnchanged()
    {
        var source = ReactiveEngine.CreateSignal(2);
        int computeRuns = 0;
        using var parity = ReactiveEngine.CreateMemo(() =>
        {
            computeRuns++;
            return source.Value % 2;
        });

        int effectRuns = 0;
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            effectRuns++;
            _ = parity.Value;
        });

        Assert.Equal(1, computeRuns);
        Assert.Equal(1, effectRuns);

        source.Value = 4; // still even
        Assert.Equal(2, computeRuns);
        Assert.Equal(1, effectRuns);

        source.Value = 5; // odd
        Assert.Equal(3, computeRuns);
        Assert.Equal(2, effectRuns);
    }

    [Fact]
    public void DiamondGraph_IsGlitchFree()
    {
        var source = ReactiveEngine.CreateSignal(1);
        using var left = ReactiveEngine.CreateMemo(() => source.Value);
        using var right = ReactiveEngine.CreateMemo(() => source.Value);

        var sums = new List<int>();
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            sums.Add(left.Value + right.Value);
        });

        Assert.Equal(new[] { 2 }, sums);

        source.Value = 2;
        Assert.Equal(new[] { 2, 4 }, sums);
    }

    [Fact]
    public void NestedMemos_PullConsistentValues()
    {
        var source = ReactiveEngine.CreateSignal(1);
        using var inner = ReactiveEngine.CreateMemo(() => source.Value + 1);
        using var outer = ReactiveEngine.CreateMemo(() => inner.Value * 10);

        var observed = new List<int>();
        using var effect = ReactiveEngine.CreateEffect(() => observed.Add(outer.Value));

        Assert.Equal(new[] { 20 }, observed);
        source.Value = 3;
        Assert.Equal(new[] { 20, 40 }, observed);
    }

    [Fact]
    public void Effect_WritingOwnDependency_IsRequeued()
    {
        var count = ReactiveEngine.CreateSignal(0);
        int runs = 0;

        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            runs++;
            if (count.Value < 3)
            {
                count.Value = count.Value + 1;
            }
        });

        Assert.Equal(4, runs);
        Assert.Equal(3, count.Peek());
    }

    [Fact]
    public void For_KeyedReconciliation_PreservesExistingNodes()
    {
        var parent = new VisualElement();

        var items = ReactiveEngine.CreateSignal(new List<TestItem>
        {
            new(1, "First"),
            new(2, "Second"),
            new(3, "Third")
        });

        int renderCount = 0;
        using var forFlow = For.Each(
            parent,
            items: () => items.Value,
            keySelector: item => item.Id,
            template: item =>
            {
                renderCount++;
                return new VisualElement { Name = $"Node_{item.Id}", Text = item.Name };
            }
        );

        Assert.Equal(3, parent.Children.Count);
        Assert.Equal(3, renderCount);

        var firstNode = parent.Children[0];
        var secondNode = parent.Children[1];
        var thirdNode = parent.Children[2];

        // Reorder items and remove the second one: [3, 1]
        items.Value = new List<TestItem>
        {
            new(3, "Third"),
            new(1, "First")
        };

        Assert.Equal(2, parent.Children.Count);
        // No new nodes rendered! Existing nodes reused
        Assert.Equal(3, renderCount);

        // Nodes must be in the new order: [Node_3, Node_1]
        Assert.Same(thirdNode, parent.Children[0]);
        Assert.Same(firstNode, parent.Children[1]);
        Assert.True(secondNode.IsDisposed);
    }

    private record TestItem(int Id, string Name);
}
