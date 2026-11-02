using RoomReservationManager.Controllers;
using RoomReservationManager.Models;
using RoomReservationManager.Views;

namespace RoomReservationManager;

internal class Program
{
    static void Main(string[] args)
    {
        var roomModel = new RoomModel();
        roomModel.CreateTable();

        var reservationModel = new ReservationModel();
        reservationModel.CreateTable();

        IAppView view = new ConsoleView();

        var app = new AppController(
            view,
            new RoomController(roomModel, view),
            new ReservationController(reservationModel, roomModel, view));

        app.Run();
    }
}