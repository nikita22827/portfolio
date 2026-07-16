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
    public async Task<Session?> GetByIdAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"SELECT id, hall_id, movie_id, start_time, price FROM ""session"" WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Session
            {
                Id = reader.GetInt32(0),
                HallId = reader.GetInt32(1),
                MovieId = reader.GetInt32(2),
                StartTime = reader.GetDateTime(3),
                Price = reader.GetDecimal(4)
            };
        }
        return null;
    }

    public async Task UpdateAsync(Session session)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"UPDATE ""session"" SET hall_id = @hall_id, movie_id = @movie_id, start_time = @start_time, price = @price WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("hall_id", session.HallId);
        command.Parameters.AddWithValue("movie_id", session.MovieId);
        command.Parameters.AddWithValue("start_time", session.StartTime);
        command.Parameters.AddWithValue("price", session.Price);
        command.Parameters.AddWithValue("id", session.Id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"DELETE FROM ""session"" WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }
    public async Task<int> GetHallIdBySessionIdAsync(int sessionId)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = "SELECT hall_id FROM \"session\" WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", sessionId);
        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }
    public async Task<List<SessionDetails>> GetDetailedByCinemaIdAsync(int cinemaId)
    {
        var result = new List<SessionDetails>();
        using var conn = await _db.GetConnectionAsync();
        string sql = @"
        SELECT s.id, c.name, h.hall_number, m.title, s.start_time, s.price
        FROM ""session"" s
        JOIN hall h ON s.hall_id = h.id
        JOIN cinema c ON h.cinema_id = c.id
        JOIN movie m ON s.movie_id = m.id
        WHERE c.id = @cinemaId
        ORDER BY s.start_time";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cinemaId", cinemaId);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SessionDetails
            {
                Id = reader.GetInt32(0),
                CinemaName = reader.GetString(1),
                HallNumber = reader.GetInt32(2),
                MovieTitle = reader.GetString(3),
                StartTime = reader.GetDateTime(4),
                Price = reader.GetDecimal(5)
            });
        }
        return result;
    }
    public async Task<List<MovieSessionInfo>> GetSessionsForMovieAsync(int movieId, string? city = null)
    {
        var result = new List<MovieSessionInfo>();
        using var connection = await _db.GetConnectionAsync();
        string sql = @"
        SELECT s.id,
               c.name AS cinema_name,
               c.address,
               c.id AS cinema_id,
               COALESCE(ROUND(AVG(r.rating), 2), 0) AS cinema_rating,
               h.hall_number,
               s.start_time,
               s.price
        FROM ""session"" s
        JOIN hall h ON s.hall_id = h.id
        JOIN cinema c ON h.cinema_id = c.id
        LEFT JOIN review r ON r.cinema_id = c.id
        WHERE s.movie_id = @movieId
        {0}
        GROUP BY s.id, c.id, h.hall_number
        ORDER BY c.name, s.start_time";

        string cityCondition = "";
        if (!string.IsNullOrEmpty(city))
        {
            cityCondition = " AND c.city = @city";
        }

        sql = string.Format(sql, cityCondition);

        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("movieId", movieId);
        if (!string.IsNullOrEmpty(city))
            cmd.Parameters.AddWithValue("city", city);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MovieSessionInfo
            {
                SessionId = reader.GetInt32(0),
                CinemaName = reader.GetString(1),
                CinemaAddress = reader.GetString(2),
                CinemaId = reader.GetInt32(3),
                CinemaRating = reader.GetDouble(4),
                HallNumber = reader.GetInt32(5),
                StartTime = reader.GetDateTime(6),
                Price = reader.GetDecimal(7)
            });
        }
        return result;
    }
    public async Task<bool> IsTimeSlotAvailable(int hallId, DateTime startTime, int movieId, int? excludeSessionId = null)
    {
        using var connection = await _db.GetConnectionAsync();

        string durQuery = "SELECT duration_minutes FROM movie WHERE id = @movieId";
        using var durCmd = new NpgsqlCommand(durQuery, connection);
        durCmd.Parameters.AddWithValue("movieId", movieId);
        var durObj = await durCmd.ExecuteScalarAsync();
        if (durObj == null) return false;
        int duration = Convert.ToInt32(durObj);

        DateTime endTime = startTime.AddMinutes(duration + 30);

        string query = @"
        SELECT 1 FROM ""session"" s
        JOIN movie m ON s.movie_id = m.id
        WHERE s.hall_id = @hallId
          AND s.start_time < @endTime
          AND (s.start_time + (m.duration_minutes + 30) * INTERVAL '1 minute') > @startTime
    ";

        if (excludeSessionId.HasValue)
        {
            query += " AND s.id != @excludeId";
        }

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("hallId", hallId);
        cmd.Parameters.AddWithValue("startTime", startTime);
        cmd.Parameters.AddWithValue("endTime", endTime);
        if (excludeSessionId.HasValue)
            cmd.Parameters.AddWithValue("excludeId", excludeSessionId.Value);

        var result = await cmd.ExecuteScalarAsync();
        return result == null; 
    }
}