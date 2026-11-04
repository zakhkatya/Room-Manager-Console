using RoomReservationManager.Models;
using RoomReservationManager.Views;

namespace RoomReservationManager.Controllers;

public class ReservationController
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

    public void Run()
    {
        var state = new BrowseState();

        while (true)
        {
            var roomNames = _roomModel.GetAll().ToDictionary(r => r.Id, r => r.RoomName);
            var result = _view.BrowseReservations(_model.GetAll(), roomNames, state);

            switch (result.Action)
            {
                case BrowseAction.Back:    
                    return;
                case BrowseAction.Add:    
                    Add(state);                  
                    break;
                case BrowseAction.Edit:   
                    Save(result.Item!, state);   
                    break;
                case BrowseAction.Delete: Delete(result.Item!, state); 
                    break;
            }
        }
    }

    private void Add(BrowseState state)
    {
        if (_roomModel.GetAll().Count == 0)
        {
            state.Fail("No rooms exist yet. Add a room first.");
            return;
        }

        Save(null, state);
    }

    private void Save(Reservation? existing, BrowseState state)
    {
        var isNew = existing is null;

        while (true)
        {
            _view.BeginForm(isNew ? "New reservation" : "Edit reservation");

            if (ChooseSlotAndRoom(existing) is not { } chosen)
            {
                state.Ok("Cancelled - nothing was saved.");
                return;
            }

            var details = _view.AskDetails(existing);

            switch (_view.ReviewForm(isNew ? "Save this reservation?" : "Save changes?"))
            {
                case FormDecision.Restart: 
                    continue;
                case FormDecision.Cancel:  
                    state.Ok("Cancelled - nothing was saved."); 
                    return;
            }

            try
            {
                if (existing is null)
                    _model.Add(chosen.Slot.StartsAt, chosen.Slot.EndsAt, details.EventName, chosen.Slot.NumPeople, details.ReservedName, details.Notes, chosen.Room.Id);
                else
                    _model.Update(existing.Id, chosen.Slot.StartsAt, chosen.Slot.EndsAt, details.EventName, chosen.Slot.NumPeople, details.ReservedName, details.Notes, chosen.Room.Id);

                state.Ok(isNew ? "Reservation added." : "Reservation updated.");
            }
            catch (Exception ex) { state.Fail(ex.Message); }

            return;
        }
    }

    private void Delete(Reservation reservation, BrowseState state)
    {
        var roomName = _roomModel.GetAll().FirstOrDefault(r => r.Id == reservation.RoomId)?.RoomName ?? $"#{reservation.RoomId}";

        var description = $"\"{reservation.EventName}\" by {reservation.ReservedName}, " +$"{reservation.StartsAt:dd.MM.yyyy HH:mm}-{reservation.EndsAt:HH:mm}, {roomName}";

        if (!_view.ConfirmDelete("Delete reservation", description))
        {
            state.Ok("Deletion cancelled.");
            return;
        }

        try
        {
            _model.Delete(reservation.Id);
            state.Ok("Reservation deleted.");
        }
        catch (Exception ex) { state.Fail(ex.Message); }
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