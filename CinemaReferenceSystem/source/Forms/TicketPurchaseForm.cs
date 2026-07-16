using CinemaReferenceSystem.Services;
using System.Drawing.Drawing2D;

namespace CinemaReferenceSystem.Forms;

public partial class TicketPurchaseForm : Form
{
    private readonly TicketService _ticketService;
    private readonly HallService _hallService;
    private readonly SessionService _sessionService;
    private readonly int _sessionId;
    private readonly int _userId;
    private readonly string _sessionInfo;
    private readonly decimal _price;

    private List<(int Row, int Seat)> allSeats;
    private HashSet<(int, int)> selectedSeats = new();

    private Panel viewportPanel;
    private DoubleBufferedPanel canvasPanel;
    private Label totalLabel;
    private Button buyButton;

    private List<SeatControlData> seatControls = new();
    private List<RowLabelData> rowLabels = new();

    private int _hallRows;
    private int _hallMaxSeatsPerRow;

    private float zoom = 1.0f;
    private bool isDragging = false;
    private Point lastMousePos;

    public TicketPurchaseForm(TicketService ticketService, HallService hallService, SessionService sessionService,
                              int sessionId, string sessionInfo, int userId, decimal price)
    {
        _ticketService = ticketService;
        _hallService = hallService;
        _sessionService = sessionService;
        _sessionId = sessionId;
        _sessionInfo = sessionInfo;
        _userId = userId;
        _price = price;
        allSeats = new List<(int, int)>();

        InitializeComponent();
        _ = LoadSeatsAsync();
    }

    private void InitializeComponent()
    {
        this.Text = $"Покупка билетов — {_sessionInfo}";
        this.Size = new Size(1100, 750);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.White;

        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(240, 240, 240) };

        var sessionInfoLabel = new Label
        {
            Text = _sessionInfo,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.DimGray,
            Location = new Point(20, 10),
            Size = new Size(700, 40),
            AutoEllipsis = true
        };

        totalLabel = new Label
        {
            Text = "0 ₽",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Location = new Point(750, 15),
            AutoSize = true
        };

