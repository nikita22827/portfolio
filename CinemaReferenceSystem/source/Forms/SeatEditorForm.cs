using CinemaReferenceSystem.Services;
using System.Drawing.Drawing2D;

namespace CinemaReferenceSystem.Forms;

public partial class SeatEditorForm : Form
{
    private readonly HallService _hallService;
    private readonly int _hallId;
    private readonly int _rows;
    private readonly int _maxSeats;
    private readonly bool _isNew;
    private HashSet<(int, int)> enabledSeats = new();

    private Panel viewportPanel;
    private DoubleBufferedPanel canvasPanel;
    private List<SeatControlData> seatControls = new();
    private List<RowLabelData> rowLabels = new();
    private Button btnSave;

    private float zoom = 1.0f;
    private bool isDragging = false;
    private Point lastMousePos;

    public SeatEditorForm(HallService hallService, int hallId, int rows, int maxSeats, bool isNew = false)
    {
        _hallService = hallService;
        _hallId = hallId;
        _rows = rows;
        _maxSeats = maxSeats;
        _isNew = isNew;
        InitializeComponent();
        _ = InitializeDataAsync();
        BuildSeats();
    }
    private async Task InitializeDataAsync()
    {
        await LoadExistingSeats();
        BuildSeats();
    }

    private async Task LoadExistingSeats()
    {
        if (_isNew)
        {
            for (int r = 1; r <= _rows; r++)
                for (int s = 1; s <= _maxSeats; s++)
                    enabledSeats.Add((r, s));
        }
        else
        {
            var seats = await _hallService.GetSeatsAsync(_hallId);
            foreach (var (r, s) in seats)
                enabledSeats.Add((r, s));
        }
    }

    private void InitializeComponent()
    {
        this.Text = "Схема зала";
        this.Size = new Size(1100, 750);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.White;

        var legendPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };

        var greenDot = new Label
        {
            Text = "●",
            ForeColor = Color.LimeGreen,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true
        };
        var greenLabel = new Label
        {
            Text = "Место будет в зале",
            Font = new Font("Segoe UI", 9),
            AutoSize = true
        };

        var grayDot = new Label
        {
            Text = "●",
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(20, 0, 0, 0)
        };
        var grayLabel = new Label
        {
            Text = "Место будет удалено из зала",
            Font = new Font("Segoe UI", 9),
            AutoSize = true
        };

        legendPanel.Controls.Add(greenDot);
        legendPanel.Controls.Add(greenLabel);
        legendPanel.Controls.Add(grayDot);
        legendPanel.Controls.Add(grayLabel);

        this.Controls.Add(legendPanel);

        btnSave = new Button
        {
            Text = "💾 Сохранить схему (оставить только зелёные места)",
            Dock = DockStyle.Bottom,
            Height = 50,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.LimeGreen,
            ForeColor = Color.White
        };
        btnSave.Click += BtnSave_Click;
        this.Controls.Add(btnSave);

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

        viewportPanel.Controls.Add(canvasPanel);
        this.Controls.Add(viewportPanel);

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
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        zoomPanel.Controls.Add(btnZoomIn);
        zoomPanel.Controls.Add(btnZoomOut);
        this.Controls.Add(zoomPanel);
        zoomPanel.BringToFront();
    }

    private void BuildSeats()
    {
        canvasPanel.Controls.Clear();
        seatControls.Clear();
        rowLabels.Clear();

        for (int r = 1; r <= _rows; r++)
        {
            var leftLbl = new Label
            {
                Text = r.ToString(),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };
            var rightLbl = new Label
            {
                Text = r.ToString(),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };
            rowLabels.Add(new RowLabelData { Row = r, LeftLabel = leftLbl, RightLabel = rightLbl });
            canvasPanel.Controls.Add(leftLbl);
            canvasPanel.Controls.Add(rightLbl);

            for (int s = 1; s <= _maxSeats; s++)
            {
                bool exists = enabledSeats.Contains((r, s));
                var seatBtn = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Text = s.ToString(),
                    Tag = (r, s),
                    Cursor = Cursors.Hand
                };
                seatBtn.FlatAppearance.BorderSize = 0;

                seatBtn.BackColor = exists ? Color.LimeGreen : Color.LightGray;
                seatBtn.ForeColor = exists ? Color.White : Color.Black;

                seatBtn.Click += (snd, e) =>
                {
                    if (snd is Button btn && btn.Tag is ValueTuple<int, int> seat)
                    {
                        if (enabledSeats.Contains(seat))
                        {
                            enabledSeats.Remove(seat);
                            btn.BackColor = Color.LightGray;
                            btn.ForeColor = Color.Black;
                        }
                        else
                        {
                            enabledSeats.Add(seat);
                            btn.BackColor = Color.LimeGreen;
                            btn.ForeColor = Color.White;
                        }
                    }
                };

                SetupPanning(seatBtn);
                seatControls.Add(new SeatControlData { Row = r, Col = s, Btn = seatBtn });
                canvasPanel.Controls.Add(seatBtn);
            }
        }
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (_rows == 0) return;

        int seatSize = (int)(32 * zoom);
        int gap = (int)(8 * zoom);
        int labelWidth = (int)(40 * zoom);
        int screenHeight = (int)(140 * zoom);

        int totalWidth = (labelWidth * 2) + (_maxSeats * (seatSize + gap));
        int totalHeight = screenHeight + (_rows * (seatSize + gap)) + (int)(20 * zoom);

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
            canvasPanel.Left = (viewportPanel.Width - canvasPanel.Width) / 2;
        else
        {
            if (canvasPanel.Left > 0) canvasPanel.Left = 0;
            if (canvasPanel.Left < viewportPanel.Width - canvasPanel.Width)
                canvasPanel.Left = viewportPanel.Width - canvasPanel.Width;
        }

        if (canvasPanel.Height <= viewportPanel.Height)
            canvasPanel.Top = (viewportPanel.Height - canvasPanel.Height) / 2;
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
            g.DrawString(text, f, Brushes.Gray, (canvasPanel.Width - size.Width) / 2,
                         arcY - size.Height + (10 * zoom));
        }
    }

    private void SetupPanning(Control ctrl)
    {
        ctrl.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                isDragging = true;
                lastMousePos = Cursor.Position;
            }
        };
        ctrl.MouseMove += (s, e) =>
        {
            if (isDragging)
            {
                Point currentPos = Cursor.Position;
                canvasPanel.Left += currentPos.X - lastMousePos.X;
                canvasPanel.Top += currentPos.Y - lastMousePos.Y;
                ClampCanvasPosition();
                lastMousePos = currentPos;
            }
        };
        ctrl.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Right) isDragging = false;
        };
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            await _hallService.UpdateSeatsAsync(_hallId, enabledSeats.ToList());
            MessageBox.Show("✅ Схема зала сохранена! Оставлены только зелёные места.",
                "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Ошибка сохранения: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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