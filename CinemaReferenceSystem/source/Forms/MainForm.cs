using CinemaReferenceSystem.Controls;
using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;
using CinemaReferenceSystem.Utils;

namespace CinemaReferenceSystem.Forms;

public partial class MainForm : Form
{
    private readonly User _currentUser;
    private readonly CinemaService _cinemaService;
    private readonly HallService _hallService;
    private readonly MovieService _movieService;
    private readonly SessionService _sessionService;
    private readonly TicketService _ticketService;
    private readonly DatabaseService _db;

    private string _currentCity = "Ижевск";
    public bool IsLogout { get; private set; } = false;

    private TabControl _tabControl = new TabControl();
    private Movie? _selectedMovie;
    private Panel? _selectedMovieCard;
    private Cinema? _selectedCinema;
    private Panel? _selectedCinemaCard;

    private readonly Color _primaryColor = Color.FromArgb(32, 178, 170);
    private readonly Color _dangerColor = Color.FromArgb(217, 83, 79); 
    private readonly Color _bgColor = Color.FromArgb(245, 247, 250);  
    private readonly Color _cardColor = Color.White;
    private Image? _noPosterCache;

    public MainForm(User currentUser, DatabaseService db)
    {
        _currentUser = currentUser;
        _cinemaService = new CinemaService(db);
        _hallService = new HallService(db);
        _movieService = new MovieService(db);
        _sessionService = new SessionService(db);
        _ticketService = new TicketService(db);
        _db = db;

        InitializeComponent();
        Text = $"Система кинотеатров — {_currentUser.Username} ({_currentUser.Role})";
        BackColor = _bgColor;
        GenerateNoPosterImage();
    }

    private async void InitializeComponent()
    {
        this.Size = new Size(1300, 760);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.WindowState = FormWindowState.Maximized;
        this.Font = new Font("Segoe UI", 10);

        _tabControl.Dock = DockStyle.Fill;
        _tabControl.ItemSize = new Size(120, 35);
        _tabControl.SelectedIndexChanged += async (s, e) => await LoadCurrentTab();

        var lblCity = new Label
        {
            Text = "Город:",
            Location = new Point(this.ClientSize.Width - 220, 8),
            Width = 60,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            BackColor = Color.Transparent
        };

        var cmbCity = new ComboBox
        {
            Location = new Point(this.ClientSize.Width - 160, 5),
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10),
            IntegralHeight = false,
            DropDownHeight = 150
        };

        var cities = await _cinemaService.GetCitiesAsync();
        cmbCity.DataSource = cities;

        if (cities.Contains(_currentCity))
            cmbCity.SelectedItem = _currentCity;
        else if (cities.Count > 0)
        {
            cmbCity.SelectedIndex = 0;
            _currentCity = cities[0] ?? "Ижевск";
        }

        cmbCity.SelectedIndexChanged += (s, e) =>
        {
            _currentCity = cmbCity.SelectedItem?.ToString() ?? _currentCity;
            _ = LoadCurrentTab();
        };

        this.Controls.Add(_tabControl);
        this.Controls.Add(lblCity);
        this.Controls.Add(cmbCity);
        lblCity.BringToFront();
        cmbCity.BringToFront();

        CreateCinemaTab();
        CreateMovieTab();
        CreateTab<UserTicketDetail>("Личный кабинет",
            () => _ticketService.GetUserTicketsAsync(_currentUser.Id),
            null, null);

