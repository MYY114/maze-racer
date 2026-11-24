using System.Drawing;
using System.Text.RegularExpressions;

namespace MazeRacer
{
    public sealed class ValidationResult
    {
        public bool Valid { get; init; }
        public string Message { get; init; }
        public List<Point> Path { get; init; } = new();
    }

    public static class SolutionValidator
    {
        private static readonly Regex NumberRegex = new(@"-?\d+", RegexOptions.Compiled);

        /// <summary>读取解：每行 "x y" 或 "x,y"，坐标为迷宫字符网格坐标（0 起）。</summary>
        public static List<Point> ReadSolution(string file)
        {
            var text = File.ReadAllText(file);
            var path = new List<Point>();
            foreach (var raw in text.Replace("\r", string.Empty).Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var nums = NumberRegex.Matches(line);
                if (nums.Count < 2) continue;
                path.Add(new Point(int.Parse(nums[0].Value), int.Parse(nums[1].Value)));
            }
            return path;
        }

        public static ValidationResult Validate(Maze maze, List<Point> path)
        {
            if (path.Count == 0)
                return new ValidationResult { Valid = false, Message = "解为空", Path = path };

            if (path[0] != maze.Start)
                return new ValidationResult { Valid = false, Message = $"起点不对：({path[0].X},{path[0].Y})，应为 ({maze.Start.X},{maze.Start.Y})", Path = path };

            if (path[^1] != maze.End)
                return new ValidationResult { Valid = false, Message = $"终点不对：({path[^1].X},{path[^1].Y})，应为 ({maze.End.X},{maze.End.Y})", Path = path };

            for (var i = 0; i < path.Count; i++)
            {
                var p = path[i];
                if (!maze.IsWalkable(p.X, p.Y))
                    return new ValidationResult { Valid = false, Message = $"第 {i + 1} 步 ({p.X},{p.Y}) 是墙或越界", Path = path };

                if (i == 0) continue;
                var prev = path[i - 1];
                var manhattan = Math.Abs(p.X - prev.X) + Math.Abs(p.Y - prev.Y);
                if (manhattan != 1)
                    return new ValidationResult { Valid = false, Message = $"第 {i + 1} 步与上一步不相邻（跳了 {manhattan} 格）", Path = path };
            }

            return new ValidationResult { Valid = true, Message = $"路径合法，{path.Count} 步", Path = path };
        }
    }
}
