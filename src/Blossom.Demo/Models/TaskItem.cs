using System;

namespace Blossom.Testing.Models;

public class TaskItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Column { get; set; } = "Backlog"; // "Backlog", "In Progress", "Done"
    public string Category { get; set; } = "Core"; // "Core", "Reactive", "UI", "Engine"
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public TaskItem(string title, string column = "Backlog", string category = "Core")
    {
        Title = title;
        Column = column;
        Category = category;
    }
}
