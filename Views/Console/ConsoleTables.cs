using RoomReservationManager.Models;
using Spectre.Console;

namespace RoomReservationManager.Views;

public partial class ConsoleView
{
    //Show table of rooms
    public BrowseResult<Room> BrowseRooms(IReadOnlyList<Room> rooms, BrowseState state) =>
        Browse(
            title: "Rooms",
            addLabel: "Add room",
            columns: ["Name", "Capacity"],
            all: rooms,
            toRow: r => [r.RoomName, r.Capacity.ToString()],
            state: state);

    //Show table of reservations
    public BrowseResult<Reservation> BrowseReservations(
        IReadOnlyList<Reservation> reservations,
        IReadOnlyDictionary<int, string> roomNames,
        BrowseState state) =>
        Browse(
            title: "Reservations",
            addLabel: "Add reservation",
            columns: ["Event", "Reserved by", "Date", "Time", "People", "Room", "Notes"],
            all: reservations,
            toRow: r =>
            [
                Truncate(r.EventName, 28),
                Truncate(r.ReservedName, 20),
                r.StartsAt.ToString("dd.MM.yyyy"),
                $"{r.StartsAt:HH:mm}-{r.EndsAt:HH:mm}",
                r.NumPeople.ToString(),
                roomNames.TryGetValue(r.RoomId, out var name) ? name : $"#{r.RoomId}",
                Truncate(r.Notes, 30)
            ],
            state: state);

