using Npgsql;
using CinemaReferenceSystem.Models;

namespace CinemaReferenceSystem.Services;

public class TicketService
{
    private readonly DatabaseService _db;

    public TicketService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Ticket>> GetBySessionIdAsync(int sessionId)
    {
        var tickets = new List<Ticket>();

        using var connection = await _db.GetConnectionAsync();

        string query = @"
            SELECT id, session_id, row_num, seat_num, is_sold
            FROM ticket
            WHERE session_id = @session_id
            ORDER BY row_num, seat_num";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("session_id", sessionId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tickets.Add(new Ticket
            {
                Id = reader.GetInt32(0),
                SessionId = reader.GetInt32(1),
                RowNum = reader.GetInt32(2),
                SeatNum = reader.GetInt32(3),
                IsSold = reader.GetBoolean(4)
            });
        }

        return tickets;
    }

    public async Task<bool> SellTicketAsync(int sessionId, int row, int seat)
    {
        try
        {
            using var connection = await _db.GetConnectionAsync();

            string checkQuery = @"
                SELECT is_sold
                FROM ticket
                WHERE session_id = @session_id AND row_num = @row AND seat_num = @seat";

            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("session_id", sessionId);
            checkCmd.Parameters.AddWithValue("row", row);
            checkCmd.Parameters.AddWithValue("seat", seat);

            var result = await checkCmd.ExecuteScalarAsync();

            if (result != null)
            {
                bool isSold = (bool)result;

                if (isSold)
                {
                    Console.WriteLine("Место уже занято!");
                    return false;
                }

                string updateQuery = @"
                    UPDATE ticket
                    SET is_sold = true
                    WHERE session_id = @session_id AND row_num = @row AND seat_num = @seat";

                using var updateCmd = new NpgsqlCommand(updateQuery, connection);
                updateCmd.Parameters.AddWithValue("session_id", sessionId);
                updateCmd.Parameters.AddWithValue("row", row);
                updateCmd.Parameters.AddWithValue("seat", seat);

                await updateCmd.ExecuteNonQueryAsync();
                return true;
            }
            else
            {
                string insertQuery = @"
                    INSERT INTO ticket (session_id, row_num, seat_num, is_sold)
                    VALUES (@session_id, @row, @seat, true)";

                using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("session_id", sessionId);
                insertCmd.Parameters.AddWithValue("row", row);
                insertCmd.Parameters.AddWithValue("seat", seat);

                await insertCmd.ExecuteNonQueryAsync();
                return true;
            }
        }
        catch (PostgresException ex)
        {
            Console.WriteLine($"Ошибка БД: {ex.Message}");
            return false;
        }
    }
    public async Task GenerateTicketsAsync(int sessionId, int rows, int seatsPerRow)
    {
        using var connection = await _db.GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            for (int row = 1; row <= rows; row++)
            {
                for (int seat = 1; seat <= seatsPerRow; seat++)
                {
                    string query = @"
                    INSERT INTO ticket (session_id, row_num, seat_num, is_sold)
                    VALUES (@session_id, @row, @seat, false)";

                    using var command = new NpgsqlCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("session_id", sessionId);
                    command.Parameters.AddWithValue("row", row);
                    command.Parameters.AddWithValue("seat", seat);

                    await command.ExecuteNonQueryAsync();
                }
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