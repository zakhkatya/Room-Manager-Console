using System.Globalization;
using RoomReservationManager.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RoomReservationManager.Views;

public class ConsoleView : IAppView
{

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

    // ---------- Tables (paged) ----------

    public void ShowRooms(IReadOnlyList<Room> rooms) =>
        ShowPaged("Rooms", rooms, pageSize: 10,
            columns: ["ID", "Name", "Capacity"],
            toRow: r => [r.Id.ToString(), Markup.Escape(r.RoomName), r.Capacity.ToString()]);

    public void ShowReservations(IReadOnlyList<Reservation> reservations) =>
        ShowPaged("Reservations", reservations, pageSize: 8,
            columns: ["ID", "Event", "Reserved by", "Start", "End", "Number of people", "Room", "Notes"],
            toRow: r =>
            [
                r.Id.ToString(),
                Markup.Escape(r.EventName),
                Markup.Escape(r.ReservedName),
                r.StartsAt.ToString("dd.MM.yyyy HH:mm"),
                r.EndsAt.ToString("dd.MM.yyyy HH:mm"),
                r.NumPeople.ToString(),
                r.RoomId.ToString(),
                Markup.Escape(r.Notes)
            ]);

    // Generic pager: arrow keys switch pages and only the table is redrawn (Live display),
    // not the whole screen.
    private static void ShowPaged<T>(
        string title,
        IReadOnlyList<T> items,
        int pageSize,
        string[] columns,
        Func<T, string[]> toRow)
    {
        AnsiConsole.Write(new Rule($"[yellow]{Markup.Escape(title)}[/]").LeftJustified());

        var pageCount = Math.Max(1, (items.Count + pageSize - 1) / pageSize);
        var page = 0;

        IRenderable Render()
        {
            var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
            foreach (var c in columns) table.AddColumn($"[bold]{c}[/]");

            var pageItems = items.Skip(page * pageSize).Take(pageSize).ToList();
            foreach (var item in pageItems) table.AddRow(toRow(item));

            // keep the table the same height on the last page so the layout doesn't jump
            if (pageCount > 1)
                for (var i = pageItems.Count; i < pageSize; i++)
                    table.AddEmptyRow();

            var footer = pageCount > 1
                ? $"[grey]Page {page + 1}/{pageCount} ({items.Count} total)  |  Left/Right: change page  |  Home/End: first/last  |  Esc: back[/]"
                : $"[grey]{items.Count} total[/]";

            return new Rows(table, new Markup(footer));
        }

        if (pageCount == 1)
        {
            AnsiConsole.Write(Render());
            return;
        }

        AnsiConsole.Live(Render()).Start(ctx =>
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;

                if (key is ConsoleKey.Escape or ConsoleKey.Enter or ConsoleKey.Q)
                    break;

                var newPage = key switch
                {
                    ConsoleKey.RightArrow or ConsoleKey.DownArrow or ConsoleKey.PageDown => Math.Min(page + 1, pageCount - 1),
                    ConsoleKey.LeftArrow or ConsoleKey.UpArrow or ConsoleKey.PageUp => Math.Max(page - 1, 0),
                    ConsoleKey.Home => 0,
                    ConsoleKey.End => pageCount - 1,
                    _ => page
                };

                if (newPage == page) continue;

                page = newPage;
                ctx.UpdateTarget(Render());
                ctx.Refresh();
            }
        });
    }

    // ---------- Pickers (arrow keys instead of typing IDs) ----------

    public Room PickRoom(IReadOnlyList<Room> rooms, string action) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<Room>()
                .Title($"Which room do you want to {Markup.Escape(action)}?")
                .PageSize(10)
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
    public ReservationSlot AskSlot(Reservation? existing = null)
    {
        var startsAt = AskStart(existing?.StartsAt);
        var endsAt = AskEnd(startsAt, existing?.EndsAt);
        var numPeople = AskInt("Number of people:", existing?.NumPeople);
        return new ReservationSlot(startsAt, endsAt, numPeople);
    }

    public Room PickAvailableRoom(IReadOnlyList<Room> availableRooms, ReservationSlot slot, int? currentRoomId = null) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<Room>()
                .Title($"Available rooms for [bold]{slot.NumPeople}[/] people, " +
                       $"{slot.StartsAt:dd.MM HH:mm}-{slot.EndsAt:HH:mm}:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to see more rooms)[/]")
                .UseConverter(r => RoomLabel(r) + (r.Id == currentRoomId ? " (current)" : ""))
                .AddChoices(availableRooms
                    .OrderByDescending(r => r.Id == currentRoomId)
                    .ThenBy(r => r.Capacity)));

    public ReservationDetails AskDetails(Reservation? existing = null)
    {
        var eventName = AskText("Event name:", existing?.EventName);
        var reservedName = AskText("Reserved by:", existing?.ReservedName);
        var notes = AskText("Notes:", existing?.Notes, allowEmpty: true);
        return new ReservationDetails(eventName, reservedName, notes);
    }

    public string AskRoomName(string? current = null) => AskText("Room name:", current);

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

    private static string RoomLabel(Room r) => $"{Markup.Escape(r.RoomName)} (capacity {r.Capacity})";

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

    private static DateTime AskStart(DateTime? current)
    {
        var day = AskDay("Start", current);

        var time = AskTime("Start",
            t => current is not null || day + t >= DateTime.Now,
            current?.TimeOfDay);

        return day + time;
    }

    private static DateTime AskEnd(DateTime start, DateTime? current)
    {
        TimeSpan? currentTime = current is not null && current.Value.Date == start.Date
            ? current.Value.TimeOfDay
            : null;

        var time = AskTime("End", t => start.Date + t > start, currentTime);

        return start.Date + time;
    }

    private static DateTime AskDay(string label, DateTime? current)
    {
        var days = Enumerable.Range(0, 60).Select(i => DateTime.Today.AddDays(i)).ToList();

        if (current.HasValue)
        {
            days.Remove(current.Value.Date);
            days.Insert(0, current.Value.Date);
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<DateTime>()
                .Title($"{label} date:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to see more days)[/]")
                .UseConverter(d => DayLabel(d, current))
                .AddChoices(days));
    }

    private static string DayLabel(DateTime d, DateTime? current)
    {
        var text = d.ToString("ddd dd.MM.yyyy", CultureInfo.InvariantCulture);
        if (d == DateTime.Today) text += " (today)";
        else if (d == DateTime.Today.AddDays(1)) text += " (tomorrow)";
        if (current.HasValue && d == current.Value.Date) text += " (current)";
        return text;
    }

    private static TimeSpan AskTime(string label, Func<TimeSpan, bool> isValid, TimeSpan? current)
    {
        var options = new List<TimeSpan>();
        for (var h = 0; h < 24; h++)
            for (var m = 0; m < 60; m += 5)
                options.Add(new TimeSpan(h, m, 0));
        options = options.Where(isValid).ToList();

        TimeSpan? cur = current.HasValue
            ? new TimeSpan(current.Value.Hours, current.Value.Minutes, 0)
            : null;
        var curValid = cur.HasValue && isValid(cur.Value);
        if (curValid && !options.Contains(cur!.Value))
        {
            options.Add(cur.Value);
            options.Sort();
        }

        if (options.Count == 0)
            return new TimeSpan(23, 59, 0);

        var hours = options.Select(t => t.Hours).Distinct().ToList();
        if (curValid && hours.Remove(cur!.Value.Hours))
            hours.Insert(0, cur.Value.Hours);

        var hour = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title($"{label} hour:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to see more hours)[/]")
                .UseConverter(h => $"{h:00}:__" + (curValid && cur!.Value.Hours == h ? " (current)" : ""))
                .AddChoices(hours));

        var minutes = options.Where(t => t.Hours == hour).Select(t => t.Minutes).ToList();
        if (curValid && cur!.Value.Hours == hour && minutes.Remove(cur.Value.Minutes))
            minutes.Insert(0, cur.Value.Minutes);

        var minute = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title($"{label} minutes:")
                .PageSize(12)
                .UseConverter(m => $"{hour:00}:{m:00}" +
                    (curValid && cur!.Value.Hours == hour && cur.Value.Minutes == m ? " (current)" : ""))
                .AddChoices(minutes));

        return new TimeSpan(hour, minute, 0);
    }
}