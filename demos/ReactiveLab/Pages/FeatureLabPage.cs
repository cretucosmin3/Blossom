using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using ReactiveLab.Ui;
using static Blossom.Reactive.ReactiveEngine;
using static ReactiveLab.Ui.UiKit;

namespace ReactiveLab.Pages;

/// <summary>
/// Nine small experiments, one per reactive feature. Each says what you should see; the counters are
/// plain ints bumped inside memos and effects, refreshed after each button press.
/// Everything here is created while the page mounts, so it is all disposed when you leave the page.
/// </summary>
public sealed class FeatureLabPage : Grid
{
    public FeatureLabPage()
    {
        Name = "FeatureLabPage";
        Columns = "*, *, *";
        Rows = "*, *, *";
        ColumnGap = 12;
        RowGap = 12;
        Padding = new Thickness(14, 0, 14, 14);

        AddChild(Diamond());
        AddChild(Batching());
        AddChild(PeekAndUntrack());
        AddChild(MemoCutoff());
        AddChild(ShowWhen());
        AddChild(Ownership());
        AddChild(Errors());
        AddChild(CycleGuard());
        AddChild(TwoWay());
    }

    /// <summary>Button that runs <paramref name="action"/>, then bumps <paramref name="stamp"/> so counter text refreshes.</summary>
    private static Btn Act(string label, Signal<int> stamp, Action action, BtnKind kind = BtnKind.Normal)
    {
        var b = new Btn(label, kind, small: true);
        b.Clicked += () =>
        {
            action();
            stamp.Value++;
        };
        return b;
    }

    private static Stack Experiment(string title, string hint)
    {
        var panel = Panel(8);
        panel.AddChild(Section(title, hint));
        return panel;
    }

    private static VisualElement Counters(Signal<int> stamp, Func<string> text)
        => Text(() =>
        {
            _ = stamp.Value;
            return text();
        }, 12f, muted: true);

    // 1 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement Diamond()
    {
        var panel = Experiment("Diamond, glitch-free",
            "d reads b and c, which both read a. d must compute once per change and never see b and c out of step.");
        var stamp = CreateSignal(0);
        var a = CreateSignal(1);
        int bRuns = 0, cRuns = 0, dRuns = 0, glitches = 0;
        var b = CreateMemo(() => { bRuns++; return a.Value * 2; });
        var c = CreateMemo(() => { cRuns++; return a.Value + 1; });
        var d = CreateMemo(() =>
        {
            dRuns++;
            int bv = b.Value, cv = c.Value;
            if (bv / 2 + 1 != cv) glitches++;
            return bv + cv;
        });

        panel.AddChild(Text(() => $"a = {a.Value}   b = 2a = {b.Value}   c = a + 1 = {c.Value}   d = b + c = {d.Value}", 13.5f, 600));
        panel.AddChild(Counters(stamp, () => $"d computed {dRuns}× · b {bRuns}× · c {cRuns}× · glitches seen: {glitches}"));
        panel.AddChild(Buttons(
            Act("a + 1", stamp, () => a.Value++),
            Act("a + 1, ×10 batched", stamp, () => Batch(() => { for (int i = 0; i < 10; i++) a.Value++; })),
            Act("×10 unbatched", stamp, () => { for (int i = 0; i < 10; i++) a.Value++; })));
        panel.AddChild(Paragraph("Expect: +1 compute each for b, c, d per click, +10 unbatched. Glitches always 0."));
        return panel;
    }

    // 2 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement Batching()
    {
        var panel = Experiment("Batch",
            "One effect reads x, y and z. Writing all three costs three runs, or one inside a batch.");
        var x = CreateSignal(0);
        var y = CreateSignal(0);
        var z = CreateSignal(0);
        var stamp = CreateSignal(0);
        int runs = 0;

        var label = Text("", 13.5f, 600);
        label.Bind(l =>
        {
            runs++;
            l.Text = $"x = {x.Value}   y = {y.Value}   z = {z.Value}   ·   effect ran {runs}×";
        });
        panel.AddChild(label);
        panel.AddChild(Buttons(
            Act("Write x, y, z", stamp, () => { x.Value++; y.Value++; z.Value++; }),
            Act("Write x, y, z in a batch", stamp, () => Batch(() => { x.Value++; y.Value++; z.Value++; })),
            Act("Write x = x", stamp, () => x.Value = x.Peek())));
        panel.AddChild(Paragraph("Expect: +3 runs, +1 run, and no run for an equal value."));
        return panel;
    }

