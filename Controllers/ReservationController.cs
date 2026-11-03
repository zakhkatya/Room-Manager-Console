using RoomReservationManager.Models;
using RoomReservationManager.Views;

namespace RoomReservationManager.Controllers;

public class ReservationController : ISectionController
{
    private readonly ReservationModel _model;
    private readonly RoomModel _roomModel;
    private readonly IAppView _view;

    public ReservationController(ReservationModel model, RoomModel roomModel, IAppView view)
    {
        _model = model;
        _roomModel = roomModel;
        _view = view;
    }

    public void Show()
    {
        var reservations = _model.GetAll();
        if (reservations.Count == 0) { _view.ShowInfo("No reservations found."); return; }
        _view.ShowReservations(reservations);
    }

    public void Add()
    {
        if (_roomModel.GetAll().Count == 0) { _view.ShowError("No rooms exist yet. Add a room first."); return; }

        if (ChooseSlotAndRoom(existing: null) is not { } chosen)
        {
            _view.ShowInfo("Reservation cancelled.");
            return;
        }

        var details = _view.AskDetails();

        try
        {
            _model.Add(chosen.Slot.StartsAt, chosen.Slot.EndsAt, details.EventName, chosen.Slot.NumPeople,
                       details.ReservedName, details.Notes, chosen.Room.Id);
            _view.ShowSuccess("Reservation added successfully!");
        }
        catch (Exception ex) { _view.ShowError(ex.Message); }
    }

    public void Edit()
    {
        var reservations = _model.GetAll();
        if (reservations.Count == 0) { _view.ShowInfo("No reservations to edit."); return; }

        var existing = _view.PickReservation(reservations, "edit");

        if (ChooseSlotAndRoom(existing) is not { } chosen)
        {
            _view.ShowInfo("Edit cancelled.");
            return;
        }

        var details = _view.AskDetails(existing);

        try
        {
            _model.Update(existing.Id, chosen.Slot.StartsAt, chosen.Slot.EndsAt, details.EventName,
                          chosen.Slot.NumPeople, details.ReservedName, details.Notes, chosen.Room.Id);
            _view.ShowSuccess("Reservation updated successfully!");
        }
        catch (Exception ex) { _view.ShowError(ex.Message); }
    }

    public void Delete()
    {
        var reservations = _model.GetAll();
        if (reservations.Count == 0) { _view.ShowInfo("No reservations to delete."); return; }

        var reservation = _view.PickReservation(reservations, "delete");

        if (!_view.Confirm($"Delete \"{reservation.EventName}\"?"))
        {
            _view.ShowInfo("Deletion cancelled.");
            return;
        }

        try
        {
            _model.Delete(reservation.Id);
            _view.ShowSuccess("Reservation deleted successfully!");
        }
        catch (Exception ex) { _view.ShowError(ex.Message); }
    }

    private (ReservationSlot Slot, Room Room)? ChooseSlotAndRoom(Reservation? existing)
    {
        while (true)
        {
            var slot = _view.AskSlot(existing);
            var available = GetAvailableRooms(slot, ignoreReservationId: existing?.Id);

            if (available.Count > 0)
                return (slot, _view.PickAvailableRoom(available, slot, existing?.RoomId));

            var anyBigEnough = _roomModel.GetAll().Any(r => r.Capacity >= slot.NumPeople);
            _view.ShowError(anyBigEnough
                ? "All rooms that are big enough are already reserved at that time."
                : $"No room has enough capacity for {slot.NumPeople} people.");

            if (!_view.Confirm("Try a different time or number of people?"))
                return null;
        }
    }

    private List<Room> GetAvailableRooms(ReservationSlot slot, int? ignoreReservationId)
    {
        var busyRoomIds = _model.GetAll()
            .Where(r => r.Id != ignoreReservationId &&
                        slot.StartsAt < r.EndsAt &&
                        slot.EndsAt > r.StartsAt)
            .Select(r => r.RoomId)
            .ToHashSet();

        return _roomModel.GetAll()
            .Where(r => r.Capacity >= slot.NumPeople && !busyRoomIds.Contains(r.Id))
            .ToList();
    }
}