        if (_tabControl.TabPages.Count > 0)
            _ = LoadCurrentTab();
    }


    private void GenerateNoPosterImage()
    {
        var bmp = new Bitmap(180, 250);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(230, 230, 235));
            g.DrawString("Нет постера", new Font("Segoe UI", 10, FontStyle.Bold),
                         Brushes.Gray, new PointF(40, 115));
        }
        _noPosterCache = bmp;
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

    private Button CreateStyledButton(string text, Color backColor, int width = 140)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 40,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void SelectCinemaCard(Panel card, Cinema cinema)
    {
        if (_selectedCinemaCard != null)
            _selectedCinemaCard.BorderStyle = BorderStyle.None;
        _selectedCinemaCard = card;
        _selectedCinemaCard.BorderStyle = BorderStyle.FixedSingle;
        _selectedCinema = cinema;
    }

    private void SelectMovieCard(Panel card, Movie movie)
    {
        if (_selectedMovieCard != null)
            _selectedMovieCard.BorderStyle = BorderStyle.None;
        _selectedMovieCard = card;
        _selectedMovieCard.BorderStyle = BorderStyle.FixedSingle;
        _selectedMovie = movie;
    }


    private void CreateTab<T>(string title, Func<Task<List<T>>> loadFunc,
        Func<Task>? addFunc = null, Func<T, Task>? editFunc = null) where T : class
    {
        var tab = new TabPage(title);
        tab.BackColor = _bgColor;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            BackgroundColor = _cardColor,
            BorderStyle = BorderStyle.None
        };

        var toolPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(10),
            BackColor = Color.Transparent
        };

        var btnRefresh = CreateStyledButton("🔄 Обновить", _primaryColor);
        btnRefresh.Click += async (s, e) => await LoadTabData(grid, loadFunc);
        toolPanel.Controls.Add(btnRefresh);

        if (addFunc != null && _currentUser.Role == "admin")
        {
            var btnAdd = CreateStyledButton("➕ Добавить", _primaryColor);
            btnAdd.Click += async (s, e) => { await addFunc(); await LoadTabData(grid, loadFunc); };
            toolPanel.Controls.Add(btnAdd);
        }

        if (editFunc != null && _currentUser.Role == "admin")
        {
            var btnEdit = CreateStyledButton("✏️ Редактировать", _primaryColor, 160);
            btnEdit.Click += async (s, e) =>
            {
                if (grid.CurrentRow?.DataBoundItem is T item)
                {
                    await editFunc(item);
                    await LoadTabData(grid, loadFunc);
                }
            };
            toolPanel.Controls.Add(btnEdit);
        }

        if (title == "Сеансы")
        {
            var btnBuy = CreateStyledButton("🎟️ Купить билет", Color.LimeGreen, 160);
            btnBuy.Click += async (s, e) => await OpenTicketPurchase(grid);
            toolPanel.Controls.Add(btnBuy);
        }

        if (title == "Личный кабинет")
        {
            var btnLogout = CreateStyledButton("🚪 Выйти", _dangerColor);
            btnLogout.Click += (s, e) =>
            {
                IsLogout = true;
                this.Close();
            };
            toolPanel.Controls.Add(btnLogout);
        }

        tab.Controls.Add(grid);
        tab.Controls.Add(toolPanel);
        tab.Tag = new Func<Task>(async () => await LoadTabData(grid, loadFunc));

        _tabControl.TabPages.Add(tab);
    }

    private async Task LoadTabData<T>(DataGridView grid, Func<Task<List<T>>> loadFunc) where T : class
    {
        try
        {
            var data = await loadFunc();
            WinFormsHelper.BindToDataGrid(grid, data);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadCurrentTab()
    {
        if (_tabControl.SelectedTab?.Tag is Func<Task> loader)
        {
            await loader();
        }
    }


    private async Task AddCinema() => await AddEntity(new Cinema(), _cinemaService.AddAsync, new List<string> { "AverageRating" });
    private async Task EditCinema(Cinema c) => await EditEntity(c, _cinemaService.UpdateAsync, new List<string> { "AverageRating" });

    private async Task AddMovie() => await AddEntity(new Movie(), _movieService.AddAsync, new List<string> { "AverageRating" });
    private async Task EditMovie(Movie m) => await EditEntity(m, _movieService.UpdateAsync, new List<string> { "AverageRating" });

    private async Task AddEntity<T>(T entity, Func<T, Task> saveAction, List<string>? excludeProperties = null) where T : class, new()
    {
        var result = WinFormsHelper.ShowEditForm(entity, $"Добавление {typeof(T).Name}", excludeProperties: excludeProperties);
        if (result != null)
        {
            await saveAction(result);
            MessageBox.Show("✅ Успешно добавлено!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task EditEntity<T>(T entity, Func<T, Task> saveAction, List<string>? excludeProperties = null) where T : class, new()
    {
        var result = WinFormsHelper.ShowEditForm(entity, $"Редактирование {typeof(T).Name}", excludeProperties: excludeProperties);
        if (result != null)
        {
            await saveAction(result);
            MessageBox.Show("✅ Успешно сохранено!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task OpenTicketPurchase(DataGridView grid)
    {
        if (grid.CurrentRow?.DataBoundItem is SessionDetails session)
        {
            using var purchaseForm = new TicketPurchaseForm(
                _ticketService, _hallService, _sessionService,
                session.Id,
                $"{session.CinemaName} • Зал {session.HallNumber} • {session.MovieTitle}\n{session.StartTime:dd.MM.yyyy HH:mm} • {session.Price} ₽",
                _currentUser.Id, session.Price
            );
            purchaseForm.ShowDialog();
            await LoadCurrentTab();
        }
        else
        {
            MessageBox.Show("Сначала выберите сеанс в таблице!", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }


    private async void CreateCinemaTab()
    {
        var tab = new TabPage("Кинотеатры");
        tab.BackColor = _bgColor;

        var toolPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(10),
            BackColor = Color.Transparent
        };

        var cinemasPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10)
        };

        var btnRefresh = CreateStyledButton("🔄 Обновить", _primaryColor);
        btnRefresh.Click += async (s, e) => await LoadCinemaCards(cinemasPanel);
        toolPanel.Controls.Add(btnRefresh);

        if (_currentUser.Role == "admin")
        {
            var btnAdd = CreateStyledButton("➕ Добавить", _primaryColor);
            btnAdd.Click += async (s, e) => { await AddCinema(); await LoadCinemaCards(cinemasPanel); };
            toolPanel.Controls.Add(btnAdd);

            var btnEdit = CreateStyledButton("✏️ Редактировать", _primaryColor, 160);
            btnEdit.Click += async (s, e) =>
            {
                if (_selectedCinema != null) { await EditCinema(_selectedCinema); await LoadCinemaCards(cinemasPanel); }
                else MessageBox.Show("Выберите кинотеатр.");
            };
            toolPanel.Controls.Add(btnEdit);

            var btnDelete = CreateStyledButton("🗑️ Удалить", _dangerColor);
            btnDelete.Click += async (s, e) =>
            {
                if (_selectedCinema != null)
                {
                    if (MessageBox.Show($"Удалить кинотеатр «{_selectedCinema.Name}»?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        await _cinemaService.DeleteAsync(_selectedCinema.Id);
                        await LoadCinemaCards(cinemasPanel);
                    }
                }
                else MessageBox.Show("Выберите кинотеатр.");
            };
            toolPanel.Controls.Add(btnDelete);
        }

        tab.Controls.Add(cinemasPanel);
        tab.Controls.Add(toolPanel);
        tab.Tag = new Func<Task>(async () => await LoadCinemaCards(cinemasPanel));
        _tabControl.TabPages.Add(tab);
    }

    private async Task LoadCinemaCards(FlowLayoutPanel panel)
    {
        _selectedCinema = null;
        _selectedCinemaCard = null;

        ClearPanelAndDisposeControls(panel);
        var cinemas = await _cinemaService.GetAllByCityAsync(_currentCity);

        foreach (var cinema in cinemas)
        {
            var card = new Panel
            {
                Width = 450,
                Height = 120,
                Margin = new Padding(10),
                BackColor = _cardColor,
                BorderStyle = BorderStyle.None,
                Tag = cinema
            };

            var lblName = new Label
            {
                Text = cinema.Name,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(15, 15),
                AutoSize = true
            };

            var lblAddress = new Label
            {
                Text = cinema.Address,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DimGray,
                Location = new Point(15, 40),
                AutoSize = true
            };

            var btnDetails = CreateStyledButton("ПОКАЗАТЬ СЕАНСЫ", _primaryColor, 150);
            btnDetails.Height = 35;
            btnDetails.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            btnDetails.Location = new Point(15, 70);

            var ratingStars = new StarRatingControl
            {
                Value = cinema.AverageRating,
                ReadOnly = true,
                Size = new Size(100, 20),
                Location = new Point(card.Width - 160, 15)
            };

            var ratingLabel = new Label
            {
                Text = cinema.AverageRating.ToString("F1"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkGray,
                Location = new Point(card.Width - 50, 15),
                AutoSize = true
            };

            card.Controls.Add(lblName);
            card.Controls.Add(lblAddress);
            card.Controls.Add(btnDetails);
            card.Controls.Add(ratingStars);
            card.Controls.Add(ratingLabel);

            Action openDetails = async () =>
            {
                using var detailForm = new CinemaDetailForm(
                    _cinemaService, _hallService, _movieService,
                    _sessionService, _ticketService, new ReviewService(_db),
                    _currentUser, cinema.Id);
                detailForm.ShowDialog();
                await LoadCinemaCards(panel);
            };

            btnDetails.Click += (s, e) => openDetails();

            var clickableControls = new Control[] { lblName, lblAddress, ratingStars, ratingLabel, card };
            foreach (var ctrl in clickableControls)
            {
                ctrl.Click += (s, e) => SelectCinemaCard(card, cinema);
                ctrl.DoubleClick += (s, e) => openDetails();
            }

            panel.Controls.Add(card);
        }
    }


    private async void CreateMovieTab()
    {
        var tab = new TabPage("Фильмы");
        tab.BackColor = _bgColor;

        var toolPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(10),
            BackColor = Color.Transparent
        };

        var moviesPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10)
        };

        var btnRefresh = CreateStyledButton("🔄 Обновить", _primaryColor);
        btnRefresh.Click += async (s, e) => await LoadMovieCards(moviesPanel);
        toolPanel.Controls.Add(btnRefresh);

        if (_currentUser.Role == "admin")
        {
            var btnAdd = CreateStyledButton("➕ Добавить", _primaryColor);
            btnAdd.Click += async (s, e) => { await AddMovie(); await LoadMovieCards(moviesPanel); };
            toolPanel.Controls.Add(btnAdd);

            var btnEdit = CreateStyledButton("✏️ Редактировать", _primaryColor, 160);
            btnEdit.Click += async (s, e) =>
            {
                if (_selectedMovie != null) { await EditMovie(_selectedMovie); await LoadMovieCards(moviesPanel); }
                else MessageBox.Show("Выберите фильм.");
            };
            toolPanel.Controls.Add(btnEdit);

            var btnDelete = CreateStyledButton("🗑️ Удалить", _dangerColor);
            btnDelete.Click += async (s, e) =>
            {
                if (_selectedMovie != null)
                {
                    if (MessageBox.Show($"Удалить фильм «{_selectedMovie.Title}»?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        await _movieService.DeleteAsync(_selectedMovie.Id);
                        await LoadMovieCards(moviesPanel);
                    }
                }
                else MessageBox.Show("Выберите фильм.");
            };
            toolPanel.Controls.Add(btnDelete);
        }

        tab.Controls.Add(moviesPanel);
        tab.Controls.Add(toolPanel);
        tab.Tag = new Func<Task>(async () => await LoadMovieCards(moviesPanel));
        _tabControl.TabPages.Add(tab);
    }

    private async Task LoadMovieCards(FlowLayoutPanel panel)
    {
        ClearPanelAndDisposeControls(panel);
        var movies = await _movieService.GetAllAsync();

        foreach (var movie in movies)
        {
            var card = new Panel
            {
                Width = 200,
                Height = 360,
                Margin = new Padding(10),
                BackColor = _cardColor,
                BorderStyle = BorderStyle.None,
                Tag = movie
            };

            PictureBox posterBox = new PictureBox
            {
                Size = new Size(180, 250),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            if (!string.IsNullOrEmpty(movie.PosterUrl))
            {
                try { posterBox.LoadAsync(movie.PosterUrl); }
                catch { posterBox.Image = _noPosterCache; }
            }
            else
            {
                posterBox.Image = _noPosterCache;
            }

            var titleLabel = new Label
            {
                Text = movie.Title,
                Location = new Point(10, 265),
                Width = 180,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoEllipsis = true
            };

            var genreLabel = new Label
            {
                Text = movie.Genre ?? "",
                Location = new Point(10, 285),
                Width = 180,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DimGray
            };

            var ratingStars = new StarRatingControl
            {
                Value = movie.AverageRating,
                ReadOnly = true,
                Size = new Size(110, 20),
                Location = new Point(10, 303)
            };

            var btnDetails = CreateStyledButton("ПОДРОБНЕЕ", _primaryColor, 180);
            btnDetails.Height = 28;
            btnDetails.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            btnDetails.Location = new Point(10, 325);

            card.Controls.Add(posterBox);
            card.Controls.Add(titleLabel);
            card.Controls.Add(genreLabel);
            card.Controls.Add(ratingStars);
            card.Controls.Add(btnDetails);

            Action openDetails = async () =>
            {
                using var detailForm = new MovieDetailForm(
                    movie.Id, _movieService, new ReviewService(_db),
                    _sessionService, _cinemaService, _currentUser,
                    _hallService, _ticketService, _currentCity);
                detailForm.ShowDialog();
                await LoadMovieCards(panel);
            };

            btnDetails.Click += (s, e) => openDetails();

            var clickableControls = new Control[] { posterBox, titleLabel, genreLabel, ratingStars, card };
            foreach (var ctrl in clickableControls)
            {
                ctrl.Click += (s, e) => SelectMovieCard(card, movie);
                ctrl.DoubleClick += (s, e) => openDetails();
            }

            panel.Controls.Add(card);
        }
    }
}