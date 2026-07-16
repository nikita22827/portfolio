using CinemaReferenceSystem.Controls;
using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;

namespace CinemaReferenceSystem.Forms;

public partial class CinemaDetailForm : Form
{
    private readonly CinemaService _cinemaService;
    private readonly HallService _hallService;
    private readonly MovieService _movieService;
    private readonly SessionService _sessionService;
    private readonly TicketService _ticketService;
    private readonly ReviewService _reviewService;
    private readonly User _currentUser;
    private int _cinemaId;
    private Cinema? _cinema;

    private Panel pnlHeader = new Panel();
    private Panel pnlReviewForm = new Panel();
    private Panel pnlReviewsWrapper = new Panel();
    private FlowLayoutPanel reviewsPanel = new FlowLayoutPanel();
    private Panel bottomToolbar = new Panel();

    private Label lblAddress = new Label();
    private Label lblPhone = new Label();
    private Label lblDescription = new Label();
    private StarRatingControl ratingPicker = new StarRatingControl();
    private TextBox txtComment = new TextBox();
    private Button btnSubmitReview = new Button();
    private StarRatingControl cinemaRatingStars = new StarRatingControl();
    private Label cinemaRatingLabel = new Label();

    private readonly Color _primaryColor = Color.FromArgb(32, 178, 170);
    private readonly Color _bgColor = Color.FromArgb(245, 247, 250);
    private readonly Color _cardColor = Color.White;
    private readonly Color _textColor = Color.FromArgb(50, 50, 50);

    public CinemaDetailForm(CinemaService cinemaService, HallService hallService,
        MovieService movieService, SessionService sessionService,
        TicketService ticketService, ReviewService reviewService,
        User currentUser, int cinemaId)
    {
        _cinemaService = cinemaService;
        _hallService = hallService;
        _movieService = movieService;
        _sessionService = sessionService;
        _ticketService = ticketService;
        _reviewService = reviewService;
        _currentUser = currentUser;
        _cinemaId = cinemaId;

        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _cinema = await _cinemaService.GetFullByIdAsync(_cinemaId);
        if (_cinema == null) { MessageBox.Show("Кинотеатр не найден"); Close(); return; }

        cinemaRatingStars.Value = _cinema.AverageRating;
        cinemaRatingLabel.Text = $"{_cinema.AverageRating:F1}";
        lblAddress.Text = $"Адрес: {_cinema.City}, {_cinema.Address}";
        lblPhone.Text = $"Телефон: {_cinema.Phone ?? "не указан"}";
        Text = _cinema.Name;

        lblDescription.Text = _cinema.Description ?? "Описание отсутствует";
        lblDescription.MaximumSize = new Size(this.ClientSize.Width - 40, 0);
        lblDescription.Size = lblDescription.GetPreferredSize(new Size(this.ClientSize.Width - 40, 0));
        pnlHeader.Height = lblDescription.Bottom + 20;

        var reviews = await _reviewService.GetCinemaReviewsAsync(_cinemaId, _currentUser.Id);
        var userReview = reviews.FirstOrDefault(r => r.UserId == _currentUser.Id);

        pnlReviewForm.Visible = true;

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

        RenderReviews(reviews);
    }

    private void RenderReviews(List<Review> reviews)
    {
        ClearPanelAndDisposeControls(reviewsPanel);

        reviewsPanel.AutoSize = false;

        int y = 0;
        foreach (var r in reviews)
        {
            var card = BuildReviewPanel(r);
            card.Top = y;
            card.Width = pnlReviewsWrapper.ClientSize.Width - 40;
            reviewsPanel.Controls.Add(card);
            y += card.Height + card.Margin.Bottom;
        }

        reviewsPanel.Width = pnlReviewsWrapper.ClientSize.Width - pnlReviewsWrapper.Padding.Horizontal;
        reviewsPanel.Height = Math.Max(y, pnlReviewsWrapper.ClientSize.Height);
    }

