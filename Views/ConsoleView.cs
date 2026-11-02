using System.Globalization;
using RoomReservationManager.Models;
using Spectre.Console;

namespace RoomReservationManager.Views;

public class ConsoleView : IAppView
{
    private const string DateFormat = "yyyy-MM-dd HH:mm";

    // ---------- Menus ----------

    public MainChoice ShowMainMenu()
    {
        AnsiConsole.Write(new FigletText("Room Manager").Color(Color.Yellow));

        return AnsiConsole.Prompt(
            new SelectionPrompt<MainChoice>()
                .Title("[bold]Main menu[/]")
                .UseConverter(c => c switch
                {
                    MainChoice.Reservations => "Manage reservations",
                    MainChoice.Rooms => "Manage rooms",
                    _ => "Exit"
                })
                .AddChoices(MainChoice.Reservations, MainChoice.Rooms, MainChoice.Exit));
    }

    public SectionChoice ShowSectionMenu(string title)
    {
        AnsiConsole.Write(new Rule($"[yellow]{Markup.Escape(title)}[/]").LeftJustified());

        return AnsiConsole.Prompt(
            new SelectionPrompt<SectionChoice>()
                .Title("[bold]What would you like to do?[/]")
                .UseConverter(c => c switch
                {
                    SectionChoice.Show => "Show all",
                    SectionChoice.Add => "Add",
                    SectionChoice.Edit => "Edit",
                    SectionChoice.Delete => "Delete",
                    _ => "Back"
                })
                .AddChoices(SectionChoice.Show, SectionChoice.Add, SectionChoice.Edit, SectionChoice.Delete, SectionChoice.Back));
    }

    // ---------- Tables ----------

    public void ShowRooms(IReadOnlyList<Room> rooms)
    {
        AnsiConsole.Write(new Rule("[yellow]Rooms[/]").LeftJustified());

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("[bold]ID[/]");
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Capacity[/]");

        foreach (var r in rooms)
            table.AddRow(r.Id.ToString(), r.Capacity.ToString());

        AnsiConsole.Write(table);
    }

    public void ShowReservations(IReadOnlyList<Reservation> reservations)
    {
        AnsiConsole.Write(new Rule("[yellow]Reservations[/]").LeftJustified());

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("[bold]ID[/]");
        table.AddColumn("[bold]Event[/]");
        table.AddColumn("[bold]Reserved by[/]");
        table.AddColumn("[bold]Start[/]");
        table.AddColumn("[bold]End[/]");
        table.AddColumn("[bold]Number of people[/]");
        table.AddColumn("[bold]Room[/]");
        table.AddColumn("[bold]Notes[/]");

        foreach (var r in reservations)
        {
            table.AddRow(
                r.Id.ToString(),
                Markup.Escape(r.EventName),
                Markup.Escape(r.ReservedName),
                r.StartsAt.ToString("dd.MM.yyyy HH:mm"),
                r.EndsAt.ToString("dd.MM.yyyy HH:mm"),
                r.NumPeople.ToString(),
                r.RoomId.ToString(),
                Markup.Escape(r.Notes));
        }

        AnsiConsole.Write(table);
    }

    // ---------- Pickers (arrow keys instead of typing IDs) ----------

    public Room PickRoom(IReadOnlyList<Room> rooms, string action) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<Room>()
                .Title($"Which room do you want to {Markup.Escape(action)}?")
                .UseConverter(RoomLabel)
                .AddChoices(rooms));

    public Reservation PickReservation(IReadOnlyList<Reservation> reservations, string action) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<Reservation>()
                .Title($"Which reservation do you want to {Markup.Escape(action)}?")
                .PageSize(10)
                .UseConverter(r =>
                    $"#{r.Id} {Markup.Escape(r.EventName)} - " +
                    $"{r.StartsAt:dd.MM HH:mm} (Room {r.RoomId})")
                .AddChoices(reservations));

    // ---------- Forms ----------

    public ReservationInput AskReservation(IReadOnlyList<Room> rooms, Reservation? existing = null)
    {
        var eventName = AskText("Event name:", existing?.EventName);
        var reservedName = AskText("Reserved by:", existing?.ReservedName);
        var startsAt = AskDate("Start", existing?.StartsAt);
        var endsAt = AskDate("End", existing?.EndsAt, after: startsAt);
        var numPeople = AskInt("Number of people:", existing?.NumPeople);
        var notes = AskText("Notes:", existing?.Notes, allowEmpty: true);

        var room = AnsiConsole.Prompt(
            new SelectionPrompt<Room>()
                .Title("Room:")
                .UseConverter(RoomLabel)
                .AddChoices(rooms));

        return new ReservationInput(startsAt, endsAt, eventName, reservedName, notes, numPeople, room.Id);
    }

    public int AskRoomCapacity(int? current = null) => AskInt("Room capacity:", current);

    // ---------- Messages ----------

    public bool Confirm(string question) => AnsiConsole.Confirm(Markup.Escape(question), defaultValue: false);

    public void ShowSuccess(string message) => AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");

    public void ShowError(string message) => AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");

    public void ShowInfo(string message) => AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");

    public void Pause()
    {
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    public void Clear() => AnsiConsole.Clear();

    // ---------- Helpers ----------

    private static string RoomLabel(Room r) => $"Capacity: {r.Capacity}";

    private static string AskText(string label, string? current = null, bool allowEmpty = false)
    {
        var prompt = new TextPrompt<string>(label);
        if (allowEmpty) prompt.AllowEmpty();
        if (!string.IsNullOrEmpty(current)) prompt.DefaultValue(current);
        return AnsiConsole.Prompt(prompt);
    }

    private static int AskInt(string label, int? current = null, int min = 1)
    {
        var prompt = new TextPrompt<int>(label)
            .ValidationErrorMessage("[red]Please enter a valid whole number.[/]")
            .Validate(n => n >= min
                ? ValidationResult.Success()
                : ValidationResult.Error($"[red]Must be at least {min}.[/]"));

        if (current.HasValue)
            prompt.DefaultValue(current.Value);

        return AnsiConsole.Prompt(prompt);
    }

    private static DateTime AskDate(string label, DateTime? current = null, DateTime? after = null)
    {
        var prompt = new TextPrompt<string>($"{label} date and time [grey]({DateFormat})[/]:")
            .Validate(input =>
            {
                if (!DateTime.TryParseExact(input, DateFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    return ValidationResult.Error($"[red]Invalid date. Use {DateFormat}[/]");

                if (after.HasValue && date <= after.Value)
                    return ValidationResult.Error("[red]End time must be after start time.[/]");

                return ValidationResult.Success();
            });

        if (current.HasValue)
            prompt.DefaultValue(current.Value.ToString(DateFormat));

        var text = AskDateText(prompt);
        return DateTime.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);
    }

    private static string AskDateText(TextPrompt<string> prompt) => AnsiConsole.Prompt(prompt);
}