    // 3 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement PeekAndUntrack()
    {
        var panel = Experiment("Peek and Untrack",
            "The effect reads 'tracked' normally, 'peeked' with Peek() and 'untracked' inside Untrack(...).");
        var tracked = CreateSignal(0);
        var peeked = CreateSignal(0);
        var untracked = CreateSignal(0);
        var stamp = CreateSignal(0);
        int runs = 0;

        var label = Text("", 13.5f, 600);
        label.Bind(l =>
        {
            runs++;
            l.Text = $"tracked {tracked.Value} · peeked {peeked.Peek()} · untracked {Untrack(() => untracked.Value)} · ran {runs}×";
        });
        panel.AddChild(label);
        panel.AddChild(Buttons(
            Act("tracked + 1", stamp, () => tracked.Value++),
            Act("peeked + 1", stamp, () => peeked.Value++),
            Act("untracked + 1", stamp, () => untracked.Value++)));
        panel.AddChild(Paragraph("Expect: only 'tracked' re-runs it. The other two catch up the next time it runs."));
        return panel;
    }

    // 4 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement MemoCutoff()
    {
        var panel = Experiment("Memo cut-off",
            "parity = n is even or odd. The effect reads only parity, so it re-runs only when the parity flips.");
        var n = CreateSignal(0);
        var stamp = CreateSignal(0);
        int memoRuns = 0, effectRuns = 0;
        var parity = CreateMemo(() =>
        {
            memoRuns++;
            return n.Value % 2 == 0 ? "even" : "odd";
        });

        var label = Text("", 15f, 700);
        label.Bind(l =>
        {
            effectRuns++;
            l.Text = $"n is {parity.Value}";
        });
        panel.AddChild(label);
        panel.AddChild(Counters(stamp, () => $"n = {n.Peek()} · memo ran {memoRuns}× · effect ran {effectRuns}×"));
        panel.AddChild(Buttons(
            Act("n + 1", stamp, () => n.Value++),
            Act("n + 2", stamp, () => n.Value += 2)));
        panel.AddChild(Paragraph("Expect: both count up on n + 1; on n + 2 only the memo does."));
        return panel;
    }

    // 5 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement ShowWhen()
    {
        var panel = Experiment("Show.When",
            "Content mounts on true and is disposed on false, with every binding inside it.");
        var on = CreateSignal(true);
        var stamp = CreateSignal(0);
        int mounts = 0, alive = 0;

        var host = Column(4).Height(44);
        Show.When(host, () => on.Value, () =>
        {
            int number = ++mounts;
            alive++;
            var content = Text(() => $"Mounted content #{number} · its own binding sees clock {LabView.Clock.Value}", 13.5f, 600);
            content.Disposed += _ => alive--;
            return content;
        }, () => Text("Fallback: the content is unmounted", 13.5f, muted: true));
        panel.AddChild(host);
        panel.AddChild(Counters(stamp, () => $"mounted {mounts}× · live instances {alive}"));
        panel.AddChild(Buttons(
            Act("Toggle", stamp, () => on.Value = !on.Peek()),
            Act("Toggle ×9 batched", stamp, () => Batch(() => { for (int i = 0; i < 9; i++) on.Value = !on.Peek(); })),
            Act("Toggle ×10 batched", stamp, () => Batch(() => { for (int i = 0; i < 10; i++) on.Value = !on.Peek(); }))));
        panel.AddChild(Paragraph("Expect: live instances is 0 or 1, never more. ×10 batched ends where it started and does not remount."));
        return panel;
    }

    // 6 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement Ownership()
    {
        var panel = Experiment("Effect ownership",
            "An effect calls For.Each each time it runs. Its previous list must be removed, not left behind.");
        var version = CreateSignal(0);
        var stamp = CreateSignal(0);
        var chips = CreateSignal<IReadOnlyList<int>>(new[] { 1, 2, 3 });
        int reruns = 0, next = 4;

        var box = Row(6).Height(28);
        box.Bind(h =>
        {
            _ = version.Value;
            reruns++;
            For.Each(h, () => chips.Value, i => i, i => Chip($"#{i}"));
        });
        panel.AddChild(box);
        panel.AddChild(Counters(stamp, () => $"effect ran {reruns}× · chips in the box: {box.Children.Count} · items: {chips.Peek().Count}"));
        panel.AddChild(Buttons(
            Act("Re-run the effect", stamp, () => version.Value++),
            Act("Add chip", stamp, () => chips.Value = chips.Peek().Append(next++).ToList()),
            Act("Remove chip", stamp, () => chips.Value = chips.Peek().Skip(1).ToList())));
        panel.AddChild(Paragraph("Expect: chips in the box always equals items, however many times the effect re-runs."));
        return panel;
    }

