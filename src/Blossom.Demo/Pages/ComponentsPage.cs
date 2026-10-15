using System;
using System.Linq;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using Blossom.Testing.Styling;
using static Blossom.Reactive.ReactiveEngine;
using Blossom.Desktop;

namespace Blossom.Testing.Pages;

/// <summary>Every demo control, grouped into cards, each wired to signals.</summary>
public sealed class ComponentsPage : ScrollView
{
    public ComponentsPage() : base(20, new Thickness(28, 8, 28, 28))
    {
        Name = "ComponentsPage";

        var grid = new ColumnLayout("*, *");
        grid.AddChild(Buttons());
        grid.AddChild(Inputs());
        grid.AddChild(Selection());
        grid.AddChild(Ranges());
        grid.AddChild(Feedback());
        grid.AddChild(Overlays());
        Content.AddChild(grid);
        Content.AddChild(ProfileForm());
    }

    private static Card Buttons()
    {
        var card = new Card("Buttons", "ButtonBase handles capture, keyboard and click; variants are one recipe.");

        var variants = new WrapPanel();
        foreach (var v in Enum.GetValues<ButtonVariant>())
            variants.AddChild(new Button(v.ToString(), variant: v));
        card.AddChild(variants);

        var sizes = new WrapPanel();
        sizes.AddChild(new Button("Small", variant: ButtonVariant.Secondary, size: ButtonSize.Sm));
        sizes.AddChild(new Button("Medium", variant: ButtonVariant.Secondary));
        sizes.AddChild(new Button("Large", variant: ButtonVariant.Secondary, size: ButtonSize.Lg));
        sizes.AddChild(new Button("Disabled", "lock", ButtonVariant.Outline) { Enabled = false });
        card.AddChild(sizes);

        var icons = new WrapPanel();
        icons.AddChild(new Button("New task", "plus"));
        icons.AddChild(new Button("Delete", "trash", ButtonVariant.Destructive));
        foreach (var name in new[] { "search", "star", "bell", "sliders" })
            icons.AddChild(Button.IconOnly(name, ButtonVariant.Outline));

        var clicks = CreateSignal(0);
        var counter = new Button("Clicked 0 times", "zap", ButtonVariant.Ghost);
        counter.Clicked += () => clicks.Value++;
        counter.Bind(b => b.Label = clicks.Value == 1 ? "Clicked once" : $"Clicked {clicks.Value} times");
        icons.AddChild(counter);
        card.AddChild(icons);
        return card;
    }

    private static Card Inputs()
    {
        var card = new Card("Text fields", "TextInputBase supplies caret, selection, clipboard and word jumps.");

        var email = CreateSignal("");
        var emailValid = CreateMemo(() => email.Value.Contains('@') && email.Value.Contains('.'));
        var emailField = new TextField("you@example.com", "mail").BindValue(email);
        emailField.Bind(f => f.Invalid = email.Value.Length > 0 && !emailValid.Value);
        var emailHint = Ui.Text(() => email.Value.Length == 0 ? "We never share your email."
            : emailValid.Value ? "Looks good." : "Enter a valid email address.", "text.small");
        emailHint.Bind(h => h.UseStyle(email.Value.Length == 0 ? "text.small" : emailValid.Value ? "text.ok" : "text.error"));
        card.AddChild(Field("Email", emailField, emailHint));

        var password = CreateSignal("");
        var strength = CreateMemo(() =>
        {
            var p = password.Value;
            int score = (p.Length >= 8 ? 1 : 0) + (p.Any(char.IsDigit) ? 1 : 0)
                      + (p.Any(char.IsUpper) ? 1 : 0) + (p.Any(c => !char.IsLetterOrDigit(c)) ? 1 : 0);
            return p.Length == 0 ? 0 : Math.Max(1, score);
        });
        var passField = new TextField("At least 8 characters", "lock") { IsPassword = true }.BindValue(password);
        var meter = new ProgressBar(4f);
        meter.Bind(m => m.Value = strength.Value / 4f);
        var meterText = Ui.Text(() => strength.Value switch
        {
            0 => "Strength",
            1 => "Weak",
            2 => "Fair",
            3 => "Good",
            _ => "Strong"
        }, "text.small");
        card.AddChild(Field("Password", passField, Ui.Row(10, meter.Grow(), meterText.Width(52))));

        var qty = new TextField("0", "hash") { DigitsOnly = true, MaxLength = 4 };
        card.AddChild(Field("Quantity (digits only)", qty, null));
        return card;
    }

