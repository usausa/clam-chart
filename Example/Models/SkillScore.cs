namespace Example.Models;

public sealed class SkillScore
{
    public static ChartAccessorCollection<SkillScore> Accessors { get; } = new()
    {
        { "skill", static x => x.Skill },
        { "score", static x => x.Score }
    };

    public required string Skill { get; init; }

    public double Score { get; init; }
}
