using RoomReservationManager.Models;

namespace RoomReservationManager.Views;

public enum MainChoice { Reservations, Rooms, Exit }
public enum BrowseAction { Add, Edit, Delete, Back }
public enum FormDecision { Save, Restart, Cancel }

public record BrowseResult<T>(BrowseAction Action, T? Item) where T : class;

public class BrowseState
{
    public string Query { get; set; } = "";
    public int Index { get; set; }
    public string? Message { get; set; }
    public bool MessageIsError { get; set; }

    public void Ok(string message) { Message = message; MessageIsError = false; }
    public void Fail(string message) { Message = message; MessageIsError = true; }
}

public record ReservationSlot(DateTime StartsAt, DateTime EndsAt, int NumPeople);
public record ReservationDetails(string EventName, string ReservedName, string Notes);

public interface IAppView
{
    MainChoice ShowMainMenu(Func<IReadOnlyList<Room>> getRooms, Func<IReadOnlyList<Reservation>> getReservations);

    BrowseResult<Room> BrowseRooms(IReadOnlyList<Room> rooms, BrowseState state);
    BrowseResult<Reservation> BrowseReservations(
        IReadOnlyList<Reservation> reservations,
        IReadOnlyDictionary<int, string> roomNames,
        BrowseState state);

    void BeginForm(string title);
    string AskRoomName(string? current = null);
    int AskRoomCapacity(int? current = null);
    ReservationSlot AskSlot(Reservation? existing = null);
    Room PickAvailableRoom(IReadOnlyList<Room> availableRooms, ReservationSlot slot, int? currentRoomId = null);
    ReservationDetails AskDetails(Reservation? existing = null);
    FormDecision ReviewForm(string question);

    bool ConfirmDelete(string title, string description);
    bool Confirm(string question);
    void ShowError(string message);
    void ShowInfo(string message);
    void Clear();
}