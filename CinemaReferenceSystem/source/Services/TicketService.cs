using CinemaReferenceSystem.Models;
using Npgsql;

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

    public async Task<bool> SellTicketAsync(int sessionId, int row, int seat, int userId)
    {
        try
        {
            using var connection = await _db.GetConnectionAsync();

            string sql = @"
            INSERT INTO ticket (session_id, row_num, seat_num, is_sold, user_id)
            VALUES (@session_id, @row, @seat, true, @userId)
            ON CONFLICT (session_id, row_num, seat_num)
            DO UPDATE SET is_sold = true, user_id = EXCLUDED.user_id
            WHERE ticket.is_sold = false";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("session_id", sessionId);
            cmd.Parameters.AddWithValue("row", row);
            cmd.Parameters.AddWithValue("seat", seat);
            cmd.Parameters.AddWithValue("userId", userId);

            int affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }
        catch (PostgresException ex)
        {
            Console.WriteLine($"Ошибка БД: {ex.Message}");
            return false;
        }
    }
    public async Task<int> GetDisplaySeatNumberAsync(int sessionId, int rowNum, int realSeatNum)
    {
        using var connection = await _db.GetConnectionAsync();

        string hallQuery = "SELECT hall_id FROM \"session\" WHERE id = @sessionId";
        using var cmd = new NpgsqlCommand(hallQuery, connection);
        cmd.Parameters.AddWithValue("sessionId", sessionId);
        var hallObj = await cmd.ExecuteScalarAsync();
        if (hallObj == null) return realSeatNum;
        int hallId = Convert.ToInt32(hallObj);

        string seatQuery = "SELECT row_num, seat_num FROM seat WHERE hall_id = @hallId ORDER BY row_num, seat_num";
        using var seatCmd = new NpgsqlCommand(seatQuery, connection);
        seatCmd.Parameters.AddWithValue("hallId", hallId);
        using var reader = await seatCmd.ExecuteReaderAsync();

        var seatsInRow = new List<int>();
        while (await reader.ReadAsync())
        {
            int r = reader.GetInt32(0);
            int s = reader.GetInt32(1);
            if (r == rowNum)
                seatsInRow.Add(s);
        }

        int index = seatsInRow.IndexOf(realSeatNum);
        return index >= 0 ? index + 1 : realSeatNum;
    }

    public async Task<List<UserTicketDetail>> GetUserTicketsAsync(int userId)
    {
        var tickets = new List<UserTicketDetail>();
        using var connection = await _db.GetConnectionAsync();

        string query = @"
        SELECT c.name AS cinema_name, h.hall_number, m.title AS movie_title,
               s.start_time, s.price, t.row_num, t.seat_num, s.id AS session_id
        FROM ticket t
        JOIN ""session"" s ON t.session_id = s.id
        JOIN hall h ON s.hall_id = h.id
        JOIN cinema c ON h.cinema_id = c.id
        JOIN movie m ON s.movie_id = m.id
        WHERE t.user_id = @userId AND t.is_sold = true
          AND s.start_time + (m.duration_minutes * INTERVAL '1 minute') > NOW()
        ORDER BY s.start_time DESC";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("userId", userId);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tickets.Add(new UserTicketDetail
            {
                CinemaName = reader.GetString(0),
                HallNumber = reader.GetInt32(1),
                MovieTitle = reader.GetString(2),
                StartTime = reader.GetDateTime(3),
                Price = reader.GetDecimal(4),
                RowNum = reader.GetInt32(5),
                SeatNum = reader.GetInt32(6),
                SessionId = reader.GetInt32(7)
            });
        }

        foreach (var ticket in tickets)
        {
            ticket.DisplaySeatNum = await GetDisplaySeatNumberAsync(ticket.SessionId, ticket.RowNum, ticket.SeatNum);
        }

        return tickets;
    }
    public async Task GenerateTicketsAsync(int sessionId, List<(int Row, int Seat)> seats)
    {
        using var connection = await _db.GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();
        try
        {
            foreach (var (row, seat) in seats)
            {
                string query = @"
                INSERT INTO ticket (session_id, row_num, seat_num, is_sold)
                VALUES (@session_id, @row, @seat, false)
                ON CONFLICT (session_id, row_num, seat_num) DO NOTHING";
                using var command = new NpgsqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("session_id", sessionId);
                command.Parameters.AddWithValue("row", row);
                command.Parameters.AddWithValue("seat", seat);
                await command.ExecuteNonQueryAsync();
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