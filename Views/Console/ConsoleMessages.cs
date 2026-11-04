using Spectre.Console;

namespace RoomReservationManager.Views;

public partial class ConsoleView
{
    public bool Confirm(string question) => AnsiConsole.Confirm(Markup.Escape(question), defaultValue: false);

    public void ShowError(string message) => AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");

    public void ShowInfo(string message) => AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");

    public void Clear() => AnsiConsole.Clear();
}