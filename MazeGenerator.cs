using System.Drawing;

namespace MazeRacer
{
    public static class MazeGenerator
    {
        public const string AlgoDfs = "深度优先（完美迷宫）";
        public const string AlgoPrim = "随机 Prim（多分叉）";

        public static readonly string[] Algorithms = { AlgoDfs, AlgoPrim };

        public static Maze Generate(int cols, int rows, int seed, string algorithm, int braidPercent)
        {
            if (cols < 2) cols = 2;
            if (rows < 2) rows = 2;

            var rand = new Random(seed);
            var maze = new Maze(cols, rows);

            if (algorithm == AlgoPrim) CarvePrim(maze, rand);
            else CarveDfs(maze, rand);

            Braid(maze, braidPercent, rand);
            return maze;
        }

        private static Point Cell(int cx, int cy) => new(cx * 2 + 1, cy * 2 + 1);

        private static void OpenCell(Maze maze, int cx, int cy)
        {
            var p = Cell(cx, cy);
            maze.Grid[p.X, p.Y] = ' ';
        }

        private static void OpenWallBetween(Maze maze, int cx, int cy, int nx, int ny)
        {
            var p = Cell(cx, cy);
            maze.Grid[p.X + (nx - cx), p.Y + (ny - cy)] = ' ';
        }

        private static void CarveDfs(Maze maze, Random rand)
        {
            var visited = new bool[maze.Cols, maze.Rows];
            var stack = new Stack<Point>();
            var start = new Point(0, 0);
            visited[0, 0] = true;
            OpenCell(maze, 0, 0);
            stack.Push(start);

            var dirs = new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };

            while (stack.Count > 0)
            {
                var cur = stack.Peek();
                var options = new List<Point>();
                foreach (var d in dirs)
                {
                    var nx = cur.X + d.X;
                    var ny = cur.Y + d.Y;
                    if (nx >= 0 && ny >= 0 && nx < maze.Cols && ny < maze.Rows && !visited[nx, ny])
                        options.Add(new Point(nx, ny));
                }

                if (options.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var next = options[rand.Next(options.Count)];
                visited[next.X, next.Y] = true;
                OpenCell(maze, next.X, next.Y);
                OpenWallBetween(maze, cur.X, cur.Y, next.X, next.Y);
                stack.Push(next);
            }
        }

        private static void CarvePrim(Maze maze, Random rand)
        {
            var visited = new bool[maze.Cols, maze.Rows];
            var frontier = new List<(Point cell, Point from)>();
            var start = new Point(rand.Next(maze.Cols), rand.Next(maze.Rows));
            visited[start.X, start.Y] = true;
            OpenCell(maze, start.X, start.Y);

            void AddFrontier(Point cell)
            {
                var dirs = new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };
                foreach (var d in dirs)
                {
                    var nx = cell.X + d.X;
                    var ny = cell.Y + d.Y;
                    if (nx >= 0 && ny >= 0 && nx < maze.Cols && ny < maze.Rows && !visited[nx, ny])
                        frontier.Add((new Point(nx, ny), cell));
                }
            }

            AddFrontier(start);

            while (frontier.Count > 0)
            {
                var pick = rand.Next(frontier.Count);
                var (cell, from) = frontier[pick];
                frontier.RemoveAt(pick);

                if (visited[cell.X, cell.Y]) continue;

                visited[cell.X, cell.Y] = true;
                OpenCell(maze, cell.X, cell.Y);
                OpenWallBetween(maze, from.X, from.Y, cell.X, cell.Y);
                AddFrontier(cell);
            }
        }

        /// <summary>把一部分死胡同打通，制造环路（更难，纯 DFS 解不出来）。</summary>
        private static void Braid(Maze maze, int percent, Random rand)
        {
            if (percent <= 0) return;
            var dirs = new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };

            for (var cy = 0; cy < maze.Rows; cy++)
            {
                for (var cx = 0; cx < maze.Cols; cx++)
                {
                    var p = Cell(cx, cy);
                    var open = 0;
                    var closed = new List<Point>();
                    foreach (var d in dirs)
                    {
                        var nx = cx + d.X;
                        var ny = cy + d.Y;
                        if (nx < 0 || ny < 0 || nx >= maze.Cols || ny >= maze.Rows) continue;
                        if (maze.Grid[p.X + d.X, p.Y + d.Y] == ' ')
                            open++;
                        else
                            closed.Add(new Point(nx, ny));
                    }

                    if (open == 1 && closed.Count > 0 && rand.Next(100) < percent)
                    {
                        var target = closed[rand.Next(closed.Count)];
                        OpenWallBetween(maze, cx, cy, target.X, target.Y);
                    }
                }
            }
        }
    }
}
