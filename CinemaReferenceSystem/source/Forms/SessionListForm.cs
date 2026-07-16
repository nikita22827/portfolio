using CinemaReferenceSystem.Controls;
using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;
using CinemaReferenceSystem.Utils;

namespace CinemaReferenceSystem.Forms;

public partial class SessionListForm : Form
{
    private readonly int _cinemaId;
    private readonly SessionService _sessionService;
    private readonly TicketService _ticketService;
    private readonly HallService _hallService;
    private readonly MovieService _movieService;
    private readonly User _currentUser;

    private DateTime _selectedDate = DateTime.Today;
    private FlowLayoutPanel sessionsPanel;
    private Panel datePanel;
    private List<MovieSessionInfo> _allSessions = new();

    public SessionListForm(int cinemaId, SessionService sessionService,
        TicketService ticketService, HallService hallService,
        MovieService movieService, User currentUser)
    {
        _cinemaId = cinemaId;
        _sessionService = sessionService;
        _ticketService = ticketService;
        _hallService = hallService;
        _movieService = movieService;
        _currentUser = currentUser;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = "Сеансы в кинотеатре";
        Size = new Size(1000, 750);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 245, 250);
        Font = new Font("Segoe UI", 10);

        var toolPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(15, 10, 15, 10),
            BackColor = Color.White,
            WrapContents = false
        };

        Button CreateFlatButton(string text, int width, Color back, Color fore)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 38,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            if (back == Color.White) btn.FlatAppearance.BorderColor = Color.LightGray;
            else btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        var btnRefresh = CreateFlatButton("🔄 Обновить", 120, Color.White, Color.Black);
        btnRefresh.Click += async (s, e) => await LoadDataAsync();
        toolPanel.Controls.Add(btnRefresh);

        if (_currentUser.Role == "admin")
        {
            var btnAdd = CreateFlatButton("➕ Добавить сеанс", 160, Color.White, Color.Black);
            btnAdd.Click += async (s, e) => { await AddSession(); await LoadDataAsync(); };
            toolPanel.Controls.Add(btnAdd);

            var btnEdit = CreateFlatButton("✏️ Редактировать", 150, Color.White, Color.Black);
            btnEdit.Click += async (s, e) => { await EditSession(); await LoadDataAsync(); };
            toolPanel.Controls.Add(btnEdit);

            var btnDelete = CreateFlatButton("🗑️ Удалить", 120, Color.FromArgb(240, 80, 80), Color.White);
            btnDelete.Click += async (s, e) => { await DeleteSession(); await LoadDataAsync(); };
            toolPanel.Controls.Add(btnDelete);
        }

        datePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(10, 10, 0, 10),
            BackColor = Color.White,
            WrapContents = false,
            AutoScroll = true
        };

        sessionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(15)
        };

        this.Controls.Add(sessionsPanel);
        this.Controls.Add(datePanel);
        this.Controls.Add(toolPanel);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var sessions = await _sessionService.GetDetailedByCinemaIdAsync(_cinemaId);
            _allSessions = sessions.Select(s => new MovieSessionInfo
            {
                SessionId = s.Id,
                MovieTitle = s.MovieTitle,
                HallNumber = s.HallNumber,
                StartTime = s.StartTime,
                Price = s.Price
            }).ToList();

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
                _selectedDate = today;
                datePanel.Controls.Clear();
                MessageBox.Show("На ближайшие дни сеансов нет.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (!availableDates.Contains(_selectedDate))
                    _selectedDate = availableDates.First();
                BuildDateButtons(availableDates);
            }

            RenderMoviePanels();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки сеансов: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BuildDateButtons(List<DateTime> dates)
    {
        datePanel.Controls.Clear();
        foreach (var date in dates)
        {
            string text = date == DateTime.Today ? $"Сегодня\n{date:dd MMM}" :
                          date == DateTime.Today.AddDays(1) ? $"Завтра\n{date:dd MMM}" :
                          date == DateTime.Today.AddDays(2) ? $"Послезавтра\n{date:dd MMM}" :
                          $"{date:dddd}\n{date:dd MMM}";

            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Size = new Size(110, 45),
                Margin = new Padding(5, 0, 5, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = date == _selectedDate ? Color.FromArgb(0, 120, 215) : Color.White,
                ForeColor = date == _selectedDate ? Color.White : Color.Black,
                Tag = date,
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = date == _selectedDate ? 0 : 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(230, 230, 230);

            btn.Click += DateButton_Click;
            datePanel.Controls.Add(btn);
        }
    }

    private void DateButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is DateTime date)
        {
            _selectedDate = date;
            foreach (Button b in datePanel.Controls.OfType<Button>())
            {
                DateTime dt = (DateTime)b.Tag;
                bool isSelected = dt == _selectedDate;
                b.BackColor = isSelected ? Color.FromArgb(0, 120, 215) : Color.White;
                b.ForeColor = isSelected ? Color.White : Color.Black;
                b.FlatAppearance.BorderSize = isSelected ? 0 : 1;
            }
            RenderMoviePanels();
        }
    }

    private async void RenderMoviePanels()
    {
        sessionsPanel.Controls.Clear();

        var filteredSessions = _allSessions
            .Where(s => s.StartTime.Date == _selectedDate.Date)
            .ToList();

        if (filteredSessions.Count == 0)
        {
            sessionsPanel.Controls.Add(new Label
            {
                Text = "На выбранную дату сеансов нет.",
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Margin = new Padding(10)
            });
            return;
        }

        var allMovies = await _movieService.GetAllAsync();
        var movieGroups = filteredSessions.GroupBy(s => s.MovieTitle);

        foreach (var group in movieGroups)
        {
            var movieInfo = allMovies.FirstOrDefault(m => m.Title == group.Key);

            var card = new Panel
            {
                Width = sessionsPanel.ClientSize.Width - 35,
                Height = 200,
                BackColor = Color.White,
                Margin = new Padding(0, 5, 0, 15),
                Padding = new Padding(15)
            };

            PictureBox posterBox = new PictureBox
            {
                Size = new Size(110, 160),
                Location = new Point(20, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            if (movieInfo != null && !string.IsNullOrEmpty(movieInfo.PosterUrl))
            {
                try { posterBox.LoadAsync(movieInfo.PosterUrl); } catch { }
            }
            card.Controls.Add(posterBox);

            var lblTitle = new Label
            {
                Text = group.Key,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(150, 20),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            var lblGenre = new Label
            {
                Text = movieInfo?.Genre ?? "жанр не указан",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(153, 50),
                AutoSize = true
            };
            card.Controls.Add(lblGenre);

            string year = movieInfo?.ReleaseYear?.ToString() ?? "—";
            string country = movieInfo?.Country ?? "—";
            var lblDetails = new Label
            {
                Text = $"{country}, {year}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(153, 70),
                AutoSize = true
            };
            card.Controls.Add(lblDetails);

            if (movieInfo != null)
            {
                var ratingStars = new StarRatingControl
                {
                    Value = movieInfo.AverageRating,
                    ReadOnly = true,
                    Size = new Size(110, 20),
                    Location = new Point(150, 95)
                };
                card.Controls.Add(ratingStars);
            }

            var sessionsFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Location = new Point(150, 130),
                Size = new Size(card.Width - 180, 60),
                AutoScroll = true
            };

            foreach (var session in group.OrderBy(s => s.StartTime))
            {
                var btnSession = new Button
                {
                    Text = $"{session.StartTime:HH:mm}\n{session.Price:F0} ₽",
                    Size = new Size(85, 45),
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

            card.Controls.Add(sessionsFlow);
            sessionsPanel.Controls.Add(card);
        }
    }

    private async Task AddSession()
    {
        var halls = await _hallService.GetByCinemaIdAsync(_cinemaId);
        var hallItems = halls.Select(h => new KeyValuePair<object, string>(
            h.Id, $"Зал №{h.HallNumber} ({h.RowsCount} рядов)")).ToList();

        var movies = await _movieService.GetAllAsync();
        var movieItems = movies.Select(m => new KeyValuePair<object, string>(
            m.Id, m.Title)).ToList();

        var comboData = new Dictionary<string, List<KeyValuePair<object, string>>>
        {
            ["HallId"] = hallItems,
            ["MovieId"] = movieItems
        };

        var newSession = WinFormsHelper.ShowEditForm<Session>(null, "Добавление сеанса", comboData);
        if (newSession == null) return;

        bool timeAvailable = await _sessionService.IsTimeSlotAvailable(
            newSession.HallId, newSession.StartTime, newSession.MovieId);
        if (!timeAvailable)
        {
            MessageBox.Show("В этом зале в указанное время уже проходит другой сеанс (с учётом 30-минутного перерыва).",
                "Конфликт расписания", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            int sessionId = await _sessionService.AddAsync(newSession);
            var seats = await _hallService.GetSeatsAsync(newSession.HallId);
            if (seats.Any())
                await _ticketService.GenerateTicketsAsync(sessionId, seats);
            else
                MessageBox.Show("⚠️ В зале нет мест! Билеты не созданы.", "Предупреждение");

            MessageBox.Show("✅ Сеанс добавлен.", "Готово");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка добавления сеанса: {ex.Message}", "Ошибка");
        }
    }

    private async Task EditSession()
    {
        string? input = Microsoft.VisualBasic.Interaction.InputBox(
            "Введите ID сеанса для редактирования:", "Редактирование сеанса", "");
        if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int id))
            return;

        var session = await _sessionService.GetByIdAsync(id);
        if (session == null)
        {
            MessageBox.Show("Сеанс не найден!");
            return;
        }

        var halls = await _hallService.GetByCinemaIdAsync(_cinemaId);
        var hallItems = halls.Select(h => new KeyValuePair<object, string>(
            h.Id, $"Зал №{h.HallNumber}")).ToList();

        var movies = await _movieService.GetAllAsync();
        var movieItems = movies.Select(m => new KeyValuePair<object, string>(
            m.Id, m.Title)).ToList();

        var comboData = new Dictionary<string, List<KeyValuePair<object, string>>>
        {
            ["HallId"] = hallItems,
            ["MovieId"] = movieItems
        };

        var updated = WinFormsHelper.ShowEditForm(session, "Редактирование сеанса", comboData);
        if (updated != null)
        {
            bool timeAvailable = await _sessionService.IsTimeSlotAvailable(
                updated.HallId, updated.StartTime, updated.MovieId, updated.Id);
            if (!timeAvailable)
            {
                MessageBox.Show("Новое время конфликтует с другими сеансами в этом зале (с учётом 30-минутной уборки).",
                    "Конфликт расписания", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await _sessionService.UpdateAsync(updated);
            MessageBox.Show("✅ Сеанс обновлён.");
        }
    }

    private async Task DeleteSession()
    {
        string? input = Microsoft.VisualBasic.Interaction.InputBox(
            "Введите ID сеанса для удаления:", "Удаление сеанса", "");
        if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int id))
            return;

        if (MessageBox.Show($"Удалить сеанс с ID = {id}? Все билеты будут удалены.",
            "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            await _sessionService.DeleteAsync(id);
            MessageBox.Show("✅ Сеанс удалён.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
        }
    }

    private async Task OpenTicketPurchase(MovieSessionInfo sessionInfo)
    {
        string sessionDescription =
            $"{sessionInfo.CinemaName} • Зал {sessionInfo.HallNumber}\n" +
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