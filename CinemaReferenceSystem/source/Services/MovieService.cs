using Npgsql;
using CinemaReferenceSystem.Models;

namespace CinemaReferenceSystem.Services;

public class MovieService
{
    private readonly DatabaseService _db;

    public MovieService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Movie>> GetAllAsync()
    {
        var movies = new List<Movie>();
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        SELECT m.id, m.title, m.genre, m.duration_minutes, m.director,
               m.poster_url, m.trailer_url, m.country, m.description,
               m.premiere_date, m.release_year,
               COALESCE(ROUND(AVG(mr.rating), 2), 0) AS avg_rating
        FROM movie m
        LEFT JOIN movie_review mr ON m.id = mr.movie_id
        GROUP BY m.id
        ORDER BY m.id";
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            movies.Add(new Movie
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Genre = reader.IsDBNull(2) ? null : reader.GetString(2),
                DurationMinutes = reader.GetInt32(3),
                Director = reader.IsDBNull(4) ? null : reader.GetString(4),
                PosterUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                TrailerUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                Country = reader.IsDBNull(7) ? null : reader.GetString(7),
                Description = reader.IsDBNull(8) ? null : reader.GetString(8),
                PremiereDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                ReleaseYear = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                AverageRating = reader.GetDouble(11)
            });
        }
        return movies;
    }

    public async Task AddAsync(Movie movie)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        INSERT INTO movie (title, genre, duration_minutes, director,
                           poster_url, trailer_url, country, description,
                           premiere_date, release_year)
        VALUES (@title, @genre, @duration, @director,
                @poster_url, @trailer_url, @country, @description,
                @premiere_date, @release_year)";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("title", movie.Title);
        command.Parameters.AddWithValue("genre", (object?)movie.Genre ?? DBNull.Value);
        command.Parameters.AddWithValue("duration", movie.DurationMinutes);
        command.Parameters.AddWithValue("director", (object?)movie.Director ?? DBNull.Value);
        command.Parameters.AddWithValue("poster_url", (object?)movie.PosterUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("trailer_url", (object?)movie.TrailerUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("country", (object?)movie.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("description", (object?)movie.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("premiere_date", (object?)movie.PremiereDate ?? DBNull.Value);
        command.Parameters.AddWithValue("release_year", (object?)movie.ReleaseYear ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }
    public async Task UpdateAsync(Movie movie)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        UPDATE movie
        SET title = @title, genre = @genre, duration_minutes = @duration,
            director = @director, poster_url = @poster_url,
            trailer_url = @trailer_url, country = @country,
            description = @description, premiere_date = @premiere_date,
            release_year = @release_year
        WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", movie.Id);
        command.Parameters.AddWithValue("title", movie.Title);
        command.Parameters.AddWithValue("genre", (object?)movie.Genre ?? DBNull.Value);
        command.Parameters.AddWithValue("duration", movie.DurationMinutes);
        command.Parameters.AddWithValue("director", (object?)movie.Director ?? DBNull.Value);
        command.Parameters.AddWithValue("poster_url", (object?)movie.PosterUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("trailer_url", (object?)movie.TrailerUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("country", (object?)movie.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("description", (object?)movie.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("premiere_date", (object?)movie.PremiereDate ?? DBNull.Value);
        command.Parameters.AddWithValue("release_year", (object?)movie.ReleaseYear ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = "DELETE FROM movie WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Movie?> GetFullByIdAsync(int id)
    {
        using var connection = await _db.GetConnectionAsync();
        string query = @"
        SELECT m.id, m.title, m.genre, m.duration_minutes, m.director,
               m.poster_url, m.trailer_url, m.country, m.description,
               m.premiere_date, m.release_year,
               COALESCE(ROUND(AVG(mr.rating), 2), 0) AS avg_rating
        FROM movie m
        LEFT JOIN movie_review mr ON m.id = mr.movie_id
        WHERE m.id = @id
        GROUP BY m.id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Movie
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Genre = reader.IsDBNull(2) ? null : reader.GetString(2),
                DurationMinutes = reader.GetInt32(3),
                Director = reader.IsDBNull(4) ? null : reader.GetString(4),
                PosterUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                TrailerUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                Country = reader.IsDBNull(7) ? null : reader.GetString(7),
                Description = reader.IsDBNull(8) ? null : reader.GetString(8),
                PremiereDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                ReleaseYear = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                AverageRating = reader.GetDouble(11)
            };
        }
        return null;
    }
}