using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;
using CinemaReferenceSystem.Controls;

namespace CinemaReferenceSystem.Forms;

public partial class MovieSessionsForm : Form
{
    private readonly int _movieId;
    private readonly SessionService _sessionService;
    private readonly HallService _hallService;
    private readonly TicketService _ticketService;
    private readonly User _currentUser;
    private readonly string _city;

    private DateTime _selectedDate;
    private List<MovieSessionInfo> _allSessions = new();
    private FlowLayoutPanel sessionsPanel;
    private Panel datePanel;

    public MovieSessionsForm(int movieId, SessionService sessionService,
        HallService hallService, TicketService ticketService, User currentUser, string city)
    {
        _movieId = movieId;
        _sessionService = sessionService;
        _hallService = hallService;
        _ticketService = ticketService;
        _currentUser = currentUser;
        _city = city;

        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _allSessions = await _sessionService.GetSessionsForMovieAsync(_movieId, _city);

            var today = DateTime.Today;
            var availableDates = _allSessions
                .Select(s => s.StartTime.Date)
                .Distinct()
                .Where(d => d >= today)
                .OrderBy(d => d)
                .Take(7)
                .ToList();

            if (availableDates.Count == 0)
            {
                MessageBox.Show("Нет доступных сеансов для этого фильма.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                return;
            }

            _selectedDate = availableDates.First();
            BuildDateButtons(availableDates);
            await ShowSessionsForSelectedDate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
        }
    }

    private void InitializeComponent()
    {
        this.Text = "Сеансы фильма";
        this.Size = new Size(950, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.White;

        datePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(15, 5, 15, 5)
        };
        var lineBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(222, 226, 230) };
        datePanel.Controls.Add(lineBottom);
        this.Controls.Add(datePanel);

        sessionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(20, 20, 20, 20),
            BackColor = Color.White
        };
        this.Controls.Add(sessionsPanel);
        sessionsPanel.BringToFront();

        this.Resize += (s, e) => { RefreshCinemaWidths(); };
    }

    private void RefreshCinemaWidths()
    {
        int safeWidth = sessionsPanel.ClientSize.Width - sessionsPanel.Padding.Horizontal - 5;
        foreach (Control ctrl in sessionsPanel.Controls)
        {
            if (ctrl is Panel pnl)
            {
                pnl.Width = safeWidth;
                foreach (Control subCtrl in pnl.Controls)
                {
                    if (subCtrl is FlowLayoutPanel flow)
                        flow.Width = pnl.Width - flow.Left - 15;
                }
            }
        }
    }

    private void BuildDateButtons(List<DateTime> dates)
    {
        datePanel.Controls.Clear();
        var lineBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(222, 226, 230) };
        datePanel.Controls.Add(lineBottom);

        int x = 20;
        foreach (var date in dates)
        {
            string text;
            if (date == DateTime.Today) text = $"Сегодня\n{date:dd MMM}";
            else if (date == DateTime.Today.AddDays(1)) text = $"Завтра\n{date:dd MMM}";
            else if (date == DateTime.Today.AddDays(2)) text = $"Послезавтра\n{date:dd MMM}";
            else text = $"{date:dddd}\n{date:dd MMM}";

            bool isSelected = (date == _selectedDate);

            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9, isSelected ? FontStyle.Bold : FontStyle.Regular),
                Size = new Size(125, 48),
                Location = new Point(x, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = isSelected ? Color.FromArgb(13, 110, 253) : Color.White,
                ForeColor = isSelected ? Color.White : Color.FromArgb(73, 80, 87),
                Tag = date,
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = isSelected ? 0 : 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(222, 226, 230);

            btn.Click += DateButton_Click;
            datePanel.Controls.Add(btn);
            x += 135;
        }
    }

    private async void DateButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is DateTime date)
        {
            _selectedDate = date;
            foreach (Button b in datePanel.Controls.OfType<Button>())
            {
                DateTime dt = (DateTime)b.Tag;
                bool isSel = (dt == _selectedDate);

                b.BackColor = isSel ? Color.FromArgb(13, 110, 253) : Color.White;
                b.ForeColor = isSel ? Color.White : Color.FromArgb(73, 80, 87);
                b.Font = new Font("Segoe UI", 9, isSel ? FontStyle.Bold : FontStyle.Regular);
                b.FlatAppearance.BorderSize = isSel ? 0 : 1;
            }
            await ShowSessionsForSelectedDate();
        }
    }

    private async Task ShowSessionsForSelectedDate()
    {
        sessionsPanel.Controls.Clear();

        var sessions = _allSessions
            .Where(s => s.StartTime.Date == _selectedDate.Date)
            .OrderBy(s => s.StartTime)
            .ToList();

        if (sessions.Count == 0)
        {
            var lbl = new Label
            {
                Text = "На выбранную дату сеансов не найдено.",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(10, 20, 0, 0)
            };
            sessionsPanel.Controls.Add(lbl);
            return;
        }

        var groups = sessions.GroupBy(s => s.CinemaId);
        int initialWidth = sessionsPanel.ClientSize.Width > 40 ? sessionsPanel.ClientSize.Width - 45 : 850;

        foreach (var group in groups)
        {
            var first = group.First();

            var cinemaPanel = new Panel
            {
                Width = initialWidth,
                Height = 110,
                Margin = new Padding(0, 0, 0, 15),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var infoPanel = new Panel
            {
                Location = new Point(15, 12),
                Size = new Size(280, 90),
                BackColor = Color.Transparent
            };

            var lblCinema = new Label
            {
                Text = $"Кинотеатр \"{first.CinemaName}\"",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(33, 37, 41)
            };
            infoPanel.Controls.Add(lblCinema);

            var lblAddress = new Label
            {
                Text = first.CinemaAddress,
                Font = new Font("Segoe UI", 9),
                Location = new Point(0, 26),
                AutoSize = true,
                ForeColor = Color.FromArgb(108, 117, 125)
            };
            infoPanel.Controls.Add(lblAddress);

            var ratingStars = new StarRatingControl
            {
                Value = first.CinemaRating,
                ReadOnly = true,
                Size = new Size(100, 18),
                Location = new Point(0, 52)
            };
            infoPanel.Controls.Add(ratingStars);

            var ratingLabel = new Label
            {
                Text = $"{first.CinemaRating:F1}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Orange,
                Location = new Point(105, 52),
                AutoSize = true
            };
            infoPanel.Controls.Add(ratingLabel);

            cinemaPanel.Controls.Add(infoPanel);

            var sessionsFlow = new FlowLayoutPanel
            {
                Location = new Point(310, 12),
                Size = new Size(cinemaPanel.Width - 325, 85),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            foreach (var session in group.OrderBy(s => s.StartTime))
            {
                var btnSession = new Button
                {
                    Text = $"{session.StartTime:HH:mm}\nот {session.Price:F0} ₽",
                    Size = new Size(105, 52),
                    Margin = new Padding(0, 0, 10, 10),
                    BackColor = Color.FromArgb(40, 167, 69),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Tag = session,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnSession.FlatAppearance.BorderSize = 0;

                btnSession.Click += async (s, e) =>
                {
                    if (s is Button btn && btn.Tag is MovieSessionInfo info)
                        await OpenTicketPurchase(info);
                };
                sessionsFlow.Controls.Add(btnSession);
            }

            cinemaPanel.Controls.Add(sessionsFlow);
            sessionsPanel.Controls.Add(cinemaPanel);
        }

        RefreshCinemaWidths();
    }

    private async Task OpenTicketPurchase(MovieSessionInfo sessionInfo)
    {
        string sessionDescription = $"{sessionInfo.CinemaName} • Зал {sessionInfo.HallNumber}\n" +
                                    $"{sessionInfo.StartTime:dd.MM.yyyy HH:mm} • {sessionInfo.Price} ₽";

        using var purchaseForm = new TicketPurchaseForm(
            _ticketService,
            _hallService,
            _sessionService,
            sessionInfo.SessionId,
            sessionDescription,
            _currentUser.Id,
            sessionInfo.Price
        );
        purchaseForm.ShowDialog();
    }
}