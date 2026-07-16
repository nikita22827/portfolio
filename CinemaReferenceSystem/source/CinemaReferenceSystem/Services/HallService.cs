using Npgsql;
using CinemaReferenceSystem.Models;

namespace CinemaReferenceSystem.Services;

public class HallService
{
    private readonly DatabaseService _db;

    public HallService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Hall>> GetAllAsync()
    {
        var halls = new List<Hall>();

        using var connection = await _db.GetConnectionAsync();

        string query = @"
            SELECT id, cinema_id, hall_number, rows_count, seats_per_row, seats_count
            FROM public.hall
            ORDER BY id";

        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            halls.Add(new Hall
            {
                Id = reader.GetInt32(0),
                CinemaId = reader.GetInt32(1),
                HallNumber = reader.GetInt32(2),
                RowsCount = reader.GetInt32(3),
                SeatsPerRow = reader.GetInt32(4),
                SeatsCount = reader.GetInt32(5)
            });
        }

        return halls;
    }

    public async Task<List<Hall>> GetByCinemaIdAsync(int cinemaId)
    {
        var halls = new List<Hall>();

        using var connection = await _db.GetConnectionAsync();

        string query = @"
            SELECT id, cinema_id, hall_number, seats_count
            FROM hall
            WHERE cinema_id = @cinema_id
            ORDER BY hall_number";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("cinema_id", cinemaId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            halls.Add(new Hall
            {
                Id = reader.GetInt32(0),
                CinemaId = reader.GetInt32(1),
                HallNumber = reader.GetInt32(2),
                SeatsCount = reader.GetInt32(3)
            });
        }

        return halls;
    }

    public async Task AddAsync(Hall hall)
    {
        using var connection = await _db.GetConnectionAsync();

        hall.SeatsCount = hall.RowsCount * hall.SeatsPerRow;

        string query = @"
        INSERT INTO hall (cinema_id, hall_number, rows_count, seats_per_row, seats_count)
        VALUES (@cinema_id, @hall_number, @rows_count, @seats_per_row, @seats_count)";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("cinema_id", hall.CinemaId);
        command.Parameters.AddWithValue("hall_number", hall.HallNumber);
        command.Parameters.AddWithValue("rows_count", hall.RowsCount);
        command.Parameters.AddWithValue("seats_per_row", hall.SeatsPerRow);
        command.Parameters.AddWithValue("seats_count", hall.SeatsCount);

        await command.ExecuteNonQueryAsync();
    }
    public async Task<int?> GetHallIdAsync(int cinemaId, int hallNumber)
    {
        using var connection = await _db.GetConnectionAsync();

        string query = @"
        SELECT id 
        FROM hall
        WHERE cinema_id = @cinema_id AND hall_number = @hall_number";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("cinema_id", cinemaId);
        command.Parameters.AddWithValue("hall_number", hallNumber);

        var result = await command.ExecuteScalarAsync();

        if (result == null)
            return null;

        return (int)result;
    }
    public async Task<int?> GetSeatsCountByIdAsync(int hallId)
    {
        using var connection = await _db.GetConnectionAsync();

        string query = @"SELECT seats_count FROM hall WHERE id = @hall_id";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("hall_id", hallId);

        var result = await command.ExecuteScalarAsync();

        if (result == null)
            return null;

        return Convert.ToInt32(result);
    }
    public async Task<(int rows, int seatsPerRow)?> GetHallLayoutAsync(int hallId)
    {
        using var connection = await _db.GetConnectionAsync();

        string query = @"
        SELECT rows_count, seats_per_row
        FROM hall
        WHERE id = @hall_id";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("hall_id", hallId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return (reader.GetInt32(0), reader.GetInt32(1));
        }

        return null;
    }
}