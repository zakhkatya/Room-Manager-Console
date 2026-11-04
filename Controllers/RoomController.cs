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

    public void Run()
    {
        var state = new BrowseState();

        while (true)
        {
            var result = _view.BrowseRooms(_model.GetAll(), state);

            switch (result.Action)
            {
                case BrowseAction.Back:
                    return;
                case BrowseAction.Add:    
                    Save(null, state);
                    break;
                case BrowseAction.Edit:
                    Save(result.Item!, state);
                    break;
                case BrowseAction.Delete: Delete(result.Item!, state);
                    break;
            }
        }
    }

    private void Save(Room? existing, BrowseState state)
    {
        var isNew = existing is null;

        while (true)
        {
            _view.BeginForm(isNew ? "New room" : "Edit room");

            var roomName = _view.AskRoomName(existing?.RoomName);
            var capacity = _view.AskRoomCapacity(existing?.Capacity);

            switch (_view.ReviewForm(isNew ? "Save this room?" : "Save changes?"))
            {
                case FormDecision.Restart: continue;
                case FormDecision.Cancel:  state.Ok("Cancelled - nothing was saved."); return;
            }

            try
            {
                if (existing is null) _model.Add(roomName, capacity);
                else _model.Update(existing.Id, roomName, capacity);

                state.Ok(isNew ? "Room added." : "Room updated.");
            }
            catch (Exception ex) { state.Fail(ex.Message); }

            return;
        }
    }

    private void Delete(Room room, BrowseState state)
    {
        if (!_view.ConfirmDelete("Delete room", $"\"{room.RoomName}\" (capacity {room.Capacity})"))
        {
            state.Ok("Deletion cancelled.");
            return;
        }

        try
        {
            _model.Delete(room.Id);
            state.Ok("Room deleted.");
        }
        catch (Exception ex) { state.Fail(ex.Message); }
    }
}