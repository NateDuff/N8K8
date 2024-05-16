namespace N8.Shared;

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);
