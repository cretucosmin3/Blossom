using System;

namespace Blossom.Testing.Models;

public enum TodoColumn
{
    Backlog,
    InProgress,
    Done
}

public class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public TodoColumn Column { get; set; } = TodoColumn.Backlog;
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
