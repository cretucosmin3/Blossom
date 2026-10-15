using Blossom.Reactive;

namespace SnakeDemo;

public enum GamePhase { Menu, Playing, Paused, GameOver }

public enum Difficulty { Chill, Normal, Frantic }

/// <summary>
/// The state the game and the Blossom UI share. The game writes these signals; the UI reads them
/// through bindings, so every write schedules exactly the repaint it needs and nothing more.
/// </summary>
public sealed class GameState
{
    public Signal<GamePhase> Phase { get; } = new(GamePhase.Menu);
    public Signal<Difficulty> Difficulty { get; } = new(SnakeDemo.Difficulty.Normal);
    public Signal<int> Score { get; } = new(0);
    public Signal<int> Best { get; } = new(0);
    public Signal<int> Length { get; } = new(0);
    public Signal<int> Level { get; } = new(1);
    public Signal<bool> NewBest { get; } = new(false);
    public Signal<DeathCause> Death { get; } = new(DeathCause.None);

    public Signal<bool> ShowPerf { get; } = new(true);
    public Signal<PerfSnapshot> Perf { get; } = new(default);

    public static int PointsPerFood(Difficulty d) => d switch
    {
        SnakeDemo.Difficulty.Chill => 5,
        SnakeDemo.Difficulty.Frantic => 20,
        _ => 10,
    };

    /// <summary>Seconds per snake step at level 1; each level shaves a little off.</summary>
    public static float BaseStepSeconds(Difficulty d) => d switch
    {
        SnakeDemo.Difficulty.Chill => 0.15f,
        SnakeDemo.Difficulty.Frantic => 0.07f,
        _ => 0.11f,
    };
}
