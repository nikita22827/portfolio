using CinemaReferenceSystem.Models;
using Npgsql;

namespace CinemaReferenceSystem.Services;

public class ReviewService
{
    private readonly DatabaseService _db;
    public ReviewService(DatabaseService db) => _db = db;

    public async Task<List<Review>> GetCinemaReviewsAsync(int cinemaId, int currentUserId)
    {
        var reviews = new List<Review>();
        using var conn = await _db.GetConnectionAsync();

        string sql = @"
            SELECT r.id, r.cinema_id, r.user_id, u.username, r.rating, r.comment, r.created_at,
                   COALESCE(likes.c, 0) AS likes,
                   COALESCE(dislikes.c, 0) AS dislikes,
                   v.vote AS current_vote
            FROM review r
            JOIN app_user u ON r.user_id = u.id
            LEFT JOIN LATERAL (
                SELECT COUNT(*) c FROM review_vote WHERE review_id = r.id AND vote = 1
            ) likes ON true
            LEFT JOIN LATERAL (
                SELECT COUNT(*) c FROM review_vote WHERE review_id = r.id AND vote = -1
            ) dislikes ON true
            LEFT JOIN review_vote v ON v.review_id = r.id AND v.user_id = @uid
            WHERE r.cinema_id = @cid
            ORDER BY r.created_at DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cid", cinemaId);
        cmd.Parameters.AddWithValue("uid", currentUserId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reviews.Add(new Review
            {
                Id = reader.GetInt32(0),
                CinemaId = reader.GetInt32(1),
                MovieId = null,
                UserId = reader.GetInt32(2),
                Username = reader.GetString(3),
                Rating = reader.GetInt32(4),
                Comment = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = reader.GetDateTime(6),
                Likes = reader.GetInt32(7),
                Dislikes = reader.GetInt32(8),
                CurrentUserVote = reader.IsDBNull(9) ? null : reader.GetInt16(9)
            });
        }

        return reviews;
    }

    public async Task<List<Review>> GetMovieReviewsAsync(int movieId, int currentUserId)
    {
        var reviews = new List<Review>();
        using var conn = await _db.GetConnectionAsync();

        string sql = @"
            SELECT r.id, r.movie_id, r.user_id, u.username, r.rating, r.comment, r.created_at,
                   COALESCE(likes.c, 0) AS likes,
                   COALESCE(dislikes.c, 0) AS dislikes,
                   v.vote AS current_vote
            FROM movie_review r
            JOIN app_user u ON r.user_id = u.id
            LEFT JOIN LATERAL (
                SELECT COUNT(*) c FROM movie_review_vote WHERE review_id = r.id AND vote = 1
            ) likes ON true
            LEFT JOIN LATERAL (
                SELECT COUNT(*) c FROM movie_review_vote WHERE review_id = r.id AND vote = -1
            ) dislikes ON true
            LEFT JOIN movie_review_vote v ON v.review_id = r.id AND v.user_id = @uid
            WHERE r.movie_id = @mid
            ORDER BY r.created_at DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("mid", movieId);
        cmd.Parameters.AddWithValue("uid", currentUserId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reviews.Add(new Review
            {
                Id = reader.GetInt32(0),
                CinemaId = null,
                MovieId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Username = reader.GetString(3),
                Rating = reader.GetInt32(4),
                Comment = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = reader.GetDateTime(6),
                Likes = reader.GetInt32(7),
                Dislikes = reader.GetInt32(8),
                CurrentUserVote = reader.IsDBNull(9) ? null : reader.GetInt16(9)
            });
        }

        return reviews;
    }

    public async Task UpsertCinemaReviewAsync(int cinemaId, int userId, int rating, string? comment)
    {
        using var conn = await _db.GetConnectionAsync();
        string sql = @"
            INSERT INTO review (cinema_id, user_id, rating, comment)
            VALUES (@cid, @uid, @rating, @comment)
            ON CONFLICT (cinema_id, user_id) DO UPDATE SET
                rating = EXCLUDED.rating,
                comment = EXCLUDED.comment,
                created_at = CURRENT_TIMESTAMP";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cid", cinemaId);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("rating", rating);
        cmd.Parameters.AddWithValue("comment", (object?)comment ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpsertMovieReviewAsync(int movieId, int userId, int rating, string? comment)
    {
        using var conn = await _db.GetConnectionAsync();
        string sql = @"
            INSERT INTO movie_review (movie_id, user_id, rating, comment)
            VALUES (@mid, @uid, @rating, @comment)
            ON CONFLICT (movie_id, user_id) DO UPDATE SET
                rating = EXCLUDED.rating,
                comment = EXCLUDED.comment,
                created_at = CURRENT_TIMESTAMP";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("mid", movieId);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("rating", rating);
        cmd.Parameters.AddWithValue("comment", (object?)comment ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task VoteCinemaReviewAsync(int reviewId, int userId, int vote)
    {
        await VoteAsync("review_vote", reviewId, userId, vote);
    }

    public async Task VoteMovieReviewAsync(int reviewId, int userId, int vote)
    {
        await VoteAsync("movie_review_vote", reviewId, userId, vote);
    }

    private async Task VoteAsync(string voteTable, int reviewId, int userId, int vote)
    {
        using var conn = await _db.GetConnectionAsync();

        string del = $"DELETE FROM {voteTable} WHERE review_id = @rid AND user_id = @uid";
        using (var cmd = new NpgsqlCommand(del, conn))
        {
            cmd.Parameters.AddWithValue("rid", reviewId);
            cmd.Parameters.AddWithValue("uid", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        if (vote != 0)
        {
            string ins = $"INSERT INTO {voteTable} (review_id, user_id, vote) VALUES (@rid, @uid, @vote)";
            using var cmd = new NpgsqlCommand(ins, conn);
            cmd.Parameters.AddWithValue("rid", reviewId);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("vote", vote);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}