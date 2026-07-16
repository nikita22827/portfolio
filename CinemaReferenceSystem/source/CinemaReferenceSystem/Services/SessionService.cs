using Npgsql;
using CinemaReferenceSystem.Models;

namespace CinemaReferenceSystem.Services;

public class SessionService
{
    private readonly DatabaseService _db;

    public SessionService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Session>> GetAllAsync()
    {
        var sessions = new List<Session>();

        using var connection = await _db.GetConnectionAsync();

        string query = @"
            SELECT id, hall_id, movie_id, start_time, price
            FROM ""session""
            ORDER BY start_time";

        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            sessions.Add(new Session
            {
                Id = reader.GetInt32(0),
                HallId = reader.GetInt32(1),
                MovieId = reader.GetInt32(2),
                StartTime = reader.GetDateTime(3),
                Price = reader.GetDecimal(4)
            });
        }

        return sessions;
    }

    public async Task<int> AddAsync(Session session)
    {
        using var connection = await _db.GetConnectionAsync();

        string query = @"
        INSERT INTO ""session"" (hall_id, movie_id, start_time, price)
        VALUES (@hall_id, @movie_id, @start_time, @price)
        RETURNING id";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("hall_id", session.HallId);
        command.Parameters.AddWithValue("movie_id", session.MovieId);
        command.Parameters.AddWithValue("start_time", session.StartTime);
        command.Parameters.AddWithValue("price", session.Price);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<List<Session>> GetByHallIdAsync(int hallId)
    {
        var sessions = new List<Session>();

        using var connection = await _db.GetConnectionAsync();

        string query = @"
            SELECT id, hall_id, movie_id, start_time, price
            FROM ""session""
            WHERE hall_id = @hall_id
            ORDER BY start_time";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("hall_id", hallId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            sessions.Add(new Session
            {
                Id = reader.GetInt32(0),
                HallId = reader.GetInt32(1),
                MovieId = reader.GetInt32(2),
                StartTime = reader.GetDateTime(3),
                Price = reader.GetDecimal(4)
            });
        }

        return sessions;
    }
    public async Task<List<SessionDetails>> GetDetailedAllAsync()
    {
        var sessions = new List<SessionDetails>();

        using var connection = await _db.GetConnectionAsync();

        string query = @"
        SELECT 
            s.id,
            c.name AS cinema_name,
            h.hall_number,
            m.title AS movie_title,
            s.start_time,
            s.price
        FROM ""session"" s
        JOIN hall h ON s.hall_id = h.id
        JOIN cinema c ON h.cinema_id = c.id
        JOIN movie m ON s.movie_id = m.id
        ORDER BY s.start_time";

        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            sessions.Add(new SessionDetails
            {
                Id = reader.GetInt32(0),
                CinemaName = reader.GetString(1),
                HallNumber = reader.GetInt32(2),
                MovieTitle = reader.GetString(3),
                StartTime = reader.GetDateTime(4),
                Price = reader.GetDecimal(5)
            });
        }

        return sessions;
    }
}