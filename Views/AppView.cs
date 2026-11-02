using RoomReservationManager.Models;

namespace RoomReservationManager.Views;

public enum MainChoice { Reservations, Rooms, Exit }
public enum SectionChoice { Show, Add, Edit, Delete, Back }

public record ReservationInput(
    DateTime StartsAt, 
    DateTime EndsAt,
    string EventName, 
    string ReservedName,
    string Notes, 
    int NumPeople,
    int RoomId);

public interface IAppView
{
    MainChoice ShowMainMenu();
    SectionChoice ShowSectionMenu(string title);

    void ShowRooms(IReadOnlyList<Room> rooms);
    void ShowReservations(IReadOnlyList<Reservation> reservations);

    bool AskRoomIsBig(bool? current = null);
    Room PickRoom(IReadOnlyList<Room> rooms, string action);
    Reservation PickReservation(IReadOnlyList<Reservation> reservations, string action);
    ReservationInput AskReservation(IReadOnlyList<Room> rooms, Reservation? existing = null);

    bool Confirm(string question);
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowInfo(string message);
    void Pause();
    void Clear();
}