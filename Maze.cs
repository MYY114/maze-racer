using System.Drawing;
using System.Text;

namespace MazeRacer
{
    /// <summary>字符网格迷宫：2*Cols+1 × 2*Rows+1，'#' 墙、' ' 通路、S 起点、E 终点。</summary>
    public sealed class Maze
    {
        public int Cols { get; }
        public int Rows { get; }
        public char[,] Grid { get; }
        public int Width => Cols * 2 + 1;
        public int Height => Rows * 2 + 1;
        public Point Start => new(1, 1);
        public Point End => new(Cols * 2 - 1, Rows * 2 - 1);

        public Maze(int cols, int rows)
        {
            if (cols < 1) cols = 1;
            if (rows < 1) rows = 1;
            Cols = cols;
            Rows = rows;
            Grid = new char[Width, Height];
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                    Grid[x, y] = '#';
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsWall(int x, int y) => !InBounds(x, y) || Grid[x, y] == '#';

        public bool IsWalkable(int x, int y) => InBounds(x, y) && Grid[x, y] != '#';

        public string ToAscii()
        {
            var sb = new StringBuilder((Width + 2) * Height);
            sb.AppendLine($"{Cols} {Rows}");
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (x == Start.X && y == Start.Y) sb.Append('S');
                    else if (x == End.X && y == End.Y) sb.Append('E');
                    else sb.Append(Grid[x, y]);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static Maze Parse(string text)
        {
            var lines = text.Replace("\r", string.Empty).Split('\n');
            var idx = 0;
            while (idx < lines.Length && string.IsNullOrWhiteSpace(lines[idx])) idx++;
            if (idx >= lines.Length) throw new FormatException("迷宫文件为空");

            var head = lines[idx].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (head.Length < 2) throw new FormatException("第一行应为：列 行");
            var cols = int.Parse(head[0]);
            var rows = int.Parse(head[1]);
            var maze = new Maze(cols, rows);

            var expected = maze.Height;
            var startReading = idx + 1;
            for (var y = 0; y < expected; y++)
            {
                if (startReading + y >= lines.Length) throw new FormatException("迷宫行数不足");
                var line = lines[startReading + y].TrimEnd('\r');
                if (line.Length < maze.Width) throw new FormatException($"第 {y + 1} 行长度不足（应为 {maze.Width}）");
                for (var x = 0; x < maze.Width; x++)
                {
                    var c = line[x];
                    maze.Grid[x, y] = c == '#' ? '#' : ' ';
                }
            }
            return maze;
        }

        public void SaveAscii(string path) => File.WriteAllText(path, ToAscii(), new UTF8Encoding(false));
    }
}