    private static VisualElement Chip(string text)
    {
        var chip = Text(text, 12f, 600).Width(44).Height(24);
        chip.Style.Text.Alignment = TextAlign.Center;
        chip.Style.Border.Roundness = 12;
        chip.Bind(c => c.Style.BackColor = Look.Current.Value.Accent.WithAlpha(50));
        return chip;
    }

    // 7 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement Errors()
    {
        var panel = Experiment("Errors",
            "A memo that throws keeps the error until its input changes. A throwing effect does not stop the others.");
        var input = CreateSignal(9);
        var stamp = CreateSignal(0);
        var root = CreateMemo(() => input.Value < 0
            ? throw new InvalidOperationException($"no real square root of {input.Value}")
            : Math.Sqrt(input.Value));

        panel.AddChild(Text(() =>
        {
            try
            {
                return $"√{input.Value} = {root.Value:F3}";
            }
            catch (InvalidOperationException e)
            {
                return $"memo error: {e.Message}";
            }
        }, 13.5f, 600, colour: p => input.Value < 0 ? p.Danger : p.Text));
        panel.AddChild(Buttons(
            Act("input − 5", stamp, () => input.Value -= 5),
            Act("input + 5", stamp, () => input.Value += 5)));

        var boom = CreateSignal(0);
        string caught = "";
        CreateEffect(() =>
        {
            if (boom.Value % 2 == 1)
                throw new InvalidOperationException("this effect throws on odd values");
        });
        var sibling = Text("", 12.5f);
        sibling.Bind(s => s.Text = $"a sibling effect on the same signal saw {boom.Value}");
        panel.AddChild(Buttons(Act("Write to the throwing effect's signal", stamp, () =>
        {
            try
            {
                boom.Value++;
                caught = "no exception (even value)";
            }
            catch (Exception e)
            {
                caught = $"caught from the write: {e.Message}";
            }
        })));
        panel.AddChild(sibling);
        panel.AddChild(Counters(stamp, () => caught));
        return panel;
    }

    // 8 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement CycleGuard()
    {
        var panel = Experiment("Cycle guard",
            "When armed, an effect writes the signal it reads, forever. The flush stops it after 10 000 runs and throws.");
        var counter = CreateSignal(0);
        var armed = CreateSignal(false);
        var stamp = CreateSignal(0);
        string result = "Not run yet";

        CreateEffect(() =>
        {
            int v = counter.Value;
            if (armed.Value)
                counter.Value = v + 1;
        });

        panel.AddChild(Text(() => $"counter = {counter.Value:N0}", 15f, 700));
        panel.AddChild(Buttons(
            Act("Start the runaway effect", stamp, () =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    armed.Value = true;
                    result = "finished without an error (unexpected)";
                }
                catch (Exception e)
                {
                    result = $"caught after {sw.Elapsed.TotalMilliseconds:F0} ms: {e.Message}";
                }
                finally
                {
                    armed.Value = false;
                }
            }, BtnKind.Danger),
            Act("counter + 1", stamp, () => counter.Value++)));
        var outcome = Counters(stamp, () => result).Height(32);
        outcome.Style.Text.Overflow = Blossom.Core.Visual.Enums.TextOverflow.Wrap;
        outcome.Style.Text.MaxLines = 2;
        panel.AddChild(outcome);
        panel.AddChild(Paragraph("Expect: the counter jumps by about 10 000, the error shows here, and the app keeps running."));
        return panel;
    }

    // 9 ─────────────────────────────────────────────────────────────────────────────────────

    private static VisualElement TwoWay()
    {
        var panel = Experiment("Two-way binding",
            "Both fields are bound to one signal. A memo validates it; typing in either field updates everything.");
        var name = CreateSignal("Ada");
        var stamp = CreateSignal(0);
        var problem = CreateMemo(() =>
        {
            string v = name.Value.Trim();
            if (v.Length < 3) return "at least 3 characters";
            if (v.Length > 16) return "16 characters at most";
            if (v.Any(char.IsDigit)) return "no digits, please";
            return "";
        });

        panel.AddChild(Row(8, new Field("Name", 160).BindValue(name).Grow(), new Field("Same signal", 160).BindValue(name).Grow()));
        panel.AddChild(Text(() => problem.Value.Length == 0 ? $"Valid: hello, {name.Value.Trim()}!" : $"Invalid: {problem.Value}",
            13.5f, 600, colour: p => problem.Value.Length == 0 ? p.Success : p.Danger));
        panel.AddChild(Text(() => $"{name.Value.Length} characters · upper case: {name.Value.ToUpperInvariant()}", 12f, muted: true));
        panel.AddChild(Buttons(
            Act("Set to \"Grace\"", stamp, () => name.Value = "Grace"),
            Act("Set to \"R2D2\"", stamp, () => name.Value = "R2D2")));
        return panel;
    }
}
