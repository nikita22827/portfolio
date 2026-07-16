using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;
using CinemaReferenceSystem.Controls;

namespace CinemaReferenceSystem.Forms;

public partial class MovieDetailForm : Form
{
    private readonly int _movieId;
    private readonly MovieService _movieService;
    private readonly ReviewService _reviewService;
    private readonly SessionService _sessionService;
    private readonly CinemaService _cinemaService;
    private readonly string _city;
    private readonly HallService _hallService;
    private readonly TicketService _ticketService;
    private readonly User _currentUser;
    private Movie? _movie;

    private PictureBox pnlPoster;
    private Label lblTitle, lblGenre, lblYear, lblCountry, lblDescription, lblDuration;
    private Button btnTrailer, btnSessions;
    private FlowLayoutPanel reviewsPanel;
    private StarRatingControl ratingPicker;
    private TextBox txtComment;
    private Button btnSubmitReview;
    private StarRatingControl movieRatingStars;
    private Label movieRatingLabel;

    public MovieDetailForm(int movieId, MovieService movieService, ReviewService reviewService,
            SessionService sessionService, CinemaService cinemaService, User currentUser,
            HallService hallService, TicketService ticketService, string city)
    {
        _movieId = movieId;
        _movieService = movieService;
        _reviewService = reviewService;
        _sessionService = sessionService;
        _cinemaService = cinemaService;
        _currentUser = currentUser;
        _hallService = hallService;
        _ticketService = ticketService;
        _city = city;

        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _movie = await _movieService.GetFullByIdAsync(_movieId);
        if (_movie == null) { MessageBox.Show("Фильм не найден"); Close(); return; }
        movieRatingStars.Value = _movie.AverageRating;
        movieRatingLabel.Text = $"{_movie.AverageRating:F1}";

        Text = _movie.Title;
        if (!string.IsNullOrEmpty(_movie.PosterUrl))
        {
            try
            {
                pnlPoster.LoadAsync(_movie.PosterUrl);
            }
            catch
            {
                pnlPoster.BackColor = Color.LightGray;
            }
        }
        else
        {
            pnlPoster.BackColor = Color.LightGray;
        }

        lblTitle.Text = _movie.Title;
        lblGenre.Text = _movie.Genre ?? "";
        lblYear.Text = _movie.ReleaseYear?.ToString() ?? "—";
        lblCountry.Text = _movie.Country ?? "—";
        int hours = _movie.DurationMinutes / 60;
        int minutes = _movie.DurationMinutes % 60;
        string durationText = hours > 0
            ? $"{hours} час {minutes} минут"
            : $"{minutes} минут";
        lblDuration.Text = $"Длительность: {durationText}";
        lblDescription.Text = _movie.Description ?? "Описание отсутствует";

        btnTrailer.Visible = !string.IsNullOrEmpty(_movie.TrailerUrl);
        btnTrailer.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(_movie.TrailerUrl))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_movie.TrailerUrl) { UseShellExecute = true });
        };

        var allReviews = await _reviewService.GetMovieReviewsAsync(_movieId, _currentUser.Id);
        var userReview = allReviews.FirstOrDefault(r => r.UserId == _currentUser.Id);
        if (userReview != null)
        {
            ratingPicker.Value = userReview.Rating;
            txtComment.Text = userReview.Comment ?? "";
        }
        else
        {
            ratingPicker.Value = 0;
            txtComment.Text = "";
        }
        await RefreshReviews();
    }

    private async void BtnSubmitReview_Click(object sender, EventArgs e)
    {
        int rating = (int)ratingPicker.Value;
        if (rating < 1 || rating > 10)
        {
            MessageBox.Show("Выберите оценку (нажмите на звёзды).");
            return;
        }
        string? comment = string.IsNullOrWhiteSpace(txtComment.Text) ? null : txtComment.Text;
        await _reviewService.UpsertMovieReviewAsync(_movieId, _currentUser.Id, rating, txtComment.Text);
        MessageBox.Show("Отзыв сохранён!");
        await RefreshReviews();
    }

    private void BtnSessions_Click(object? sender, EventArgs e)
    {
        using var sessionsForm = new MovieSessionsForm(
            _movieId, _sessionService, _hallService, _ticketService, _currentUser, _city);
        sessionsForm.ShowDialog();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1020, 850);
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "О фильме";
        this.BackColor = Color.White;

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 420,
            BackColor = Color.White,
            Padding = new Padding(25)
        };

        pnlPoster = new PictureBox
        {
            Size = new Size(250, 375),
            Location = new Point(25, 25),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(245, 245, 247)
        };
        topPanel.Controls.Add(pnlPoster);

        lblTitle = new Label { Font = new Font("Segoe UI", 22, FontStyle.Bold), Location = new Point(300, 25), AutoSize = true, ForeColor = Color.FromArgb(33, 37, 41) };
        lblGenre = new Label { Font = new Font("Segoe UI", 10), Location = new Point(300, 75), AutoSize = true, ForeColor = Color.Gray };
        lblYear = new Label { Font = new Font("Segoe UI", 10), Location = new Point(300, 100), AutoSize = true, ForeColor = Color.Gray };
        lblCountry = new Label { Font = new Font("Segoe UI", 10), Location = new Point(300, 125), AutoSize = true, ForeColor = Color.Gray };

        movieRatingStars = new StarRatingControl
        {
            Location = new Point(300, 155),
            Size = new Size(130, 22),
            ReadOnly = true
        };

        movieRatingLabel = new Label
        {
            Location = new Point(440, 155),
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.Orange
        };
        topPanel.Controls.Add(movieRatingStars);
        topPanel.Controls.Add(movieRatingLabel);

        lblDuration = new Label { Font = new Font("Segoe UI", 10, FontStyle.Italic), Location = new Point(300, 190), AutoSize = true, ForeColor = Color.FromArgb(108, 117, 125) };

        lblDescription = new Label
        {
            Location = new Point(300, 225),
            Width = 650,
            Height = 90,
            AutoSize = false,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(73, 80, 87),
            TextAlign = ContentAlignment.TopLeft
        };

        topPanel.Controls.AddRange(new Control[] { lblTitle, lblGenre, lblYear, lblCountry, lblDuration, lblDescription });

        btnTrailer = new Button
        {
            Text = "▶ Смотреть трейлер",
            Location = new Point(300, 335),
            Width = 180,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(240, 240, 245),
            ForeColor = Color.FromArgb(30, 30, 30),
            Cursor = Cursors.Hand
        };
        btnTrailer.FlatAppearance.BorderSize = 0;

        btnSessions = new Button
        {
            Text = "🎟️ Расписание и Билеты",
            Location = new Point(495, 335),
            Width = 220,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnSessions.FlatAppearance.BorderSize = 0;
        btnSessions.Click += BtnSessions_Click;

        topPanel.Controls.Add(btnTrailer);
        topPanel.Controls.Add(btnSessions);
        this.Controls.Add(topPanel);

        var pnlReviewForm = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 140,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(20, 15, 20, 15)
        };

        var lineTop = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(222, 226, 230) };
        pnlReviewForm.Controls.Add(lineTop);

        var lblRate = new Label { Text = "Ваша оценка", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(25, 20), AutoSize = true, ForeColor = Color.FromArgb(33, 37, 41) };
        ratingPicker = new StarRatingControl
        {
            Location = new Point(25, 45),
            Size = new Size(180, 30),
            ReadOnly = false
        };

        var lblComment = new Label { Text = "Оставить отзыв", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(230, 20), AutoSize = true, ForeColor = Color.FromArgb(33, 37, 41) };
        txtComment = new TextBox
        {
            Location = new Point(230, 45),
            Width = 550,
            Height = 70,
            Multiline = true,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle
        };

        btnSubmitReview = new Button
        {
            Text = "Отправить",
            Location = new Point(795, 45),
            Width = 140,
            Height = 70,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(33, 37, 41),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnSubmitReview.FlatAppearance.BorderSize = 0;
        btnSubmitReview.Click += BtnSubmitReview_Click;

        pnlReviewForm.Controls.AddRange(new Control[] { lblRate, ratingPicker, lblComment, txtComment, btnSubmitReview });
        this.Controls.Add(pnlReviewForm);

        reviewsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(25, 10, 25, 10),
            BackColor = Color.White
        };
        this.Controls.Add(reviewsPanel);
        reviewsPanel.BringToFront();

        this.Resize += (s, e) => { RefreshReviewWidths(); };
    }

    private void RefreshReviewWidths()
    {
        int safeWidth = reviewsPanel.ClientSize.Width - reviewsPanel.Padding.Horizontal - 5;
        foreach (Control ctrl in reviewsPanel.Controls)
        {
            if (ctrl is Panel pnl)
            {
                pnl.Width = safeWidth;
                foreach (Control subCtrl in pnl.Controls)
                {
                    if (subCtrl is TextBox txt)
                        txt.Width = pnl.Width - 30;
                }
            }
        }
    }

    private async Task RefreshReviews()
    {
        var reviews = await _reviewService.GetMovieReviewsAsync(_movieId, _currentUser.Id);
        reviewsPanel.Controls.Clear();
        foreach (var r in reviews)
        {
            reviewsPanel.Controls.Add(BuildReviewPanel(r));
        }
        RefreshReviewWidths();
    }

    private Panel BuildReviewPanel(Review review)
    {
        int initialWidth = reviewsPanel.ClientSize.Width > 50 ? reviewsPanel.ClientSize.Width - 55 : 900;

        var panel = new Panel
        {
            Width = initialWidth,
            Height = 135,
            BackColor = Color.FromArgb(245, 247, 250),
            Margin = new Padding(0, 0, 0, 15)
        };

        var lblUser = new Label { Text = string.IsNullOrEmpty(review.Username) ? "Аноним" : review.Username, Location = new Point(15, 12), Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(33, 37, 41) };
        var lblDate = new Label { Text = review.CreatedAt.ToString("dd MMMM yyyy в HH:mm"), Location = new Point(160, 14), Font = new Font("Segoe UI", 9), AutoSize = true, ForeColor = Color.Gray };
        var reviewStars = new StarRatingControl { Value = review.Rating, ReadOnly = true, Size = new Size(110, 20), Location = new Point(panel.Width - 130, 12), Anchor = AnchorStyles.Top | AnchorStyles.Right };

        var txtComment = new TextBox
        {
            Text = review.Comment,
            Location = new Point(15, 42),
            Width = panel.Width - 30,
            Height = 45,
            ReadOnly = true,
            Multiline = true,
            BorderStyle = BorderStyle.None,
            BackColor = panel.BackColor,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(43, 45, 66)
        };

        var btnLike = new Button
        {
            Text = $"👍 {review.Likes}",
            Location = new Point(15, 95),
            Width = 75,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(230, 235, 240),
            Cursor = Cursors.Hand
        };
        btnLike.FlatAppearance.BorderSize = 0;

        var btnDislike = new Button
        {
            Text = $"👎 {review.Dislikes}",
            Location = new Point(100, 95),
            Width = 75,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(230, 235, 240),
            Cursor = Cursors.Hand
        };
        btnDislike.FlatAppearance.BorderSize = 0;

        btnLike.Click += async (s, e) =>
        {
            await _reviewService.VoteMovieReviewAsync(review.Id, _currentUser.Id, 1);
            await RefreshReviews();
        };
        btnDislike.Click += async (s, e) =>
        {
            await _reviewService.VoteMovieReviewAsync(review.Id, _currentUser.Id, -1);
            await RefreshReviews();
        };

        panel.Controls.AddRange(new Control[] { lblUser, lblDate, reviewStars, txtComment, btnLike, btnDislike });
        return panel;
    }
}