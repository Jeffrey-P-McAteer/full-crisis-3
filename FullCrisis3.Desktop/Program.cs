using FullCrisis3.Core;
using System;

namespace FullCrisis3.Desktop;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new Game1();
        game.Run();
    }
}