    //Show a result of searching
    private BrowseResult<T> Browse<T>(
        string title,
        string addLabel,
        string[] columns,
        IReadOnlyList<T> all,
        Func<T, string[]> toRow,
        BrowseState state) where T : class
    {
        var data = all.Select(item => (Item: item, Cells: toRow(item))).ToList();

        var message = state.Message;
        var messageIsError = state.MessageIsError;
        state.Message = null;

        var searching = false;
        var menuOpen = false;
        var menuIndex = 0;
        var lastW = -1;
        var lastH = -1;

        List<(T Item, string[] Cells)> Filter()
        {
            var q = state.Query.Trim();
            if (q.Length == 0) return data;
            return data
                .Where(d => d.Cells.Any(c => c.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        int PageSize() => Math.Clamp(GetHeight() - 12, 3, 15);

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

            var list = Filter();
            var n = list.Count;
            state.Index = Math.Clamp(state.Index, 0, n);

            var pageSize = PageSize();
            var onAdd = state.Index == n;
            var pivot = onAdd ? Math.Max(n - 1, 0) : state.Index;
            var page = pivot / pageSize;
            var pageCount = Math.Max(1, (n + pageSize - 1) / pageSize);

            var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Expand();
            table.AddColumn(new TableColumn(" ").NoWrap());
            foreach (var c in columns)
                table.AddColumn(new TableColumn($"[bold]{Markup.Escape(c)}[/]").NoWrap());

            if (n == 0)
            {
                var empty = Enumerable.Repeat("", columns.Length + 1).ToArray();
                empty[1] = "[grey](nothing found)[/]";
                table.AddRow(empty);
            }

            var pageItems = list.Skip(page * pageSize).Take(pageSize).ToList();
            for (var i = 0; i < pageItems.Count; i++)
            {
                var selected = !onAdd && page * pageSize + i == state.Index;
                var cells = new List<string> { selected ? "[yellow]>[/]" : " " };

                foreach (var cell in pageItems[i].Cells)
                {
                    var text = Markup.Escape(cell.Length == 0 ? " " : cell);
                    cells.Add(selected ? $"[black on yellow]{text}[/]" : text);
                }

                table.AddRow(cells.ToArray());
            }

            var query = Markup.Escape(state.Query);
            var searchLine = searching
                ? $"[bold]Search:[/] {query}[yellow]_[/]   [grey]{n} of {data.Count}[/]"
                : state.Query.Length == 0
                    ? "[grey]Search: press / to search[/]"
                    : $"[bold]Search:[/] {query}   [grey]{n} of {data.Count}  (Esc clears the search)[/]";

            var addLine = onAdd
                ? $"[black on green] + {Markup.Escape(addLabel)} [/]"
                : $"[green] + {Markup.Escape(addLabel)} [/]";

            var menuLine = "";
            if (menuOpen)
            {
                string[] items = ["Edit", "Delete", "Cancel"];
                menuLine = "   " + string.Join("  ", items.Select((t, i) =>
                    i == menuIndex ? $"[black on yellow] {t} [/]" : $"[grey] {t} [/]"));
            }

            var messageLine = message is null
                ? ""
                : messageIsError
                    ? $"[red]{Markup.Escape(message)}[/]"
                    : $"[green]{Markup.Escape(message)}[/]";

            string hint;
            if (searching)
                hint = "Type to filter  |  Enter: done  |  Esc: clear search";
            else if (width >= 100)
                hint = "Up/Down: select  |  Left/Right: page  |  Enter: actions  |  E: edit  |  D: delete  |  A: add  |  /: search  |  Esc: back";
            else
                hint = "Enter: actions  |  /: search  |  Esc: back";

            var screen = new Rows(
                new Rule($"[yellow]{Markup.Escape(title)}[/] [grey](page {page + 1}/{pageCount})[/]").LeftJustified(),
                new Markup(PadLine(searchLine, width)),
                table,
                new Markup(PadLine(addLine, width)),
                new Markup(PadLine(menuLine, width)),
                new Markup(PadLine(messageLine, width)),
                new Markup(PadLine($"[grey]{Markup.Escape(hint)}[/]", width)));

            Redraw(screen);
        }

        AnsiConsole.Clear();
        AnsiConsole.Cursor.Hide();
        try
        {
            while (true)
            {
                Draw();

                var key = Console.ReadKey(true);
                message = null;

                var list = Filter();
                var n = list.Count;

                if (searching)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            state.Query = "";
                            state.Index = 0;
                            searching = false;
                            break;
                        case ConsoleKey.Enter:
                            searching = false;
                            break;
                        case ConsoleKey.Backspace:
                            if (state.Query.Length > 0)
                            {
                                state.Query = state.Query[..^1];
                                state.Index = 0;
                            }
                            break;
                        default:
                            if (!char.IsControl(key.KeyChar))
                            {
                                state.Query += key.KeyChar;
                                state.Index = 0;
                            }
                            break;
                    }
                    continue;
                }

                if (menuOpen)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.LeftArrow or ConsoleKey.UpArrow:
                            menuIndex = Math.Max(0, menuIndex - 1);
                            break;
                        case ConsoleKey.RightArrow or ConsoleKey.DownArrow:
                            menuIndex = Math.Min(2, menuIndex + 1);
                            break;
                        case ConsoleKey.Escape:
                            menuOpen = false;
                            break;
                        case ConsoleKey.Enter:
                            menuOpen = false;
                            if (menuIndex == 0) return new BrowseResult<T>(BrowseAction.Edit, list[state.Index].Item);
                            if (menuIndex == 1) return new BrowseResult<T>(BrowseAction.Delete, list[state.Index].Item);
                            break;
                    }
                    continue;
                }

                var onRow = state.Index < n;
                var pageSize = PageSize();

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        state.Index = Math.Max(0, state.Index - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        state.Index = Math.Min(n, state.Index + 1);
                        break;
                    case ConsoleKey.LeftArrow or ConsoleKey.PageUp:
                        if (n > 0) state.Index = Math.Max(0, Math.Min(state.Index, n - 1) - pageSize);
                        break;
                    case ConsoleKey.RightArrow or ConsoleKey.PageDown:
                        if (onRow) state.Index = Math.Min(n - 1, state.Index + pageSize);
                        break;
                    case ConsoleKey.Home:
                        state.Index = 0;
                        break;
                    case ConsoleKey.End:
                        state.Index = n;
                        break;
                    case ConsoleKey.Enter:
                        if (!onRow) return new BrowseResult<T>(BrowseAction.Add, null);
                        menuOpen = true;
                        menuIndex = 0;
                        break;
                    case ConsoleKey.E:
                        if (onRow) return new BrowseResult<T>(BrowseAction.Edit, list[state.Index].Item);
                        break;
                    case ConsoleKey.D or ConsoleKey.Delete:
                        if (onRow) return new BrowseResult<T>(BrowseAction.Delete, list[state.Index].Item);
                        break;
                    case ConsoleKey.A:
                        return new BrowseResult<T>(BrowseAction.Add, null);
                    case ConsoleKey.F:
                        searching = true;
                        break;
                    case ConsoleKey.Escape:
                        if (state.Query.Length > 0)
                        {
                            state.Query = "";
                            state.Index = 0;
                        }
                        else
                        {
                            return new BrowseResult<T>(BrowseAction.Back, null);
                        }
                        break;
                    default:
                        if (key.KeyChar == '/') searching = true;
                        break;
                }
            }
        }
        finally
        {
            AnsiConsole.Cursor.Show();
        }
    }
}