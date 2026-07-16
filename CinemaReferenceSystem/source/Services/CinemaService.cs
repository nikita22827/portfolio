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

    public async Task DeleteAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = "DELETE FROM cinema WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Cinema?> GetFullByIdAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        SELECT c.id, c.name, c.address, c.phone, c.city, c.description,
               COALESCE(ROUND(AVG(r.rating), 2), 0) AS avg_rating
        FROM cinema c
        LEFT JOIN review r ON r.cinema_id = c.id
        WHERE c.id = @id
        GROUP BY c.id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Cinema
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Address = reader.GetString(2),
                Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                City = reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                AverageRating = reader.GetDouble(6)
            };
        }
        return null;
    }
    public async Task<List<string>> GetCitiesAsync()
    {
        var cities = new List<string>();
        using var connection = await _db.GetConnectionAsync();
        string query = "SELECT DISTINCT city FROM cinema ORDER BY city";
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            cities.Add(reader.GetString(0));
        return cities;
    }

    public async Task<List<Cinema>> GetAllByCityAsync(string city)
    {
        var cinemas = new List<Cinema>();
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        SELECT c.id, c.name, c.address, c.phone, c.city, c.description,
               COALESCE(avg_r.avg, 0) AS avg_rating
        FROM cinema c
        LEFT JOIN LATERAL (
            SELECT ROUND(AVG(rating), 2) AS avg FROM review WHERE cinema_id = c.id
        ) avg_r ON true
        WHERE c.city = @city
        ORDER BY c.id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("city", city);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            cinemas.Add(new Cinema
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Address = reader.GetString(2),
                Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                City = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                AverageRating = reader.GetDouble(6)
            });
        }
        return cinemas;
    }

    public async Task AddAsync(Cinema cinema)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        INSERT INTO cinema (name, address, phone, city, description)
        VALUES (@name, @address, @phone, @city, @description)";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("name", cinema.Name);
        command.Parameters.AddWithValue("address", cinema.Address);
        command.Parameters.AddWithValue("phone", (object?)cinema.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("city", cinema.City);
        command.Parameters.AddWithValue("description", (object?)cinema.Description ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }
    public async Task UpdateAsync(Cinema cinema)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        UPDATE cinema
        SET name = @name, address = @address, phone = @phone,
            city = @city, description = @description
        WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", cinema.Id);
        command.Parameters.AddWithValue("name", cinema.Name);
        command.Parameters.AddWithValue("address", cinema.Address);
        command.Parameters.AddWithValue("phone", (object?)cinema.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("city", cinema.City);
        command.Parameters.AddWithValue("description", (object?)cinema.Description ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }
}