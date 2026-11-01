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
        var rooms = _roomModel.GetAll();
        if (rooms.Count == 0) { _view.ShowError("No rooms exist yet. Add a room first."); return; }

        var input = _view.AskReservation(rooms);

        if (HasConflict(input, ignoreId: null))
        {
            _view.ShowError("That room is already reserved during this time.");
            return;
        }

        try
        {
            _model.Add(input.StartsAt, input.EndsAt, input.EventName,
                       input.ReservedName, input.Notes, input.RoomId);
            _view.ShowSuccess("Reservation added successfully!");
        }
        catch (Exception ex) { _view.ShowError(ex.Message); }
    }

    public void Edit()
    {
        var reservations = _model.GetAll();
        if (reservations.Count == 0) { _view.ShowInfo("No reservations to edit."); return; }

        var existing = _view.PickReservation(reservations, "edit");
        var input = _view.AskReservation(_roomModel.GetAll(), existing);

        if (HasConflict(input, ignoreId: existing.Id))
        {
            _view.ShowError("That room is already reserved during this time.");
            return;
        }

        try
        {
            _model.Update(existing.Id, input.StartsAt, input.EndsAt, input.EventName,
                          input.ReservedName, input.Notes, input.RoomId);
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

    // Business rule: the same room can't be booked twice at overlapping times.
    private bool HasConflict(ReservationInput input, int? ignoreId) =>
        _model.GetAll().Any(r =>
            r.RoomId == input.RoomId &&
            r.Id != ignoreId &&
            input.StartsAt < r.EndsAt &&
            input.EndsAt > r.StartsAt);
}