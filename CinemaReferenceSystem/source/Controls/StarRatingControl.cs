using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CinemaReferenceSystem.Controls;

public class StarRatingControl : Control
{
    private double _value = 0;
    private double _hoverValue = -1;
    private bool _readOnly = false;
    private int _starCount = 5;
    private int _starSpacing = 4;
    private Color _emptyColor = Color.LightGray;
    private Color _filledColor = Color.Gold;

    public event EventHandler? ValueChanged;

    public StarRatingControl()
    {
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        Size = new Size(5 * 30 + 4 * 4, 30);
    }

    public double Value
    {
        get => _value;
        set { if (value >= 0 && value <= 10) { _value = value; Invalidate(); } }
    }

    public bool ReadOnly
    {
        get => _readOnly;
        set { _readOnly = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        double effectiveValue = _hoverValue >= 0 ? _hoverValue : _value;
        int starWidth = (Width - (_starCount - 1) * _starSpacing) / _starCount;
        int starHeight = Height;

        for (int i = 0; i < _starCount; i++)
        {
            double starScore = effectiveValue - i * 2.0;
            double fillPercent = Math.Max(0, Math.Min(1, starScore / 2.0));

            int x = i * (starWidth + _starSpacing);
            Rectangle starRect = new Rectangle(x, 0, starWidth, starHeight);

            DrawStar(e.Graphics, starRect, _emptyColor);

            if (fillPercent > 0)
            {
                int filledWidth = (int)(starRect.Width * fillPercent);
                if (filledWidth > 0)
                {
                    RectangleF clipRect = new RectangleF(starRect.Left, starRect.Top, filledWidth, starRect.Height);
                    e.Graphics.SetClip(clipRect);
                    DrawStar(e.Graphics, starRect, _filledColor);
                    e.Graphics.ResetClip();
                }
            }
        }
    }

    private void DrawStar(Graphics g, Rectangle rect, Color color)
    {
        PointF[] points = GetStarPoints(rect);
        using SolidBrush brush = new SolidBrush(color);
        g.FillPolygon(brush, points);
    }

    private PointF[] GetStarPoints(Rectangle rect)
    {
        float cx = rect.Left + rect.Width / 2f;
        float cy = rect.Top + rect.Height / 2f;
        float outerR = rect.Width / 2f - 1;
        float innerR = outerR * 0.4f;

        PointF[] pts = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            double angle = -Math.PI / 2 + i * Math.PI / 5;
            float r = (i % 2 == 0) ? outerR : innerR;
            pts[i] = new PointF(cx + (float)(r * Math.Cos(angle)), cy + (float)(r * Math.Sin(angle)));
        }
        return pts;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_readOnly) return;
        int starWidth = (Width - (_starCount - 1) * _starSpacing) / _starCount;
        int index = e.X / (starWidth + _starSpacing);
        if (index < 0) index = 0;
        if (index >= _starCount) index = _starCount - 1;

        int xInStar = e.X - index * (starWidth + _starSpacing);
        int add = (xInStar < starWidth / 2) ? 1 : 2;
        _hoverValue = index * 2 + add;
        if (_hoverValue > 10) _hoverValue = 10;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_readOnly) return;
        _hoverValue = -1;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (_readOnly) return;
        int starWidth = (Width - (_starCount - 1) * _starSpacing) / _starCount;
        int index = e.X / (starWidth + _starSpacing);
        if (index < 0) index = 0;
        if (index >= _starCount) index = _starCount - 1;

        int xInStar = e.X - index * (starWidth + _starSpacing);
        int add = (xInStar < starWidth / 2) ? 1 : 2;
        _value = index * 2 + add;
        if (_value > 10) _value = 10;
        _hoverValue = -1;
        ValueChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }
}