    private static Card Selection()
    {
        var card = new Card("Toggles", "ToggleBase tracks checked state; painting is custom Skia.");

        var notify = CreateSignal(true);
        card.AddChild(new Switch("Email notifications").BindChecked(notify));
        card.AddChild(new Switch("Weekly digest", true));
        card.AddChild(new Switch("Locked setting", true) { Enabled = false });
        card.AddChild(Ui.Divider());

        var remember = new Checkbox("Remember this device", true);
        var terms = new Checkbox("I accept the terms");
        card.AddChild(remember);
        card.AddChild(terms);

        var frequency = CreateSignal(1);
        var seg = new Segmented("Daily", "Weekly", "Monthly").BindSelected(frequency);
        var summary = Ui.Text(() => notify.Value
            ? $"You'll get {new[] { "daily", "weekly", "monthly" }[frequency.Value]} emails."
            : "Notifications are off.", "text.muted");
        card.AddChild(Ui.Row(12, seg, summary));
        return card;
    }

    private static Card Ranges()
    {
        var card = new Card("Sliders & progress", "A slider and a bar share one signal; the bar eases in OnUpdate.");

        var volume = CreateSignal(40f);
        var slider = new Slider(0, 100, 40).BindValue(volume);
        card.AddChild(Ui.Column(8,
            Ui.Row(8, Ui.Text("Volume", "text.label").Grow(), Ui.Text(() => $"{volume.Value:0}%", "text.accent")),
            slider));
        var level = new ProgressBar();
        level.Bind(p => p.Value = volume.Value / 100f);
        card.AddChild(level);
        card.AddChild(Ui.Divider());

        var upload = CreateSignal(0f);
        var running = CreateSignal(false);
        var bar = new ProgressBar();
        bar.Bind(p => p.Value = upload.Value);
        var start = new Button("Simulate upload", "arrow-right", ButtonVariant.Secondary, ButtonSize.Sm);
        start.Bind(b => b.Enabled = !running.Value);
        IDisposable? timer = null;
        start.Clicked += () =>
        {
            Batch(() => { upload.Value = 0f; running.Value = true; });
            timer = Shell.PostPeriodic(TimeSpan.FromMilliseconds(60), () =>
            {
                upload.Value = Math.Min(1f, upload.Peek() + 0.025f);
                if (upload.Peek() < 1f) return;
                timer?.Dispose();
                running.Value = false;
                AppState.Notify("Upload complete", "blossom-gallery.zip finished uploading.", Tone.Success);
            });
        };
        card.BindLifecycle(new Disposer(() => timer?.Dispose()));
        card.AddChild(Ui.Row(12, start, Ui.Text(() => running.Value ? $"Uploading… {upload.Value * 100:0}%" : upload.Value >= 1f ? "Done" : "Idle", "text.muted").Grow()));
        card.AddChild(bar);
        return card;
    }

    private static Card Feedback()
    {
        var card = new Card("Badges & avatars", "Tones map to theme tokens; avatars hash names to a hue.");
        var badges = new WrapPanel();
        foreach (var tone in Enum.GetValues<Tone>())
            badges.AddChild(new Badge(tone.ToString(), tone));
        card.AddChild(badges);

        var people = new WrapPanel { Gap = 14, LineGap = 12 };
        foreach (var name in new[] { "Ada Lovelace", "Alan Turing", "Grace Hopper", "Katherine Johnson", "Linus Torvalds" })
            people.AddChild(Ui.Row(8, new Avatar(name, 28f), Ui.Text(name.Split(' ')[0], "text.body")));
        card.AddChild(people);
        return card;
    }

