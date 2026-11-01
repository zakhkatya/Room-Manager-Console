using Microsoft.Data.Sqlite;
namespace RoomReservationManager.Models;

public class RoomModel
{
    private readonly string _connectionString = "Data Source=rooms.db";

    // Init Table
    public void CreateTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Room (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RoomName TEXT NO NULL
                Capacity INTEGER NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    // Get all rooms
    public List<Room> GetAll()
    {
        var rooms = new List<Room>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, RoomName, Capacity FROM Room";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            rooms.Add(new Room
            {
                Id = reader.GetInt32(0),
                RoomName = reader.GetString(1),
                Capacity = reader.GetInt32(1)
            });
        }

        return rooms;
    }

    // Add room
    public void Add(string roomName, int capacity)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO Room (RoomName, Capacity) VALUES (@roomName, @capacity)";

        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@capacity", capacity);
        command.ExecuteNonQuery();
    }

    // Update info about room
    public void Update(int id, string roomName, int capacity)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Room
            SET RoomName = @roomName,
                Capacity = @capacity
            WHERE Id = @id
            """;

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@capacity", capacity);
        command.ExecuteNonQuery();
    }

    // Delete room
    public void Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM Reservation WHERE RoomId = @id";
        check.Parameters.AddWithValue("@id", id);

        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            throw new InvalidOperationException("This room has reservations. Delete or move them first.");

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Room WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }
}