        buyButton = new Button
        {
            Text = "Купить выбранные",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.DodgerBlue,
            ForeColor = Color.White,
            Size = new Size(180, 40),
            Location = new Point(850, 10),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        buyButton.FlatAppearance.BorderSize = 0;
        buyButton.Click += BuyButton_Click;

        bottomPanel.Controls.Add(sessionInfoLabel);
        bottomPanel.Controls.Add(totalLabel);
        bottomPanel.Controls.Add(buyButton);

        viewportPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 245, 245)
        };
        SetupPanning(viewportPanel);

        canvasPanel = new DoubleBufferedPanel
        {
            BackColor = Color.FromArgb(245, 245, 245),
            Location = new Point(0, 0)
        };
        canvasPanel.Paint += CanvasPanel_Paint;
        SetupPanning(canvasPanel);

        var btnZoomIn = new Button
        {
            Text = "+",
            Font = new Font("Segoe UI", 18, FontStyle.Regular),
            Size = new Size(50, 50),
            Location = new Point(5, 5),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        var btnZoomOut = new Button
        {
            Text = "—",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Size = new Size(50, 50),
            Location = new Point(5, 60),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        GraphicsPath circlePath = new GraphicsPath();
        circlePath.AddEllipse(0, 0, 50, 50);
        btnZoomIn.Region = new Region(circlePath);
        btnZoomOut.Region = new Region(circlePath);

        btnZoomIn.FlatAppearance.BorderSize = 0;
        btnZoomOut.FlatAppearance.BorderSize = 0;

        btnZoomIn.Click += (s, e) => Zoom(0.2f);
        btnZoomOut.Click += (s, e) => Zoom(-0.2f);

        var zoomPanel = new Panel
        {
            Size = new Size(60, 120),
            Location = new Point(this.ClientSize.Width - 80, 50),
            BackColor = Color.Transparent
        };
        zoomPanel.Controls.Add(btnZoomIn);
        zoomPanel.Controls.Add(btnZoomOut);

        this.Controls.Add(zoomPanel);
        zoomPanel.BringToFront();

        viewportPanel.Controls.Add(zoomPanel);
        viewportPanel.Controls.Add(canvasPanel);

        this.Controls.Add(viewportPanel);
        this.Controls.Add(bottomPanel);
    }

    private async Task LoadSeatsAsync()
    {
        try
        {
            int hallId = await _sessionService.GetHallIdBySessionIdAsync(_sessionId);
            var hall = await _hallService.GetByIdAsync(hallId);
            if (hall == null)
            {
                MessageBox.Show("Зал не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _hallRows = hall.RowsCount;
            _hallMaxSeatsPerRow = hall.MaxSeatsPerRow;

            allSeats = await _hallService.GetSeatsAsync(hallId);

            var soldTickets = await _ticketService.GetBySessionIdAsync(_sessionId);
            var soldSet = soldTickets.Where(t => t.IsSold)
                                     .Select(t => (t.RowNum, t.SeatNum))
                                     .ToHashSet();

            canvasPanel.Controls.Clear();
            seatControls.Clear();
            rowLabels.Clear();

            for (int r = 1; r <= _hallRows; r++)
            {
                var realSeatsInRow = allSeats.Where(s => s.Row == r).Select(s => s.Seat).OrderBy(s => s).ToList();
                var displayNumberMap = new Dictionary<int, int>();
                for (int idx = 0; idx < realSeatsInRow.Count; idx++)
                    displayNumberMap[realSeatsInRow[idx]] = idx + 1;

                var leftLbl = new Label { Text = r.ToString(), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.DimGray };
                var rightLbl = new Label { Text = r.ToString(), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.DimGray };
                rowLabels.Add(new RowLabelData { Row = r, LeftLabel = leftLbl, RightLabel = rightLbl });
                canvasPanel.Controls.Add(leftLbl);
                canvasPanel.Controls.Add(rightLbl);

                for (int s = 1; s <= _hallMaxSeatsPerRow; s++)
                {
                    if (displayNumberMap.ContainsKey(s))
                    {
                        int displayNum = displayNumberMap[s];
                        var seatBtn = new Button
                        {
                            FlatStyle = FlatStyle.Flat,
                            Text = displayNum.ToString(),
                            Tag = (r, s),
                            Cursor = Cursors.Hand
                        };
                        seatBtn.FlatAppearance.BorderSize = 0;

                        bool sold = soldSet.Contains((r, s));
                        if (sold)
                        {
                            seatBtn.BackColor = Color.FromArgb(220, 53, 69);
                            seatBtn.ForeColor = Color.White;
                            seatBtn.Enabled = false;
                        }
                        else
                        {
                            seatBtn.BackColor = Color.FromArgb(40, 167, 69);
                            seatBtn.ForeColor = Color.White;
                            seatBtn.Click += SeatBtn_Click;
                        }

                        SetupPanning(seatBtn);
                        seatControls.Add(new SeatControlData { Row = r, Col = s, Btn = seatBtn });
                        canvasPanel.Controls.Add(seatBtn);
                    }
                }
            }
            ApplyLayout();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки мест: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyLayout()
    {
        if (_hallRows == 0) return;

        int seatSize = (int)(32 * zoom);
        int gap = (int)(8 * zoom);
        int labelWidth = (int)(40 * zoom);
        int screenHeight = (int)(140 * zoom); 

        int totalWidth = (labelWidth * 2) + (_hallMaxSeatsPerRow * (seatSize + gap));
        int totalHeight = screenHeight + (_hallRows * (seatSize + gap)) + (20 * zoom > 0 ? (int)(20 * zoom) : 20);

        canvasPanel.Size = new Size(totalWidth, totalHeight);
        Font scaledFont = new Font("Segoe UI", Math.Max(6, 9 * zoom), FontStyle.Bold);

        foreach (var rowData in rowLabels)
        {
            int y = screenHeight + (rowData.Row - 1) * (seatSize + gap);
            rowData.LeftLabel.Bounds = new Rectangle(0, y, labelWidth, seatSize);
            rowData.RightLabel.Bounds = new Rectangle(totalWidth - labelWidth, y, labelWidth, seatSize);
            rowData.LeftLabel.Font = scaledFont;
            rowData.RightLabel.Font = scaledFont;
        }

        foreach (var seatData in seatControls)
        {
            int x = labelWidth + (seatData.Col - 1) * (seatSize + gap);
            int y = screenHeight + (seatData.Row - 1) * (seatSize + gap);

            seatData.Btn.Bounds = new Rectangle(x, y, seatSize, seatSize);
            seatData.Btn.Font = scaledFont;

            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, seatSize, seatSize);
            seatData.Btn.Region = new Region(path);
        }

        CenterCanvas();
        canvasPanel.Invalidate();
    }

    private void Zoom(float delta)
    {
        float oldZoom = zoom;
        zoom += delta;

        if (zoom < 0.5f) zoom = 0.5f;
        if (zoom > 2.5f) zoom = 2.5f;
        if (zoom == oldZoom) return;

        int centerX = viewportPanel.Width / 2;
        int centerY = viewportPanel.Height / 2;
        float ratio = zoom / oldZoom;
        int dx = centerX - canvasPanel.Left;
        int dy = centerY - canvasPanel.Top;

        ApplyLayout();

        canvasPanel.Left = centerX - (int)(dx * ratio);
        canvasPanel.Top = centerY - (int)(dy * ratio);
        CenterCanvas();
    }

    private void CenterCanvas()
    {
        if (canvasPanel.Width <= viewportPanel.Width)
            canvasPanel.Left = (viewportPanel.Width - canvasPanel.Width) / 2;
        if (canvasPanel.Height <= viewportPanel.Height)
            canvasPanel.Top = (viewportPanel.Height - canvasPanel.Height) / 2;
    }

    private void ClampCanvasPosition()
    {
        if (canvasPanel.Width <= viewportPanel.Width)
        {
            canvasPanel.Left = (viewportPanel.Width - canvasPanel.Width) / 2;
        }
        else
        {
            if (canvasPanel.Left > 0) canvasPanel.Left = 0;
            if (canvasPanel.Left < viewportPanel.Width - canvasPanel.Width)
                canvasPanel.Left = viewportPanel.Width - canvasPanel.Width;
        }

        if (canvasPanel.Height <= viewportPanel.Height)
        {
            canvasPanel.Top = (viewportPanel.Height - canvasPanel.Height) / 2;
        }
        else
        {
            if (canvasPanel.Top > 0) canvasPanel.Top = 0;
            if (canvasPanel.Top < viewportPanel.Height - canvasPanel.Height)
                canvasPanel.Top = viewportPanel.Height - canvasPanel.Height;
        }
    }

    private void CanvasPanel_Paint(object sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int arcY = (int)(40 * zoom);
        int arcWidth = canvasPanel.Width - (int)(100 * zoom);
        int arcHeight = (int)(60 * zoom);
        int arcX = (int)(50 * zoom);

        using (Pen p = new Pen(Color.DarkGray, 2 * zoom))
        {
            if (arcWidth > 0 && arcHeight > 0)
                g.DrawArc(p, arcX, arcY, arcWidth, arcHeight, 180, 180);
        }

        using (Font f = new Font("Segoe UI", Math.Max(8, 16 * zoom), FontStyle.Bold))
        {
            string text = "ЭКРАН";
            SizeF size = g.MeasureString(text, f);
            g.DrawString(text, f, Brushes.Gray, (canvasPanel.Width - size.Width) / 2, arcY - size.Height + (10 * zoom));
        }
    }

    private void SetupPanning(Control ctrl)
    {
        ctrl.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Right)
            {
                isDragging = true;
                lastMousePos = Cursor.Position;
            }
        };
        ctrl.MouseMove += (s, e) => {
            if (isDragging)
            {
                Point currentPos = Cursor.Position;
                canvasPanel.Left += currentPos.X - lastMousePos.X;
                canvasPanel.Top += currentPos.Y - lastMousePos.Y;
                ClampCanvasPosition();
                lastMousePos = currentPos;
            }
        };
        ctrl.MouseUp += (s, e) => {
            if (e.Button == MouseButtons.Right) isDragging = false;
        };
    }

    private void SeatBtn_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is ValueTuple<int, int> seat)
        {
            if (selectedSeats.Contains(seat))
            {
                selectedSeats.Remove(seat);
                btn.BackColor = Color.FromArgb(40, 167, 69);
                btn.ForeColor = Color.White;
            }
            else
            {
                selectedSeats.Add(seat);
                btn.BackColor = Color.FromArgb(0, 123, 255);
                btn.ForeColor = Color.White;
            }
            UpdateTotal();
        }
    }

    private void UpdateTotal()
    {
        decimal total = selectedSeats.Count * _price;
        totalLabel.Text = $"{total} ₽";
        buyButton.Text = selectedSeats.Count > 0 ? $"Купить ({selectedSeats.Count})" : "Купить выбранные";
    }

    private async void BuyButton_Click(object? sender, EventArgs e)
    {
        if (selectedSeats.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы одно место.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Купить {selectedSeats.Count} билет(ов) на сумму {selectedSeats.Count * _price} ₽?",
                                      "Подтверждение", MessageBoxButtons.YesNo);

        if (confirm != DialogResult.Yes) return;

        bool anyFailed = false;
        foreach (var (row, seat) in selectedSeats)
        {
            bool success = await _ticketService.SellTicketAsync(_sessionId, row, seat, _userId);
            if (!success) anyFailed = true;
        }

        if (anyFailed)
            MessageBox.Show("Не все билеты удалось купить (возможно, места уже заняты).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else
            MessageBox.Show("✅ Все выбранные билеты успешно куплены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private class SeatControlData { public int Row { get; set; } public int Col { get; set; } public Button Btn { get; set; } }
    private class RowLabelData { public int Row { get; set; } public Label LeftLabel { get; set; } public Label RightLabel { get; set; } }

    private class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}