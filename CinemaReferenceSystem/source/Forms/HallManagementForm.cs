using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;
using CinemaReferenceSystem.Utils;

namespace CinemaReferenceSystem.Forms;

public partial class HallManagementForm : Form
{
    private readonly int _cinemaId;
    private readonly HallService _hallService;
    private readonly TicketService _ticketService;
    private readonly SessionService _sessionService;
    private DataGridView _grid;

    public HallManagementForm(int cinemaId, HallService hallService,
                              TicketService ticketService, SessionService sessionService)
    {
        _cinemaId = cinemaId;
        _hallService = hallService;
        _ticketService = ticketService;
        _sessionService = sessionService;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = "Управление залами кинотеатра";
        Size = new Size(850, 600);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            BackgroundColor = Color.White, 
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false, 
            EnableHeadersVisualStyles = false,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(230, 230, 230)
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(245, 245, 250),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            SelectionBackColor = Color.FromArgb(245, 245, 250),
            Padding = new Padding(10, 5, 10, 5)
        };
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        _grid.ColumnHeadersHeight = 40;

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = Color.FromArgb(0, 120, 215),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5),
            Font = new Font("Segoe UI", 10)
        };
        _grid.RowTemplate.Height = 35;

        var toolPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(15, 10, 15, 10),
            BackColor = Color.FromArgb(245, 245, 250),
            WrapContents = false
        };

        Button CreateFlatButton(string text, int width, Color backColor, Color foreColor)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 38,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foreColor,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        var btnRefresh = CreateFlatButton("🔄 Обновить", 120, Color.White, Color.Black);
        btnRefresh.FlatAppearance.BorderColor = Color.LightGray;
        btnRefresh.Click += async (s, e) => await LoadDataAsync();

        var btnAdd = CreateFlatButton("➕ Добавить зал", 150, Color.White, Color.Black);
        btnAdd.FlatAppearance.BorderColor = Color.LightGray;
        btnAdd.Click += async (s, e) => await AddHall();

        var btnEdit = CreateFlatButton("✏️ Редактировать", 150, Color.White, Color.Black);
        btnEdit.FlatAppearance.BorderColor = Color.LightGray;
        btnEdit.Click += async (s, e) => await EditHall();

        var btnDelete = CreateFlatButton("🗑️ Удалить", 120, Color.FromArgb(240, 80, 80), Color.White);
        btnDelete.FlatAppearance.BorderSize = 0;
        btnDelete.Click += async (s, e) => await DeleteHall();

        var btnSchema = CreateFlatButton("🔧 Схема мест", 140, Color.FromArgb(230, 240, 255), Color.FromArgb(0, 80, 160));
        btnSchema.FlatAppearance.BorderSize = 0;
        btnSchema.Click += async (s, e) => await OpenSchema();

        toolPanel.Controls.AddRange(new Control[] { btnRefresh, btnAdd, btnEdit, btnDelete, btnSchema });

        Controls.Add(_grid);
        Controls.Add(toolPanel);

        toolPanel.BringToFront();
        _grid.BringToFront();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var halls = await _hallService.GetByCinemaIdAsync(_cinemaId);
            var display = halls.Select(h => new
            {
                h.Id,
                Номер = h.HallNumber,
                Рядов = h.RowsCount,
                МаксМестВРяду = h.MaxSeatsPerRow
            }).ToList();

            _grid.DataSource = null;
            _grid.DataSource = display;

            if (_grid.Columns["Id"] != null)
                _grid.Columns["Id"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки залов: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AddHall()
    {
        var newHall = WinFormsHelper.ShowEditForm(
            entity: new Hall { CinemaId = _cinemaId },
            title: "Добавление зала",
            comboData: null,
            excludeProperties: new List<string> { "CinemaId" }
        );
        if (newHall == null) return;

        try
        {
            int hallId = await _hallService.AddAsync(newHall);

            using var editor = new SeatEditorForm(_hallService, hallId,
                                                  newHall.RowsCount, newHall.MaxSeatsPerRow, true);
            editor.ShowDialog();

            MessageBox.Show("✅ Зал создан и схема сохранена.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка добавления зала: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task EditHall()
    {
        if (_grid.CurrentRow?.DataBoundItem == null) return;

        int id = (int)_grid.CurrentRow.DataBoundItem.GetType().GetProperty("Id")!
                                 .GetValue(_grid.CurrentRow.DataBoundItem)!;

        var hall = await _hallService.GetByIdAsync(id);
        if (hall == null)
        {
            MessageBox.Show("Зал не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var updated = WinFormsHelper.ShowEditForm(
            entity: hall,
            title: "Редактирование зала",
            comboData: null,
            excludeProperties: new List<string> { "CinemaId" }
            );
        if (updated == null) return;

        try
        {
            await _hallService.UpdateAsync(updated);
            using var editor = new SeatEditorForm(_hallService, updated.Id,
                                                  updated.RowsCount, updated.MaxSeatsPerRow, false);
            editor.ShowDialog();

            MessageBox.Show("✅ Зал обновлён и схема сохранена.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка обновления зала: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteHall()
    {
        if (_grid.CurrentRow?.DataBoundItem == null) return;

        int id = (int)_grid.CurrentRow.DataBoundItem.GetType().GetProperty("Id")!
                                 .GetValue(_grid.CurrentRow.DataBoundItem)!;

        if (MessageBox.Show($"Удалить зал с ID = {id}? Все связанные сеансы и билеты будут удалены каскадно.",
            "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            await _hallService.DeleteAsync(id);
            MessageBox.Show("✅ Зал удалён.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenSchema()
    {
        if (_grid.CurrentRow?.DataBoundItem == null) return;

        int id = (int)_grid.CurrentRow.DataBoundItem.GetType().GetProperty("Id")!
                                 .GetValue(_grid.CurrentRow.DataBoundItem)!;

        var hall = await _hallService.GetByIdAsync(id);
        if (hall == null) return;

        using var editor = new SeatEditorForm(_hallService, hall.Id,
                                              hall.RowsCount, hall.MaxSeatsPerRow, false);
        editor.ShowDialog();
    }
}