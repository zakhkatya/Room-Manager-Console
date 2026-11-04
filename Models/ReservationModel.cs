using Microsoft.Data.Sqlite;
namespace RoomReservationManager.Models;
public class ReservationModel
{
    private readonly string _connectionString = "Data Source=rooms.db";

    //Init table
    public void CreateTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Reservation (
                Id INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE,
                StartsAt TEXT NOT NULL,
                EndsAt TEXT NOT NULL,
                EventName TEXT NOT NULL,
                NumPeople INTEGER NOT NULL,
                ReservedName TEXT NOT NULL,
                Notes TEXT,
                RoomId INTEGER NOT NULL,
                FOREIGN KEY (RoomId) REFERENCES Room(Id)
            );
            """;

        command.ExecuteNonQuery();
    }

    // Get all reservations
    public List<Reservation> GetAll()
    {
        var reservations = new List<Reservation>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, StartsAt, EndsAt, EventName, NumPeople, ReservedName, Notes, RoomId
            FROM Reservation
            ORDER BY StartsAt
            """;

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            reservations.Add(new Reservation
            {
                Id = reader.GetInt32(0),
                StartsAt = DateTime.Parse(reader.GetString(1)),
                EndsAt = DateTime.Parse(reader.GetString(2)),
                EventName = reader.GetString(3),
                NumPeople = reader.GetInt32(4),
                ReservedName = reader.GetString(5),
                Notes = reader.IsDBNull(6)
                    ? string.Empty
                    : reader.GetString(6),
                RoomId = reader.GetInt32(7)
            });
        }

        return reservations;
    }

    //Add reservation
    public void Add(
        DateTime startsAt,
        DateTime endsAt,
        string eventName,
        int numPeople,
        string reservedName,
        string notes,
        int roomId)
    {
        if (endsAt <= startsAt) throw new ArgumentException("End time must be after start time.");

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Reservation
                (StartsAt, EndsAt, EventName, NumPeople, ReservedName, Notes, RoomId)
            VALUES
                (@startsAt, @endsAt, @eventName, @numPeople, @reservedName, @notes, @roomId)
            """;

        command.Parameters.AddWithValue("@startsAt", startsAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@endsAt", endsAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@eventName", eventName);
        command.Parameters.AddWithValue("@numPeople", numPeople);
        command.Parameters.AddWithValue("@reservedName", reservedName);
        command.Parameters.AddWithValue("@notes", notes);
        command.Parameters.AddWithValue("@roomId", roomId);

        command.ExecuteNonQuery();
    }

    // Update reservation
    public void Update(
        int id,
        DateTime startsAt,
        DateTime endsAt,
        string eventName,
        int numPeople,
        string reservedName,
        string notes,
        int roomId)
    {
        if (endsAt <= startsAt)
            throw new ArgumentException(
                "End time must be after start time.");

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Reservation
            SET StartsAt = @startsAt,
                EndsAt = @endsAt,
                EventName = @eventName,
                NumPeople = @numPeople,
                ReservedName = @reservedName,
                Notes = @notes,
                RoomId = @roomId
            WHERE Id = @id
            """;

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@startsAt", startsAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@endsAt", endsAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@eventName", eventName);
        command.Parameters.AddWithValue("@numPeople", numPeople);
        command.Parameters.AddWithValue("@reservedName", reservedName);
        command.Parameters.AddWithValue("@notes", notes);
        command.Parameters.AddWithValue("@roomId", roomId);

        command.ExecuteNonQuery();
    }

    // Delete reservation
    public void Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            "DELETE FROM Reservation WHERE Id = @id";

        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }
}
