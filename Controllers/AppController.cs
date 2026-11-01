using RoomReservationManager.Views;

namespace RoomReservationManager.Controllers;

public class AppController
{
    private readonly IAppView _view;
    private readonly ISectionController _rooms;
    private readonly ISectionController _reservations;

    public AppController(IAppView view, ISectionController rooms, ISectionController reservations)
    {
        _view = view;
        _rooms = rooms;
        _reservations = reservations;
    }

    public void Run()
    {
        while (true)
        {
            _view.Clear();

            switch (_view.ShowMainMenu())
            {
                case MainChoice.Reservations: RunSection("Reservations", _reservations); break;
                case MainChoice.Rooms:        RunSection("Rooms", _rooms);               break;
                case MainChoice.Exit:
                    _view.ShowInfo("Goodbye!");
                    return;
            }
        }
    }

    private void RunSection(string title, ISectionController section)
    {
        while (true)
        {
            _view.Clear();
            var choice = _view.ShowSectionMenu(title);
            if (choice == SectionChoice.Back) return;

            _view.Clear();
            switch (choice)
            {
                case SectionChoice.Show:   section.Show();   break;
                case SectionChoice.Add:    section.Add();    break;
                case SectionChoice.Edit:   section.Edit();   break;
                case SectionChoice.Delete: section.Delete(); break;
            }
            _view.Pause();
        }
    }
}