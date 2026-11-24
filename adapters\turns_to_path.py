#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""MazeRacer 适配器：把 U/D/L/R 移动序列转换成坐标解。

用法：
    python turns_to_path.py <maze.txt> <moves.txt> <solution.txt>

moves.txt 可以写：
    UDLR
    U D L R
    u,d,l,r
    up down left right
允许空格、逗号、换行；出现其他单词会直接报错，避免把日志误当成移动。

坐标以迷宫字符网格为准（0 起，左上角原点）。起点 (1,1)，终点
(2*列-1, 2*行-1)，每步上下左右一格，不能穿墙或越界，必须停在终点。
转换过程本身就会检查撞墙、越界和终点，失败时退出码为 2。
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

DIRS = {"U": (0, -1), "D": (0, 1), "L": (-1, 0), "R": (1, 0)}
WORDS = {"UP": "U", "DOWN": "D", "LEFT": "L", "RIGHT": "R"}


class AdapterError(Exception):
    """输入不合法，转换无法完成。"""


def force_utf8_console() -> None:
    """让输出在管道里也使用 UTF-8，避免 MazeRacer 读到乱码。"""
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass


def load_maze(path: str):
    try:
        text = Path(path).read_text(encoding="utf-8-sig")
    except OSError as exc:
        raise AdapterError(f"读不到迷宫文件：{exc}") from exc

    lines = text.replace("\r\n", "\n").replace("\r", "\n").split("\n")
    idx = 0
    while idx < len(lines) and not lines[idx].strip():
        idx += 1
    if idx >= len(lines):
        raise AdapterError("迷宫文件是空的")

    header = lines[idx].split()
    if len(header) < 2:
        raise AdapterError("迷宫第一行应为“列 行”")
    try:
        cols, rows = int(header[0]), int(header[1])
    except ValueError as exc:
        raise AdapterError(f"迷宫尺寸不是整数：{header[0]!r} {header[1]!r}") from exc
    if cols < 1 or rows < 1:
        raise AdapterError(f"迷宫尺寸必须为正数，实际是 {cols} x {rows}")

    width, height = cols * 2 + 1, rows * 2 + 1
    grid_lines = lines[idx + 1: idx + 1 + height]
    if len(grid_lines) < height:
        raise AdapterError(f"迷宫行数不足：需要 {height} 行，实际 {len(grid_lines)} 行")

    grid = []
    for y, raw in enumerate(grid_lines):
        line = raw.rstrip("\r")
        if len(line) < width:
            raise AdapterError(f"迷宫第 {y + 1} 行长度不足：需要 {width} 个字符，实际 {len(line)} 个")
        grid.append(line[:width])

    start = (1, 1)
    end = (width - 2, height - 2)
    if grid[start[1]][start[0]] == "#":
        raise AdapterError(f"起点 {start} 是墙")
    if grid[end[1]][end[0]] == "#":
        raise AdapterError(f"终点 {end} 是墙")
    return grid, start, end


def parse_moves(path: str) -> list[str]:
    try:
        text = Path(path).read_text(encoding="utf-8-sig", errors="replace")
    except OSError as exc:
        raise AdapterError(f"读不到 moves 文件：{exc}") from exc

    moves: list[str] = []
    for token in re.findall(r"[A-Za-z]+", text):
        upper = token.upper()
        if upper in WORDS:
            moves.append(WORDS[upper])
        elif all(ch in DIRS for ch in upper):
            moves.extend(upper)
        else:
            raise AdapterError(
                f"moves 文件里有无法识别的内容：{token!r}。"
                "只接受 U/D/L/R 或 up/down/left/right，日志请写到 stderr。"
            )

    if not moves:
        raise AdapterError("moves 文件里没有找到任何 U/D/L/R 移动")
    return moves


def simulate(grid: list[str], start: tuple[int, int], end: tuple[int, int], moves: list[str]) -> list[tuple[int, int]]:
    x, y = start
    path = [start]

    for index, move in enumerate(moves, 1):
        dx, dy = DIRS[move]
        nx, ny = x + dx, y + dy
        if ny < 0 or ny >= len(grid) or nx < 0 or nx >= len(grid[ny]):
            raise AdapterError(f"第 {index} 步 {move} 越界：({nx},{ny})")
        if grid[ny][nx] == "#":
            raise AdapterError(f"第 {index} 步 {move} 撞墙：({nx},{ny})")
        x, y = nx, ny
        path.append((x, y))

    if (x, y) != end:
        raise AdapterError(f"移动序列走完停在 ({x},{y})，没有到达终点 E {end}")
    return path


def main(argv: list[str] | None = None) -> int:
    force_utf8_console()
    parser = argparse.ArgumentParser(description="把 U/D/L/R 移动序列转换成 MazeRacer 坐标解")
    parser.add_argument("maze", help="迷宫文本路径")
    parser.add_argument("moves", help="移动序列文件路径")
    parser.add_argument("solution", help="输出的坐标解路径")
    args = parser.parse_args(argv)

    try:
        grid, start, end = load_maze(args.maze)
        moves = parse_moves(args.moves)
        path = simulate(grid, start, end, moves)
    except AdapterError as exc:
        print(f"[turns_to_path] 错误：{exc}", file=sys.stderr)
        return 2

    out = Path(args.solution)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("".join(f"{x} {y}\n" for x, y in path), encoding="utf-8")
    print(f"[turns_to_path] OK：{len(moves)} 步，{len(path)} 个坐标 -> {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
