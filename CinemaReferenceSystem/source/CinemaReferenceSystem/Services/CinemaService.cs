using Npgsql;
using CinemaReferenceSystem.Models;

namespace CinemaReferenceSystem.Services;

public class CinemaService
{
    private readonly DatabaseService _db;

    public CinemaService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Cinema>> GetAllAsync()
    {
        var cinemas = new List<Cinema>();

        using var connection = await _db.GetConnectionAsync();

        string query = "SELECT id, name, address, phone FROM cinema ORDER BY id";

        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            cinemas.Add(new Cinema
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Address = reader.GetString(2),
                Phone = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return cinemas;
    }

    public async Task AddAsync(Cinema cinema)
    {
        using var connection = await _db.GetConnectionAsync();

        string query = @"
            INSERT INTO cinema (name, address, phone)
            VALUES (@name, @address, @phone)";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("name", cinema.Name);
        command.Parameters.AddWithValue("address", cinema.Address);
        command.Parameters.AddWithValue("phone", (object?)cinema.Phone ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }
}