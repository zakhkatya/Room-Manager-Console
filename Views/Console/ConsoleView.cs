using System.Globalization;
using RoomReservationManager.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RoomReservationManager.Views;

public partial class ConsoleView : IAppView
{
    private int _mainIndex;

    //Main menu: start screen
    public MainChoice ShowMainMenu(Func<IReadOnlyList<Room>> getRooms, Func<IReadOnlyList<Reservation>> getReservations)
    {
        var options = new (MainChoice Choice, string Label)[]
        {
            (MainChoice.Reservations, "Manage reservations"),
            (MainChoice.Rooms, "Manage rooms"),
            (MainChoice.Exit, "Exit")
        };

        var statusPage = 0;
        var lastW = -1;
        var lastH = -1;

        //Draw table of rooms and business at real time
        void Draw()
        {
            var width = GetWidth();
            var height = GetHeight();
            if (width != lastW || height != lastH)
            {
                AnsiConsole.Clear();
                lastW = width;
                lastH = height;
            }

            var now = DateTime.Now;
            var statuses = BuildStatuses(getRooms(), getReservations(), now);

            var pageSize = Math.Clamp(height - 14, 3, 20);
            var pageCount = Math.Max(1, (statuses.Count + pageSize - 1) / pageSize);
            statusPage = Math.Clamp(statusPage, 0, pageCount - 1);

            var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Expand();
            table.AddColumn(new TableColumn("[bold]Room[/]").NoWrap());
            table.AddColumn(new TableColumn("[bold]Capacity[/]").NoWrap());
            table.AddColumn(new TableColumn("[bold]Status[/]").NoWrap());
            table.AddColumn(new TableColumn("[bold]Details[/]").NoWrap());

            if (statuses.Count == 0)
                table.AddRow("[grey](no rooms yet)[/]", "", "", "");

            foreach (var s in statuses.Skip(statusPage * pageSize).Take(pageSize))
            {
                string status;
                string details;

                if (s.Current is not null && s.BusyUntil is not null)
                {
                    status = "[red]● Occupied[/]";
                    details = $"until {FormatTime(s.BusyUntil.Value, now)} - {s.Current.EventName} ({s.Current.ReservedName})";
                }
                else
                {
                    status = "[green]● Free[/]";
                    details = s.Next is null
                        ? "no upcoming reservations"
                        : $"free until {FormatTime(s.Next.StartsAt, now)} (next: {s.Next.EventName})";
                }

                table.AddRow(
                    Markup.Escape(s.Room.RoomName),
                    s.Room.Capacity.ToString(),
                    status,
                    Markup.Escape(details));
            }

            var busy = statuses.Count(s => s.Current is not null);
            var header = $"[bold yellow]ROOM MANAGER[/]   [grey]{now.ToString("ddd dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)}[/]";
            var summary = $"[grey]{busy} of {statuses.Count} rooms occupied now" +
                          (pageCount > 1 ? $"  |  page {statusPage + 1}/{pageCount} (Left/Right)" : "") + "[/]";

            var lines = new List<IRenderable>
            {
                new Markup(PadLine(header, width)),
                new Rule("[yellow]Rooms right now[/]").LeftJustified(),
                table,
                new Markup(PadLine(summary, width)),
                new Markup(PadLine("", width))
            };

            for (var i = 0; i < options.Length; i++)
            {
                var text = i == _mainIndex
                    ? $"[black on yellow] > {options[i].Label} [/]"
                    : $"   {options[i].Label}";
                lines.Add(new Markup(PadLine(text, width)));
            }

            lines.Add(new Markup(PadLine("[grey]Up/Down: choose  |  Enter: open[/]", width)));

            Redraw(new Rows(lines));
        }

        AnsiConsole.Clear();
        AnsiConsole.Cursor.Hide();
        try
        {
            var lastDraw = DateTime.MinValue;
            var dirty = true;

            while (true)
            {
                if (dirty || (DateTime.Now - lastDraw).TotalSeconds >= 1)
                {
                    Draw();
                    lastDraw = DateTime.Now;
                    dirty = false;
                }

                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(30);
                    continue;
                }

                var key = Console.ReadKey(true).Key;
                dirty = true;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        _mainIndex = Math.Max(0, _mainIndex - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        _mainIndex = Math.Min(options.Length - 1, _mainIndex + 1);
                        break;
                    case ConsoleKey.LeftArrow or ConsoleKey.PageUp:
                        statusPage--;
                        break;
                    case ConsoleKey.RightArrow or ConsoleKey.PageDown:
                        statusPage++;
                        break;
                    case ConsoleKey.Enter:
                        return options[_mainIndex].Choice;
                }
            }
        }
        finally
        {
            AnsiConsole.Cursor.Show();
        }
    }

    private record RoomNow(Room Room, Reservation? Current, DateTime? BusyUntil, Reservation? Next);

    private static List<RoomNow> BuildStatuses(IReadOnlyList<Room> rooms, IReadOnlyList<Reservation> reservations, DateTime now)
    {
        return rooms.Select(room =>
        {
            var mine = reservations
                .Where(x => x.RoomId == room.Id)
                .OrderBy(x => x.StartsAt)
                .ToList();

            var current = mine.FirstOrDefault(x => x.StartsAt <= now && now < x.EndsAt);

            DateTime? busyUntil = null;
            if (current is not null)
            {
                busyUntil = current.EndsAt;
                foreach (var following in mine.Where(x => x.StartsAt > current.StartsAt))
                {
                    if (following.StartsAt > busyUntil) break;
                    if (following.EndsAt > busyUntil) busyUntil = following.EndsAt;
                }
            }

            var next = mine.FirstOrDefault(x => x.StartsAt > (busyUntil ?? now));
            return new RoomNow(room, current, busyUntil, next);
        }).ToList();
    }
}