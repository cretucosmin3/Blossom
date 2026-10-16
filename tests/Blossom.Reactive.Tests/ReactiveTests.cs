using System;
using System.Collections.Generic;
using System.Linq;
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

    [Fact]
    public void Flush_SignalWithMoreThanTenThousandEffects_RunsThemAll()
    {
        var theme = ReactiveEngine.CreateSignal(0);
        int runs = 0;
        using var scope = EffectScope.Record(() =>
        {
            for (int i = 0; i < 10_001; i++)
                ReactiveEngine.CreateEffect(() => { _ = theme.Value; runs++; });
        });

        runs = 0;
        theme.Value = 1;
        Assert.Equal(10_001, runs);
    }

    [Fact]
    public void Flush_EffectThatNeverSettles_Throws()
    {
        var count = ReactiveEngine.CreateSignal(0);

        Assert.Throws<InvalidOperationException>(() =>
            ReactiveEngine.CreateEffect(() => count.Value = count.Value + 1));
    }

    [Fact]
    public void Flush_ThrowingEffect_DoesNotCancelOtherEffects()
    {
        var source = ReactiveEngine.CreateSignal(0);
        using var failing = ReactiveEngine.CreateEffect(() =>
        {
            if (source.Value == 1) throw new InvalidOperationException("boom");
        });
        int siblingRuns = 0;
        using var sibling = ReactiveEngine.CreateEffect(() => { _ = source.Value; siblingRuns++; });

        var ex = Assert.Throws<InvalidOperationException>(() => source.Value = 1);
        Assert.Equal("boom", ex.Message);
        Assert.Equal(2, siblingRuns);
    }

    [Fact]
    public void For_SignalReadInTemplateBody_DoesNotRerunReconcile()
    {
        var parent = new VisualElement();
        var items = ReactiveEngine.CreateSignal(new List<int> { 1, 2, 3 });
        var label = ReactiveEngine.CreateSignal("a");
        int reconciles = 0;

        using var forFlow = For.Each(
            parent,
            items: () => { reconciles++; return items.Value; },
            keySelector: i => i,
            template: i => new VisualElement { Text = label.Value });

        Assert.Equal(1, reconciles);
        label.Value = "b";
        Assert.Equal(1, reconciles);
    }

    [Fact]
    public void Show_SignalReadInFactory_DoesNotRerunCondition()
    {
        var parent = new VisualElement();
        var label = ReactiveEngine.CreateSignal("a");
        int conditionRuns = 0;

        using var flow = Show.When(
            parent,
            condition: () => { conditionRuns++; return true; },
            contentFactory: () => new VisualElement { Text = label.Value });

        Assert.Equal(1, conditionRuns);
        label.Value = "b";
        Assert.Equal(1, conditionRuns);
    }

    [Fact]
    public void For_Reorders_MatchItemOrderAfterStaticChrome()
    {
        var parent = new VisualElement();
        var chrome = new VisualElement { Name = "Chrome" };
        parent.AddChild(chrome);
        var items = ReactiveEngine.CreateSignal(new List<int> { 1, 2, 3, 4 });

        using var forFlow = For.Each(
            parent,
            items: () => items.Value,
            keySelector: i => i,
            template: i => new VisualElement { Name = $"Node_{i}" },
            startIndex: 1);

        void AssertOrder(params int[] expected)
        {
            Assert.Same(chrome, parent.Children[0]);
            Assert.Equal(expected.Length + 1, parent.Children.Count);
            for (int i = 0; i < expected.Length; i++)
                Assert.Equal($"Node_{expected[i]}", parent.Children[i + 1].Name);
        }

        AssertOrder(1, 2, 3, 4);
        items.Value = new List<int> { 0, 1, 2, 3, 4 };
        AssertOrder(0, 1, 2, 3, 4);
        items.Value = new List<int> { 4, 3, 2, 1, 0 };
        AssertOrder(4, 3, 2, 1, 0);
        items.Value = new List<int> { 3, 5, 1 };
        AssertOrder(3, 5, 1);
        items.Value = new List<int> { 3, 5, 1 };
        AssertOrder(3, 5, 1);
    }

    [Fact]
    public void MemoOfMemos_Diamond_ComputesOnceWithConsistentInputs()
    {
        var a = ReactiveEngine.CreateSignal(1);
        using var b = ReactiveEngine.CreateMemo(() => a.Value * 2);
        using var c = ReactiveEngine.CreateMemo(() => a.Value + 1);
        var seen = new List<(int B, int C)>();
        using var d = ReactiveEngine.CreateMemo(() =>
        {
            seen.Add((b.Value, c.Value));
            return b.Value + c.Value;
        });
        using var effect = ReactiveEngine.CreateEffect(() => _ = d.Value);

        seen.Clear();
        a.Value = 10;

        Assert.Equal(new[] { (20, 11) }, seen);
        Assert.Equal(31, d.Value);
    }

    [Fact]
    public void ChainedDiamonds_RecomputeEachMemoOncePerWrite()
    {
        var a = ReactiveEngine.CreateSignal(0);
        Func<int> top = () => a.Value;
        int lastComputes = 0;
        Memo<int> last = null!;
        for (int i = 0; i < 12; i++)
        {
            var prev = top;
            bool isLast = i == 11;
            var left = ReactiveEngine.CreateMemo(() => prev() + 1);
            var right = ReactiveEngine.CreateMemo(() => prev() + 2);
            var join = ReactiveEngine.CreateMemo(() =>
            {
                if (isLast) lastComputes++;
                return left.Value + right.Value;
            });
            top = () => join.Value;
            last = join;
        }
        using var effect = ReactiveEngine.CreateEffect(() => _ = last.Value);

        lastComputes = 0;
        a.Value = 1;

        Assert.Equal(1, lastComputes);
    }

    [Fact]
    public void Memo_InBatch_ComputesOnceAndReadsFreshInsideBatch()
    {
        var x = ReactiveEngine.CreateSignal(0);
        var y = ReactiveEngine.CreateSignal(0);
        int computes = 0;
        using var sum = ReactiveEngine.CreateMemo(() => { computes++; return x.Value + y.Value; });
        int effectRuns = 0;
        using var effect = ReactiveEngine.CreateEffect(() => { effectRuns++; _ = sum.Value; });

        computes = 0;
        effectRuns = 0;
        int insideBatch = ReactiveEngine.Batch(() =>
        {
            x.Value = 1;
            y.Value = 2;
            int read = sum.Value;
            x.Value = 3;
            return read;
        });

        Assert.Equal(3, insideBatch);
        Assert.Equal(5, sum.Value);
        Assert.Equal(2, computes); // once for the read inside the batch, once after it
        Assert.Equal(1, effectRuns);
    }

    [Fact]
    public void Memo_WithoutObservers_RecomputesOnlyWhenRead()
    {
        var source = ReactiveEngine.CreateSignal(1);
        int computes = 0;
        using var doubled = ReactiveEngine.CreateMemo(() => { computes++; return source.Value * 2; });

        source.Value = 2;
        source.Value = 3;
        Assert.Equal(1, computes);

        Assert.Equal(6, doubled.Value);
        Assert.Equal(2, computes);
        Assert.Equal(6, doubled.Peek());
        Assert.Equal(2, computes);
    }

    [Fact]
    public void Effect_BranchNotTaken_DoesNotRecomputeStaleMemo()
    {
        var show = ReactiveEngine.CreateSignal(true);
        var source = ReactiveEngine.CreateSignal(1);
        int computes = 0;
        using var expensive = ReactiveEngine.CreateMemo(() => { computes++; return source.Value * 10; });
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            if (show.Value) _ = expensive.Value;
        });

        computes = 0;
        ReactiveEngine.Batch(() =>
        {
            show.Value = false;
            source.Value = 2;
        });

        Assert.Equal(0, computes);
    }

    [Fact]
    public void Memo_UnchangedOutput_StopsPropagationDownstream()
    {
        var source = ReactiveEngine.CreateSignal(2);
        using var parity = ReactiveEngine.CreateMemo(() => source.Value % 2);
        int labelComputes = 0;
        using var label = ReactiveEngine.CreateMemo(() => { labelComputes++; return parity.Value == 0 ? "even" : "odd"; });
        int effectRuns = 0;
        using var effect = ReactiveEngine.CreateEffect(() => { effectRuns++; _ = label.Value; });

        labelComputes = 0;
        effectRuns = 0;
        source.Value = 4;

        Assert.Equal(0, labelComputes);
        Assert.Equal(0, effectRuns);
    }

    [Fact]
    public void Effect_ReadingMemoMadeStaleInSameBatch_RunsOnce()
    {
        var x = ReactiveEngine.CreateSignal(0);
        var y = ReactiveEngine.CreateSignal(0);
        using var derived = ReactiveEngine.CreateMemo(() => y.Value + 1);
        int runs = 0;
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            runs++;
            _ = x.Value;
            _ = derived.Value;
        });

        runs = 0;
        ReactiveEngine.Batch(() =>
        {
            x.Value = 1;
            y.Value = 1;
        });

        Assert.Equal(1, runs);
    }

    [Fact]
    public void Effect_WritingSourceOfMemoItAlreadyRead_RunsAgainWithFreshMemo()
    {
        var count = ReactiveEngine.CreateSignal(0);
        using var doubled = ReactiveEngine.CreateMemo(() => count.Value * 2);
        var observed = new List<int>();
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            observed.Add(doubled.Value);
            if (count.Peek() < 2) count.Value = count.Peek() + 1;
        });

        Assert.Equal(new[] { 0, 2, 4 }, observed);
    }

    [Fact]
    public void Memo_ComputeThrows_RethrowsOnReadAndRecovers()
    {
        var divisor = ReactiveEngine.CreateSignal(1);
        using var quotient = ReactiveEngine.CreateMemo(() => 10 / divisor.Value);
        var errors = 0;
        int last = 0;
        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            try { last = quotient.Value; }
            catch (DivideByZeroException) { errors++; }
        });

        divisor.Value = 0;
        Assert.Equal(1, errors);
        Assert.Throws<DivideByZeroException>(() => quotient.Value);

        divisor.Value = 5;
        Assert.Equal(2, quotient.Value);
        Assert.Equal(2, last);
    }

    [Fact]
    public void Memo_InitialComputeThrows_ThrowsFromConstructor()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ReactiveEngine.CreateMemo<int>(() => throw new InvalidOperationException()));
    }

    [Fact]
    public void Effect_CreatedInsideEffect_IsDisposedOnRerunAndWithParent()
    {
        var outer = ReactiveEngine.CreateSignal(0);
        var inner = ReactiveEngine.CreateSignal(0);
        int innerRuns = 0;
        var parent = ReactiveEngine.CreateEffect(() =>
        {
            _ = outer.Value;
            ReactiveEngine.CreateEffect(() => { _ = inner.Value; innerRuns++; });
        });

        for (int i = 1; i <= 5; i++) outer.Value = i;
        innerRuns = 0;
        inner.Value = 1;
        Assert.Equal(1, innerRuns);

        parent.Dispose();
        innerRuns = 0;
        inner.Value = 2;
        Assert.Equal(0, innerRuns);
    }

    [Fact]
    public void Show_ContentBindings_SurviveConditionRerunWithSameValue()
    {
        var parent = new VisualElement();
        var count = ReactiveEngine.CreateSignal(0);
        var label = ReactiveEngine.CreateSignal("a");

        using var flow = Show.When(
            parent,
            condition: () => count.Value >= 0,
            contentFactory: () => new VisualElement().BindText(() => label.Value));

        count.Value = 1; // condition re-runs, stays true
        label.Value = "b";

        Assert.Equal("b", parent.Children[0].Text);
    }

    [Fact]
    public void Flush_CyclicEffect_ThrowsButOtherEffectsStillRun()
    {
        var ping = ReactiveEngine.CreateSignal(0);
        var trigger = ReactiveEngine.CreateSignal(false);
        int siblingRuns = 0;
        using var sibling = ReactiveEngine.CreateEffect(() => { _ = trigger.Value; siblingRuns++; });
        using var cyclic = ReactiveEngine.CreateEffect(() =>
        {
            if (trigger.Value) ping.Value = ping.Value + 1;
        });

        siblingRuns = 0;
        Assert.Throws<InvalidOperationException>(() => trigger.Value = true);
        Assert.Equal(1, siblingRuns);
    }

    [Fact]
    public void SignalWrite_ToSubscribedEffect_DoesNotAllocate()
    {
        var source = ReactiveEngine.CreateSignal(0);
        int sink = 0;
        using var effect = ReactiveEngine.CreateEffect(() => sink += source.Value);
        for (int i = 1; i <= 1000; i++) source.Value = i;

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 1001; i <= 2000; i++) source.Value = i;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void For_InsertRemoveAndReorderInOneWrite_KeepsChromeBeforeAndAfter()
    {
        var parent = new VisualElement();
        var header = new VisualElement { Name = "Header" };
        var footer = new VisualElement { Name = "Footer" };
        parent.AddChild(header);
        parent.AddChild(footer);
        var items = ReactiveEngine.CreateSignal(Enumerable.Range(0, 50).ToList());
        var mounted = new Dictionary<int, VisualElement>();

        using var forFlow = For.Each(
            parent,
            items: () => items.Value,
            keySelector: i => i,
            template: i => mounted[i] = new VisualElement { Name = $"Node_{i}" },
            startIndex: 1);

        var kept = mounted[10];
        var dropped = mounted[11];
        var next = Enumerable.Range(0, 50).Where(i => i % 3 != 2).Reverse().Concat(new[] { 100, 101 }).ToList();
        items.Value = next;

        var expected = new[] { "Header" }.Concat(next.Select(i => $"Node_{i}")).Append("Footer");
        Assert.Equal(expected, parent.Children.Select(c => c.Name));
        Assert.Same(kept, mounted[10]);
        Assert.True(dropped.IsDisposed);
        Assert.Null(dropped.Parent);
    }

    [Fact]
    public void For_CreatedInsideEffect_RemovesItsElementsWhenEffectReruns()
    {
        var parent = new VisualElement();
        var version = ReactiveEngine.CreateSignal(0);
        var items = new List<int> { 1, 2, 3 };
        var created = new List<VisualElement>();

        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            _ = version.Value;
            For.Each(parent, () => items, i => i, i =>
            {
                var element = new VisualElement();
                created.Add(element);
                return element;
            });
        });

        version.Value = 1;
        version.Value = 2;

        Assert.Equal(3, parent.Children.Count);
        Assert.Equal(created.Skip(6), parent.Children);
        Assert.All(created.Take(6), e => Assert.True(e.IsDisposed));

        effect.Dispose();
        Assert.Empty(parent.Children);
    }

    [Fact]
    public void Show_CreatedInsideEffect_RemovesItsContentWhenEffectReruns()
    {
        var parent = new VisualElement();
        var version = ReactiveEngine.CreateSignal(0);

        using var effect = ReactiveEngine.CreateEffect(() =>
        {
            _ = version.Value;
            Show.When(parent, () => true, () => new VisualElement { Name = $"Content_{version.Peek()}" });
        });

        version.Value = 1;

        var only = Assert.Single(parent.Children);
        Assert.Equal("Content_1", only.Name);
    }

    [Fact]
    public void Memo_DisposedByItsScopeWhileReadElsewhere_KeepsUpdating()
    {
        var a = ReactiveEngine.CreateSignal(1);
        Memo<int> doubled = null!;
        var scope = EffectScope.Record(() => { doubled = ReactiveEngine.CreateMemo(() => a.Value * 2); });
        var seen = new List<int>();
        using var effect = ReactiveEngine.CreateEffect(() => seen.Add(doubled.Value));

        scope.Dispose();
        a.Value = 5;

        Assert.Equal(new[] { 2, 10 }, seen);
    }

    [Fact]
    public void Memo_ReadAfterItsScopeWasDisposed_RecomputesAndSubscribesAgain()
    {
        // A static memo first touched while a component is built lands in that component's scope.
        var a = ReactiveEngine.CreateSignal(1);
        Memo<int> plusOne = null!;
        EffectScope.Record(() => { plusOne = ReactiveEngine.CreateMemo(() => a.Value + 1); }).Dispose();

        a.Value = 10;
        Assert.Equal(11, plusOne.Value);

        var seen = new List<int>();
        using var effect = ReactiveEngine.CreateEffect(() => seen.Add(plusOne.Value));
        a.Value = 20;
        Assert.Equal(new[] { 11, 21 }, seen);
    }

    private record TestItem(int Id, string Name);
}
