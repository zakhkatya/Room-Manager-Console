using RoomReservationManager.Models;
using RoomReservationManager.Views;

namespace RoomReservationManager.Controllers;

public class RoomController : ISectionController
{
    private readonly RoomModel _model;
    private readonly IAppView _view;

    public RoomController(RoomModel model, IAppView view)
    {
        _model = model;
        _view = view;
    }

    public void Show()
    {
        var rooms = _model.GetAll();
        if (rooms.Count == 0) { _view.ShowInfo("No rooms found."); return; }
        _view.ShowRooms(rooms);
    }

    public void Add()
    {
        var isBig = _view.AskRoomIsBig();

        try
        {
            _model.Add(isBig);
            _view.ShowSuccess("Room added successfully!");
        }
        catch (Exception ex) { _view.ShowError(ex.Message); }
    }

    public void Edit()
    {
        var rooms = _model.GetAll();
        if (rooms.Count == 0) { _view.ShowInfo("No rooms to edit."); return; }

        var room = _view.PickRoom(rooms, "edit");
        var isBig = _view.AskRoomIsBig(room.IsBig);

        try
        {
            _model.Update(room.Id, isBig);
            _view.ShowSuccess("Room updated successfully!");
        }
        catch (Exception ex) { _view.ShowError(ex.Message); }
    }

    public void Delete()
    {
        var rooms = _model.GetAll();
        if (rooms.Count == 0) { _view.ShowInfo("No rooms to delete."); return; }

        var room = _view.PickRoom(rooms, "delete");

        if (!_view.Confirm($"Delete room {room.Id}?"))
        {
            _view.ShowInfo("Deletion cancelled.");
            return;
        }

        try
        {
            _model.Delete(room.Id);   // throws if the room has reservations (see earlier fix)
            _view.ShowSuccess("Room deleted successfully!");
        }
        catch (Exception ex) { _view.ShowError(ex.Message); }
    }
}