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

        string query = "SELECT id, title, genre, duration_minutes, director FROM movie ORDER BY id";

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
                Director = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return movies;
    }

    public async Task AddAsync(Movie movie)
    {
        using var connection = await _db.GetConnectionAsync();

        string query = @"
            INSERT INTO movie (title, genre, duration_minutes, director)
            VALUES (@title, @genre, @duration, @director)";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("title", movie.Title);
        command.Parameters.AddWithValue("genre", (object?)movie.Genre ?? DBNull.Value);
        command.Parameters.AddWithValue("duration", movie.DurationMinutes);
        command.Parameters.AddWithValue("director", (object?)movie.Director ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }
}