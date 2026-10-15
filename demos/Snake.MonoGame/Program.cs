using System;

namespace SnakeDemo;

internal static class Program
{
    private static int Main(string[] args)
    {
        bool smokeTest = Array.IndexOf(args, "--smoke-test") >= 0;
        using var game = new Game1(smokeTest: smokeTest);
        game.Run();
        return game.ExitCode;
    }
}