    private static Card Overlays()
    {
        var card = new Card("Dialogs & toasts", "Dialogs use View.PushOverlay; toasts are a For.Each list.");
        var wrap = new WrapPanel();

        var open = new Button("Open dialog", "layers", ButtonVariant.Outline);
        open.Clicked += () => Dialogs.Confirm(open.ParentView, "Delete workspace?",
            "This removes every board and task. Escape or a click outside cancels.", "Delete",
            () => AppState.Notify("Workspace kept", "Just a demo, nothing was deleted.", Tone.Warning));
        wrap.AddChild(open);

        var task = new Button("New task…", "plus", ButtonVariant.Outline);
        task.Clicked += () => Dialogs.NewTask(task.ParentView);
        wrap.AddChild(task);

        (string Label, Tone Tone, string Message)[] toasts =
        {
            ("Success", Tone.Success, "Changes saved to the workspace."),
            ("Info", Tone.Info, "A new version of Blossom is available."),
            ("Error", Tone.Danger, "Could not reach the sync server."),
        };
        foreach (var t in toasts)
        {
            var b = new Button(t.Label, variant: ButtonVariant.Secondary);
            b.Clicked += () => AppState.Notify($"{t.Label} toast", t.Message, t.Tone);
            wrap.AddChild(b);
        }
        card.AddChild(wrap);
        return card;
    }

    private static Card ProfileForm()
    {
        var card = new Card("Profile", "Two-way bindings with a derived preview. Save enables only when the form is dirty and valid.");

        var first = CreateSignal("Ada");
        var last = CreateSignal("Lovelace");
        var role = CreateSignal(0);
        string[] roles = { "Engineer", "Designer", "Manager" };
        var saved = CreateSignal(("Ada", "Lovelace", 0));

        var fullName = CreateMemo(() => $"{first.Value.Trim()} {last.Value.Trim()}".Trim());
        var valid = CreateMemo(() => first.Value.Trim().Length > 0 && last.Value.Trim().Length > 0);
        var dirty = CreateMemo(() => saved.Value != (first.Value, last.Value, role.Value));

        var firstField = new TextField("First name").BindValue(first);
        firstField.Bind(f => f.Invalid = first.Value.Trim().Length == 0);
        var lastField = new TextField("Last name").BindValue(last);
        lastField.Bind(f => f.Invalid = last.Value.Trim().Length == 0);

        var form = Ui.Column(14,
            Ui.Row(12, Field("First name", firstField, null).Grow(), Field("Last name", lastField, null).Grow()),
            Field("Role", new Segmented(roles).BindSelected(role), null));

        var save = new Button("Save changes", "check");
        save.Bind(b => b.Enabled = valid.Value && dirty.Value);
        save.Clicked += () =>
        {
            Batch(() =>
            {
                saved.Value = (first.Peek(), last.Peek(), role.Peek());
                AppState.UserName.Value = Untrack(() => fullName.Value);
            });
            AppState.Notify("Profile saved", $"Signed in as {Untrack(() => fullName.Value)}.", Tone.Success);
        };
        var reset = new Button("Reset", variant: ButtonVariant.Ghost);
        reset.Bind(b => b.Enabled = dirty.Value);
        reset.Clicked += () => Batch(() =>
        {
            first.Value = saved.Peek().Item1;
            last.Value = saved.Peek().Item2;
            role.Value = saved.Peek().Item3;
        });
        form.AddChild(Ui.Row(8, save, reset, Ui.Spacer(),
            Ui.Text(() => dirty.Value ? "Unsaved changes" : "All changes saved", "text.small")));

        var avatar = new Avatar("Ada Lovelace", 56f);
        avatar.Bind(a => a.Name = fullName.Value.Length > 0 ? fullName.Value : "?");
        var badge = new Badge("Engineer", Tone.Primary);
        badge.Bind(b => b.Text = roles[role.Value]);
        var preview = new Card(style: "card-muted");
        preview.Gap = 10;
        preview.AddChild(Ui.Row(0, avatar, Ui.Spacer()));
        preview.AddChild(Ui.Text(() => fullName.Value.Length > 0 ? fullName.Value : "Your name", "text.heading"));
        preview.AddChild(Ui.Row(8, badge, Ui.Spacer()));
        preview.AddChild(Ui.Text(() => $"{first.Value.Trim().ToLowerInvariant()}@blossom.dev", "text.muted"));

        var layout = new ColumnLayout("*, 260", 24);
        layout.AddChild(form);
        layout.AddChild(preview);
        card.AddChild(layout);
        return card;
    }

    private static Stack Field(string label, VisualElement input, VisualElement? hint)
    {
        var col = Ui.Column(6, Ui.Text(label, "text.label"), input);
        if (hint != null) col.AddChild(hint);
        return col;
    }

    private sealed class Disposer : IDisposable
    {
        private readonly Action _action;
        public Disposer(Action action) => _action = action;
        public void Dispose() => _action();
    }
}
