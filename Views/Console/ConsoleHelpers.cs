using Spectre.Console;
using Spectre.Console.Rendering;

namespace RoomReservationManager.Views;

public partial class ConsoleView
{
    private static int GetWidth()
    {
        try { return Math.Max(40, Console.WindowWidth); }
        catch { return 100; }
    }

    private static int GetHeight()
    {
        try { return Math.Max(10, Console.WindowHeight); }
        catch { return 30; }
    }

    private static void Redraw(IRenderable screen)
    {
        Console.SetCursorPosition(0, 0);
        AnsiConsole.Write(screen);
        Console.Write("\u001b[J");
    }

    private static string PadLine(string markup, int width)
    {
        var visible = Markup.Remove(markup).Length;
        return markup + new string(' ', Math.Max(0, width - 1 - visible));
    }

    private static string FormatTime(DateTime t, DateTime now) =>
        t.Date == now.Date ? t.ToString("HH:mm") : t.ToString("dd.MM HH:mm");

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";
}