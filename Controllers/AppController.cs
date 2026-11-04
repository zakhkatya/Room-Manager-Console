using RoomReservationManager.Models;
using RoomReservationManager.Views;

namespace RoomReservationManager.Controllers;

public class AppController
{
    private readonly IAppView _view;
    private readonly RoomController _rooms;
   private readonly ReservationController _reservations;
    private readonly RoomModel _roomModel;
    private readonly ReservationModel _reservationModel;

    public AppController(
        IAppView view, 
        RoomController rooms, 
        ReservationController reservations, 
        RoomModel roomModel, 
        ReservationModel reservationModel)
    {
        _view = view;
        _rooms = rooms;
        _reservations = reservations;
        _roomModel = roomModel;
        _reservationModel = reservationModel;
    }

    public void Run()
    {
        while (true)
        {
            switch (_view.ShowMainMenu(_roomModel.GetAll, _reservationModel.GetAll))
            {
                case MainChoice.Reservations:
                    _reservations.Run(); 
                    break;
                case MainChoice.Rooms:        
                    _rooms.Run();        
                    break;
                case MainChoice.Exit:
                    _view.Clear();
                    _view.ShowInfo("Goodbye!");
                    return;
            }
        }
    }
}