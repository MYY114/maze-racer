"""演示用的“慢脚本”：先睡 250ms，再正常 BFS，用来看排名差异。

真实测速时请删掉 sleep，不然它永远排最后。
"""
import sys
import time
from collections import deque


def main():
    maze_path, out_path = sys.argv[1], sys.argv[2]
    lines = open(maze_path, "r", encoding="utf-8").read().splitlines()
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

    time.sleep(0.25)

    queue = deque([start])
    parent = {start: None}
    for_pop = ((1, 0), (-1, 0), (0, 1), (0, -1))
    while queue:
        x, y = queue.popleft()
        if (x, y) == end:
            break
        for dx, dy in for_pop:
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] != "#" and (nx, ny) not in parent:
                parent[(nx, ny)] = (x, y)
                queue.append((nx, ny))

    path = []
    cur = end
    while cur is not None and cur in parent:
        path.append(cur)
        cur = parent[cur]
    path.reverse()

    with open(out_path, "w", encoding="utf-8") as f:
        for x, y in path:
            f.write(f"{x} {y}\n")


if __name__ == "__main__":
    main()
