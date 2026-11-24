"""BFS（广度优先）解迷宫：读取 ASCII 迷宫，输出最短路径到 solution.txt。

用法：python solve_bfs.py maze.txt solution.txt
坐标是字符网格坐标（0 开始），每行输出 "x y"。
"""
import sys
from collections import deque


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
    queue = deque([start])
    parent = {start: None}
    dirs = ((1, 0), (-1, 0), (0, 1), (0, -1))

    while queue:
        x, y = queue.popleft()
        if (x, y) == end:
            break
        for dx, dy in dirs:
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] != "#" and (nx, ny) not in parent:
                parent[(nx, ny)] = (x, y)
                queue.append((nx, ny))

    if end not in parent:
        return []

    path = []
    cur = end
    while cur is not None:
        path.append(cur)
        cur = parent[cur]
    path.reverse()
    return path


def main():
    maze_path, out_path = sys.argv[1], sys.argv[2]
    grid, start, end = load_maze(maze_path)
    path = solve(grid, start, end)
    with open(out_path, "w", encoding="utf-8") as f:
        for x, y in path:
            f.write(f"{x} {y}\n")


if __name__ == "__main__":
    main()
