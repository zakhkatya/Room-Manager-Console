namespace RoomReservationManager.Models;
public class Reservation
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public int NumPeople { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string ReservedName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
