using Npgsql;
using CinemaReferenceSystem.Models;

namespace CinemaReferenceSystem.Services;

public class AuthService
{
    private readonly DatabaseService _db;

    public AuthService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<bool> RegisterAsync(string username, string password, string role = "user")
    {
        using var connection = await _db.GetConnectionAsync();

        const string query = @"
            INSERT INTO app_user (username, password_hash, role)
            VALUES (@username, @password_hash, @role)";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("password_hash", password);
        command.Parameters.AddWithValue("role", role);

        try
        {
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        using var connection = await _db.GetConnectionAsync();

        const string query = @"
        SELECT id, username, role
        FROM app_user
        WHERE username = @username
          AND password_hash = crypt(@password, password_hash)
        LIMIT 1";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("password", password);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            Role = reader.GetString(2)
        };
    }
}