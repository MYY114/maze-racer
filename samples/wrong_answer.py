"""故意写错的示例：输出一条不合法的路径，用来验证“答案校验”功能。

路径会跳过好几格，还会穿过墙，工具应判为「答案错误」。
"""
import sys

maze_path, out_path = sys.argv[1], sys.argv[2]
lines = open(maze_path, "r", encoding="utf-8").read().splitlines()
cols, rows = map(int, lines[0].split())

start = (1, 1)
end = (cols * 2 - 1, rows * 2 - 1)

with open(out_path, "w", encoding="utf-8") as f:
    f.write("1 1\n")
    f.write(f"{end[0]} 1\n")      # 直接横穿到右边，跳过很多格
    f.write(f"{end[0]} {end[1]}\n")
