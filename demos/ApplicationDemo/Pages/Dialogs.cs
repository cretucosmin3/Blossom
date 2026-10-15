using System;
using Blossom.Core;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using static Blossom.Reactive.ReactiveEngine;
using Blossom.Desktop;

namespace Blossom.Testing.Pages;

/// <summary>Dialogs shared by several pages.</summary>
public static class Dialogs
{
    public static void NewTask(View? view, string category = "UI")
    {
        if (view == null) return;

        var title = CreateSignal("");
        var picked = CreateSignal(Array.IndexOf(AppState.Categories, category) is var i and >= 0 ? i : 2);

        var dialog = new Dialog("New task", "Tasks land in Backlog. Press Enter to create.");
        var field = new TextField("What needs doing?", "plus").BindValue(title);
        dialog.Body.AddChild(Ui.Column(6, Ui.Text("Title", "text.label"), field));
        dialog.Body.AddChild(Ui.Column(6, Ui.Text("Category", "text.label"),
            new Segmented(AppState.Categories).BindSelected(picked)));

        void Create()
        {
            if (title.Peek().Trim().Length == 0) return;
            AppState.AddTask(title.Peek(), AppState.Categories[picked.Peek()]);
            AppState.Notify("Task created", $"“{title.Peek().Trim()}” was added to Backlog.", Tone.Success);
            dialog.Close();
        }

        dialog.AddAction("Cancel", ButtonVariant.Ghost);
        var create = dialog.AddAction("Create task", ButtonVariant.Primary, Create, closes: false);
        create.Bind(b => b.Enabled = title.Value.Trim().Length > 0);
        field.Submitted += Create;
        dialog.Submit = Create;

        dialog.Closed += () => Shell.Post(dialog.Dispose);
        dialog.Open(view);
        view.SetActiveKeyboardElement(field);
    }

    public static void Confirm(View? view, string title, string message, string action, Action onConfirm)
    {
        if (view == null) return;
        var dialog = new Dialog(title, message, 400f);
        dialog.AddAction("Cancel", ButtonVariant.Ghost);
        dialog.AddAction(action, ButtonVariant.Destructive, onConfirm);
        dialog.Closed += () => Shell.Post(dialog.Dispose);
        dialog.Open(view);
    }
}
