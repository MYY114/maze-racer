using System.Drawing;
using System.Drawing.Drawing2D;

namespace MazeRacer
{
    /// <summary>迷宫预览：墙/通路/起点终点，可叠加某脚本的解法路径。</summary>
    public sealed class MazePanel : Control
    {
        private Maze _maze;
        private List<Point> _path = new();
        private string _caption = "尚未生成迷宫";

        public MazePanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(250, 251, 253);
            ResizeRedraw = true;
        }

        public void SetMaze(Maze maze, string caption)
        {
            _maze = maze;
            _caption = caption ?? string.Empty;
            _path = new List<Point>();
            Invalidate();
        }

        public void SetSolution(List<Point> path)
        {
            _path = path ?? new List<Point>();
            Invalidate();
        }

        public void SetCaption(string caption)
        {
            _caption = caption;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (_maze == null)
            {
                using var font = new Font("Microsoft YaHei UI", 10f);
                g.DrawString("生成迷宫后，这里会显示预览", font, Brushes.Gray, 16, 16);
                return;
            }

            const int pad = 14;
            var availW = Width - pad * 2;
            var availH = Height - pad * 2 - 24;
            var cell = Math.Max(1, Math.Min((float)availW / _maze.Width, (float)availH / _maze.Height));
            var drawW = cell * _maze.Width;
            var drawH = cell * _maze.Height;
            var ox = (Width - drawW) / 2f;
            var oy = pad + 18 + (availH - drawH) / 2f;

            using (var wallBrush = new SolidBrush(Color.FromArgb(52, 58, 70)))
            using (var openBrush = new SolidBrush(Color.White))
            using (var startBrush = new SolidBrush(Color.FromArgb(52, 199, 89)))
            using (var endBrush = new SolidBrush(Color.FromArgb(255, 59, 48)))
            {
                for (var y = 0; y < _maze.Height; y++)
                {
                    for (var x = 0; x < _maze.Width; x++)
                    {
                        Brush brush;
                        if (x == _maze.Start.X && y == _maze.Start.Y) brush = startBrush;
                        else if (x == _maze.End.X && y == _maze.End.Y) brush = endBrush;
                        else if (_maze.Grid[x, y] == '#') brush = wallBrush;
                        else brush = openBrush;

                        g.FillRectangle(brush, ox + x * cell, oy + y * cell, cell + 0.5f, cell + 0.5f);
                    }
                }
            }

            if (_path.Count > 0)
            {
                using var linePen = new Pen(Color.FromArgb(47, 107, 255), Math.Max(1.5f, Math.Min(cell * 0.9f, 5f)));
                linePen.LineJoin = LineJoin.Round;
                var points = _path.Select(p => new PointF(
                    ox + p.X * cell + cell / 2f,
                    oy + p.Y * cell + cell / 2f)).ToArray();

                if (points.Length > 1)
                    g.DrawLines(linePen, points);
                else if (points.Length == 1)
                    g.DrawEllipse(linePen, points[0].X - 3, points[0].Y - 3, 6, 6);
            }

            using (var titleFont = new Font("Microsoft YaHei UI", 8.5f))
            {
                g.DrawString(_caption, titleFont, Brushes.DimGray, pad, 4);

                var legend = "■ 墙    □ 通路    ─ 解法路径";
                var legendSize = g.MeasureString(legend, titleFont);
                g.DrawString(legend, titleFont, Brushes.DimGray, Width - legendSize.Width - pad, Height - legendSize.Height - 2);
            }

            using var borderPen = new Pen(Color.FromArgb(225, 228, 234));
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        }
    }
}
