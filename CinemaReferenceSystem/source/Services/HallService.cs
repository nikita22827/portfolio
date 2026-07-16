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

    public async Task<List<Hall>> GetByCinemaIdAsync(int cinemaId)
    {
        var halls = new List<Hall>();

        using var connection = await _db.GetConnectionAsync();

        string query = @"
        SELECT id, cinema_id, hall_number, rows_count, max_seats_per_row
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
                RowsCount = reader.GetInt32(3),
                MaxSeatsPerRow = reader.GetInt32(4)
            });
        }

        return halls;
    }

    public async Task<int> AddAsync(Hall hall)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        INSERT INTO hall (cinema_id, hall_number, rows_count, max_seats_per_row)
        VALUES (@cinema_id, @hall_number, @rows_count, @max_seats_per_row)
        RETURNING id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("cinema_id", hall.CinemaId);
        command.Parameters.AddWithValue("hall_number", hall.HallNumber);
        command.Parameters.AddWithValue("rows_count", hall.RowsCount);
        command.Parameters.AddWithValue("max_seats_per_row", hall.MaxSeatsPerRow);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
    public async Task UpdateAsync(Hall hall)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        UPDATE hall SET cinema_id = @cinema_id, hall_number = @hall_number,
                        rows_count = @rows_count, max_seats_per_row = @max_seats_per_row
        WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", hall.Id);
        command.Parameters.AddWithValue("cinema_id", hall.CinemaId);
        command.Parameters.AddWithValue("hall_number", hall.HallNumber);
        command.Parameters.AddWithValue("rows_count", hall.RowsCount);
        command.Parameters.AddWithValue("max_seats_per_row", hall.MaxSeatsPerRow);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = "DELETE FROM hall WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Hall?> GetByIdAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = "SELECT id, cinema_id, hall_number, rows_count, max_seats_per_row FROM hall WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Hall
            {
                Id = reader.GetInt32(0),
                CinemaId = reader.GetInt32(1),
                HallNumber = reader.GetInt32(2),
                RowsCount = reader.GetInt32(3),
                MaxSeatsPerRow = reader.GetInt32(4)
            };
        }
        return null;
    }
    public async Task<List<(int Row, int Seat)>> GetSeatsAsync(int hallId)
    {
        var seats = new List<(int, int)>();
        using var connection = await _db.GetConnectionAsync();
        string query = "SELECT row_num, seat_num FROM seat WHERE hall_id = @hallId ORDER BY row_num, seat_num";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("hallId", hallId);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            seats.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }
        return seats;
    }

    public async Task UpdateSeatsAsync(int hallId, List<(int Row, int Seat)> enabledSeats)
    {
        using var connection = await _db.GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();
        try
        {
            string deleteQuery = "DELETE FROM seat WHERE hall_id = @hallId";
            using (var cmd = new NpgsqlCommand(deleteQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("hallId", hallId);
                await cmd.ExecuteNonQueryAsync();
            }

            string insertQuery = "INSERT INTO seat (hall_id, row_num, seat_num) VALUES (@hallId, @row, @seat)";
            foreach (var (row, seat) in enabledSeats)
            {
                using var cmd = new NpgsqlCommand(insertQuery, connection, transaction);
                cmd.Parameters.AddWithValue("hallId", hallId);
                cmd.Parameters.AddWithValue("row", row);
                cmd.Parameters.AddWithValue("seat", seat);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}