    private Panel BuildReviewPanel(Review review)
    {
        var panel = new Panel
        {
            Width = reviewsPanel.ClientSize.Width - 25,
            BackColor = _cardColor,
            Margin = new Padding(10, 5, 10, 10)
        };

        var lblUser = new Label
        {
            Text = review.Username,
            Location = new Point(15, 12),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            ForeColor = _primaryColor
        };

        var lblDate = new Label
        {
            Text = review.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            Location = new Point(lblUser.Right + 10, 14),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            AutoSize = true
        };

        var reviewStars = new StarRatingControl
        {
            Value = review.Rating,
            ReadOnly = true,
            Size = new Size(90, 15),
            Location = new Point(panel.Width - 110, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        string commentText = string.IsNullOrWhiteSpace(review.Comment) ? "Без комментария" : review.Comment;

        var lblReviewText = new Label
        {
            Text = commentText,
            Location = new Point(15, 42),
            Font = new Font("Segoe UI", 10),
            ForeColor = _textColor,
            AutoSize = true,
            MaximumSize = new Size(panel.Width - 30, 0)
        };

        lblReviewText.Size = lblReviewText.GetPreferredSize(new Size(panel.Width - 30, 0));

        int buttonsTop = lblReviewText.Bottom + 10;

        var btnLike = CreateStyledButton($"👍 {review.Likes}", Color.White, _primaryColor, 60);
        btnLike.Location = new Point(15, buttonsTop);
        btnLike.Height = 30;
        btnLike.FlatAppearance.BorderSize = 1;

        var btnDislike = CreateStyledButton($"👎 {review.Dislikes}", Color.White, Color.Gray, 60);
        btnDislike.Location = new Point(80, buttonsTop);
        btnDislike.Height = 30;
        btnDislike.FlatAppearance.BorderSize = 1;

        btnLike.Click += async (s, e) => { await _reviewService.VoteCinemaReviewAsync(review.Id, _currentUser.Id, 1); await RefreshReviews(); };
        btnDislike.Click += async (s, e) => { await _reviewService.VoteCinemaReviewAsync(review.Id, _currentUser.Id, -1); await RefreshReviews(); };

        panel.Controls.Add(lblUser);
        panel.Controls.Add(lblDate);
        panel.Controls.Add(reviewStars);
        panel.Controls.Add(lblReviewText);
        panel.Controls.Add(btnLike);
        panel.Controls.Add(btnDislike);

        panel.Height = btnLike.Bottom + 12;

        return panel;
    }

    private async void BtnSubmitReview_Click(object sender, EventArgs e)
    {
        int rating = (int)ratingPicker.Value;
        if (rating < 1 || rating > 10)
        {
            MessageBox.Show("Пожалуйста, выберите оценку (нажмите на звёзды).", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string? comment = string.IsNullOrWhiteSpace(txtComment.Text) ? null : txtComment.Text;
        await _reviewService.UpsertCinemaReviewAsync(_cinemaId, _currentUser.Id, rating, comment);

        MessageBox.Show("Ваш отзыв успешно сохранён!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await RefreshReviews();
    }

    private async Task RefreshReviews()
    {
        var reviews = await _reviewService.GetCinemaReviewsAsync(_cinemaId, _currentUser.Id);
        RenderReviews(reviews);
    }

    private void BtnHalls_Click(object sender, EventArgs e)
    {
        if (_currentUser.Role != "admin") return;
        using var hallsForm = new HallManagementForm(_cinemaId, _hallService, _ticketService, _sessionService);
        hallsForm.ShowDialog();
    }

    private async void BtnSessions_Click(object sender, EventArgs e)
    {
        using var sessionsForm = new SessionListForm(
            _cinemaId, _sessionService, _ticketService, _hallService, _movieService, _currentUser
        );
        sessionsForm.ShowDialog();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1000, 750);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = _bgColor;
        this.Font = new Font("Segoe UI", 10);

        this.SizeChanged += (s, e) =>
        {
            if (lblDescription != null && pnlHeader != null)
            {
                lblDescription.MaximumSize = new Size(pnlHeader.Width - 40, 0);
                lblDescription.Size = lblDescription.GetPreferredSize(new Size(pnlHeader.Width - 40, 0));
                pnlHeader.Height = lblDescription.Bottom + 20;
            }
        };
        bottomToolbar.Dock = DockStyle.Bottom;
        bottomToolbar.Height = 60;
        bottomToolbar.BackColor = _cardColor;
        bottomToolbar.Padding = new Padding(15);

        var btnSessions = CreateStyledButton("🎬 Показать сеансы", _primaryColor, Color.White, 180);
        btnSessions.Location = new Point(bottomToolbar.Width - btnSessions.Width - 15, 10);
        btnSessions.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnSessions.Click += BtnSessions_Click;
        bottomToolbar.Controls.Add(btnSessions);

        if (_currentUser.Role == "admin")
        {
            var btnHalls = CreateStyledButton("Управление залами", Color.White, _primaryColor, 180);
            btnHalls.Location = new Point(btnSessions.Left - 190, 10);
            btnHalls.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnHalls.FlatAppearance.BorderSize = 1;
            btnHalls.Click += BtnHalls_Click;
            bottomToolbar.Controls.Add(btnHalls);
        }

        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 180;
        pnlHeader.BackColor = _cardColor;
        pnlHeader.Padding = new Padding(20);

        cinemaRatingStars.Location = new Point(20, 20);
        cinemaRatingStars.Size = new Size(140, 25);
        cinemaRatingStars.ReadOnly = true;

        cinemaRatingLabel.Location = new Point(170, 20);
        cinemaRatingLabel.AutoSize = true;
        cinemaRatingLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        cinemaRatingLabel.ForeColor = _textColor;

        lblAddress.Location = new Point(20, 55);
        lblAddress.AutoSize = true;
        lblAddress.ForeColor = Color.Gray;

        lblPhone.Location = new Point(20, 80);
        lblPhone.AutoSize = true;
        lblPhone.ForeColor = Color.Gray;

        lblDescription.Location = new Point(20, 110);
        lblDescription.AutoSize = true;
        lblDescription.Font = new Font("Segoe UI", 10);
        lblDescription.ForeColor = _textColor;

        pnlHeader.Controls.AddRange(new Control[] { cinemaRatingStars, cinemaRatingLabel, lblAddress, lblPhone, lblDescription });

        pnlReviewForm.Dock = DockStyle.Top;
        pnlReviewForm.Height = 150;
        pnlReviewForm.BackColor = _bgColor;
        pnlReviewForm.Padding = new Padding(20, 10, 20, 10);

        var reviewFormCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _cardColor,
            Padding = new Padding(15)
        };

        var lblRate = new Label { Text = "Ваша оценка:", Location = new Point(15, 18), AutoSize = true };
        ratingPicker.Location = new Point(120, 15);
        ratingPicker.Size = new Size(160, 25);
        ratingPicker.ReadOnly = false;

        var lblComment = new Label { Text = "Комментарий:", Location = new Point(15, 55), AutoSize = true };

        txtComment.Location = new Point(120, 50);
        txtComment.Width = reviewFormCard.Width - 300;
        txtComment.Height = 60;
        txtComment.Multiline = true;
        txtComment.BorderStyle = BorderStyle.FixedSingle;
        txtComment.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnSubmitReview = CreateStyledButton("Отправить", _primaryColor, Color.White, 130);
        btnSubmitReview.Location = new Point(reviewFormCard.Width - 150, 50);
        btnSubmitReview.Height = 60;
        btnSubmitReview.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSubmitReview.Click += BtnSubmitReview_Click;

        reviewFormCard.Controls.AddRange(new Control[] { lblRate, ratingPicker, lblComment, txtComment, btnSubmitReview });
        pnlReviewForm.Controls.Add(reviewFormCard);

        pnlReviewsWrapper.Dock = DockStyle.Fill;
        pnlReviewsWrapper.BackColor = _bgColor;
        pnlReviewsWrapper.Padding = new Padding(10, 0, 10, 10);
        pnlReviewsWrapper.AutoScroll = true;

        reviewsPanel.FlowDirection = FlowDirection.TopDown;
        reviewsPanel.WrapContents = false;
        reviewsPanel.AutoSize = true;
        reviewsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        reviewsPanel.Width = pnlReviewsWrapper.ClientSize.Width - pnlReviewsWrapper.Padding.Horizontal;

        pnlReviewsWrapper.SizeChanged += (s, e) =>
        {
            reviewsPanel.Width = pnlReviewsWrapper.ClientSize.Width - pnlReviewsWrapper.Padding.Horizontal;
            foreach (Control c in reviewsPanel.Controls)
                c.Width = reviewsPanel.Width - 25;
        };


        pnlReviewsWrapper.Controls.Add(reviewsPanel);

        this.Controls.Add(pnlReviewsWrapper);
        this.Controls.Add(bottomToolbar);
        this.Controls.Add(pnlReviewForm);
        this.Controls.Add(pnlHeader);
    }

    private Button CreateStyledButton(string text, Color backColor, Color foreColor, int width)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 40,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.BorderColor = _primaryColor;
        return btn;
    }

    private void ClearPanelAndDisposeControls(Panel panel)
    {
        for (int i = panel.Controls.Count - 1; i >= 0; i--)
        {
            var ctrl = panel.Controls[i];
            panel.Controls.RemoveAt(i);
            ctrl.Dispose();
        }
    }
}