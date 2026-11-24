"""DFS（深度优先）解迷宫：不保证最短路，用来对比不同算法/语言的耗时。

用法：python solve_dfs.py maze.txt solution.txt
"""
import sys


def load_maze(path):
    lines = open(path, "r", encoding="utf-8").read().splitlines()
    cols, rows = map(int, lines[0].split())
    w, h = cols * 2 + 1, rows * 2 + 1
    grid = [list(line.ljust(w)) for line in lines[1:1 + h]]
    start = end = None
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch == "S":
                start = (x, y)
            elif ch == "E":
                end = (x, y)
    return grid, start, end


def solve(grid, start, end):
    h, w = len(grid), len(grid[0])
    stack = [(start, [(start)])]
    seen = {start}
    dirs = ((1, 0), (0, 1), (-1, 0), (0, -1))

    while stack:
        (x, y), path = stack.pop()
        if (x, y) == end:
            return path
        for dx, dy in dirs:
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] != "#" and (nx, ny) not in seen:
                seen.add((nx, ny))
                stack.append(((nx, ny), path + [(nx, ny)]))
    return []


def main():
    maze_path, out_path = sys.argv[1], sys.argv[2]
    grid, start, end = load_maze(maze_path)
    path = solve(grid, start, end)
    with open(out_path, "w", encoding="utf-8") as f:
        for x, y in path:
            f.write(f"{x} {y}\n")


if __name__ == "__main__":
    main()
