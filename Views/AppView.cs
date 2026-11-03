using RoomReservationManager.Models;

namespace RoomReservationManager.Views;

public enum MainChoice { Reservations, Rooms, Exit }
public enum SectionChoice { Show, Add, Edit, Delete, Back }
public record ReservationSlot(DateTime StartsAt, DateTime EndsAt, int NumPeople);
public record ReservationDetails(string EventName, string ReservedName, string Notes);

public interface IAppView
{
    MainChoice ShowMainMenu();
    SectionChoice ShowSectionMenu(string title);

    void ShowRooms(IReadOnlyList<Room> rooms);
    void ShowReservations(IReadOnlyList<Reservation> reservations);

    string AskRoomName(string? current = null);
    int AskRoomCapacity(int? current = null);
    Room PickRoom(IReadOnlyList<Room> rooms, string action);
    Reservation PickReservation(IReadOnlyList<Reservation> reservations, string action);
    ReservationSlot AskSlot(Reservation? existing = null);
    Room PickAvailableRoom(IReadOnlyList<Room> availableRooms, ReservationSlot slot, int? currentRoomId = null);
    ReservationDetails AskDetails(Reservation? existing = null);

    bool Confirm(string question);
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowInfo(string message);
    void Pause();
    void Clear();
}