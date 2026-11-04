using System.Globalization;
using RoomReservationManager.Models;
using Spectre.Console;

namespace RoomReservationManager.Views;

public partial class ConsoleView
{
    private string _formTitle = "";
    private readonly List<(string Label, string Value)> _form = new();

    public void BeginForm(string title)
    {
        _formTitle = title;
        _form.Clear();
    }

    public string AskRoomName(string? current = null) => AskText("Room name:", "Name", current);

    public int AskRoomCapacity(int? current = null) => AskInt("Room capacity:", "Capacity", current);

    public ReservationSlot AskSlot(Reservation? existing = null)
    {
        var day = AskDay(existing?.StartsAt);
        SetField("Date", day.ToString("ddd dd.MM.yyyy", CultureInfo.InvariantCulture));

        var startTime = AskTime("Start", t => existing is not null || day + t >= DateTime.Now, existing?.StartsAt.TimeOfDay);
        var startsAt = day + startTime;

        TimeSpan? currentEnd = existing is not null && existing.EndsAt.Date == day
            ? existing.EndsAt.TimeOfDay
            : null;
        var endTime = AskTime("End", t => day + t > startsAt, currentEnd);
        var endsAt = day + endTime;

        var numPeople = AskInt("Number of people:", "People", existing?.NumPeople);

        DrawForm();
        return new ReservationSlot(startsAt, endsAt, numPeople);
    }

    public Room PickAvailableRoom(IReadOnlyList<Room> availableRooms, ReservationSlot slot, int? currentRoomId = null)
    {
        DrawForm();

        var room = AnsiConsole.Prompt(
            new SelectionPrompt<Room>()
                .Title($"Available rooms ({availableRooms.Count}) for [bold]{slot.NumPeople}[/] people, " +
                       $"{slot.StartsAt:dd.MM HH:mm}-{slot.EndsAt:HH:mm}:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to see more rooms)[/]")
                .UseConverter(r => RoomLabel(r) + (r.Id == currentRoomId ? " (current)" : ""))
                .AddChoices(availableRooms
                    .OrderByDescending(r => r.Id == currentRoomId)
                    .ThenBy(r => r.Capacity)));

        SetField("Room", $"{room.RoomName} (capacity {room.Capacity})");
        return room;
    }

    public ReservationDetails AskDetails(Reservation? existing = null)
    {
        var eventName = AskText("Event name:", "Event", existing?.EventName);
        var reservedName = AskText("Reserved by:", "Reserved by", existing?.ReservedName);
        var notes = AskNotes(existing?.Notes ?? "");
        return new ReservationDetails(eventName, reservedName, notes);
    }

    private string AskNotes(string current)
    {
        var hasCurrent = !string.IsNullOrWhiteSpace(current);
        if (hasCurrent) SetField("Notes", current);

        DrawForm();
        var want = AnsiConsole.Confirm(
            hasCurrent ? "Change the notes?" : "Add notes to the reservation?",
            defaultValue: false);

        if (!want)
        {
            if (!hasCurrent) SetField("Notes", "-");
            return current;
        }

        return AskText("Notes:", "Notes", current: null, allowEmpty: true);
    }

    public FormDecision ReviewForm(string question)
    {
        DrawForm();

        return AnsiConsole.Prompt(
            new SelectionPrompt<FormDecision>()
                .Title($"[bold]{Markup.Escape(question)}[/]")
                .UseConverter(d => d switch
                {
                    FormDecision.Save => "Yes, save",
                    FormDecision.Restart => "No, start over",
                    _ => "Cancel"
                })
                .AddChoices(FormDecision.Save, FormDecision.Restart, FormDecision.Cancel));
    }

    public bool ConfirmDelete(string title, string description)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[red]{Markup.Escape(title)}[/]").LeftJustified());
        AnsiConsole.MarkupLine(Markup.Escape(description));
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm("Are you sure you want to delete it?", defaultValue: false);
    }

    private void SetField(string label, string value)
    {
        var i = _form.FindIndex(f => f.Label == label);
        if (i >= 0) _form[i] = (label, value);
        else _form.Add((label, value));
    }

    private void DrawForm()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[yellow]{Markup.Escape(_formTitle)}[/]").LeftJustified());

        if (_form.Count > 0)
        {
            var grid = new Grid().AddColumn().AddColumn();
            foreach (var (label, value) in _form)
                grid.AddRow($"[grey]{Markup.Escape(label)}:[/]", $"[bold]{Markup.Escape(value)}[/]");

            AnsiConsole.Write(new Panel(grid).Header("Your choices").Border(BoxBorder.Rounded));
        }

        AnsiConsole.WriteLine();
    }

    private static string RoomLabel(Room r) => $"{Markup.Escape(r.RoomName)} (capacity {r.Capacity})";

    private string AskText(string label, string field, string? current = null, bool allowEmpty = false)
    {
        DrawForm();

        var prompt = new TextPrompt<string>(Markup.Escape(label));
        if (allowEmpty) prompt.AllowEmpty();
        if (!string.IsNullOrEmpty(current)) prompt.DefaultValue(current);

        var value = AnsiConsole.Prompt(prompt);
        SetField(field, value.Length == 0 ? "-" : value);
        return value;
    }

    private int AskInt(string label, string field, int? current = null, int min = 1)
    {
        DrawForm();

        var prompt = new TextPrompt<int>(Markup.Escape(label))
            .ValidationErrorMessage("[red]Please enter a valid whole number.[/]")
            .Validate(n => n >= min
                ? ValidationResult.Success()
                : ValidationResult.Error($"[red]Must be at least {min}.[/]"));

        if (current.HasValue)
            prompt.DefaultValue(current.Value);

        var value = AnsiConsole.Prompt(prompt);
        SetField(field, value.ToString());
        return value;
    }

    private DateTime AskDay(DateTime? current)
    {
        var days = Enumerable.Range(0, 60).Select(i => DateTime.Today.AddDays(i)).ToList();

        if (current.HasValue)
        {
            days.Remove(current.Value.Date);
            days.Insert(0, current.Value.Date);
        }

        DrawForm();

        return AnsiConsole.Prompt(
            new SelectionPrompt<DateTime>()
                .Title("Date:")
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

    private TimeSpan AskTime(string label, Func<TimeSpan, bool> isValid, TimeSpan? current)
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
        {
            var fallback = new TimeSpan(23, 59, 0);
            SetField(label, $"{fallback:hh\\:mm}");
            return fallback;
        }

        var hours = options.Select(t => t.Hours).Distinct().ToList();
        if (curValid && hours.Remove(cur!.Value.Hours))
            hours.Insert(0, cur.Value.Hours);

        DrawForm();
        var hour = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title($"{label} hour:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to see more hours)[/]")
                .UseConverter(h => $"{h:00}:__" + (curValid && cur!.Value.Hours == h ? " (current)" : ""))
                .AddChoices(hours));

        SetField(label, $"{hour:00}:__");

        var minutes = options.Where(t => t.Hours == hour).Select(t => t.Minutes).ToList();
        if (curValid && cur!.Value.Hours == hour && minutes.Remove(cur.Value.Minutes))
            minutes.Insert(0, cur.Value.Minutes);

        DrawForm();
        var minute = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title($"{label} minutes:")
                .PageSize(12)
                .UseConverter(m => $"{hour:00}:{m:00}" +
                    (curValid && cur!.Value.Hours == hour && cur.Value.Minutes == m ? " (current)" : ""))
                .AddChoices(minutes));

        SetField(label, $"{hour:00}:{minute:00}");
        return new TimeSpan(hour, minute, 0);
    }
}