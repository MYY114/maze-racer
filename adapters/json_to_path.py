#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""MazeRacer 适配器：把 JSON 路径转换成坐标解。

用法：
    python json_to_path.py <raw.json> <solution.txt>

支持的 JSON 写法（任选其一）：
    {"path": [[1,1], [2,1], ...]}
    {"solution": [[1,1], ...]}
    {"route": [[1,1], ...]}
    {"points": [[1,1], ...]}
    [[1,1], [2,1], ...]
    [1, 1, 2, 1, ...]                 # 扁平坐标对
    ["1 1", "2 1", ...]
    [{"x": 1, "y": 1}, ...]

脚本只做结构转换，不改写路径；路径是否合法由 MazeRacer 校验。
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

PATH_KEYS = ("path", "solution", "route", "points", "coordinates", "answer")


class AdapterError(Exception):
    """JSON 结构或坐标不合法。"""


def force_utf8_console() -> None:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass


def as_int(value, where: str) -> int:
    if isinstance(value, bool) or value is None:
        raise AdapterError(f"{where} 不是整数：{value!r}")
    if isinstance(value, int):
        return value
    if isinstance(value, float):
        if value.is_integer():
            return int(value)
        raise AdapterError(f"{where} 不是整数：{value!r}")
    if isinstance(value, str) and re.fullmatch(r"-?\d+", value.strip()):
        return int(value.strip())
    raise AdapterError(f"{where} 不是整数：{value!r}")


def parse_point(value, where: str) -> tuple[int, int]:
    if isinstance(value, dict):
        if "x" in value and "y" in value:
            return as_int(value["x"], f"{where}.x"), as_int(value["y"], f"{where}.y")
        raise AdapterError(f"{where} 是对象，但缺少 x / y 字段：{value!r}")

    if isinstance(value, (list, tuple)):
        if len(value) != 2:
            raise AdapterError(f"{where} 应为 [x, y]，实际是 {value!r}")
        return as_int(value[0], f"{where}[0]"), as_int(value[1], f"{where}[1]")

    if isinstance(value, str):
        match = re.fullmatch(r"\s*(-?\d+)\s*(?:,|\s)\s*(-?\d+)\s*", value)
        if not match:
            raise AdapterError(f"{where} 应为 \"x y\" 或 \"x,y\"，实际是 {value!r}")
        return int(match.group(1)), int(match.group(2))

    raise AdapterError(f"{where} 不是坐标：{value!r}")


def extract_points(data) -> list[tuple[int, int]]:
    if isinstance(data, dict):
        for key in PATH_KEYS:
            if key in data:
                return extract_points(data[key])
        if "x" in data and "y" in data:
            return [parse_point(data, "point")]
        raise AdapterError(
            "JSON 对象里找不到路径字段（path / solution / route / points / coordinates），"
            "也没有直接的 x / y"
        )

    if isinstance(data, str):
        points = []
        for line_no, line in enumerate(data.splitlines(), 1):
            if not line.strip():
                continue
            points.append(parse_point(line, f"字符串第 {line_no} 行"))
        if not points:
            raise AdapterError("JSON 字符串里没有坐标")
        return points

    if isinstance(data, list):
        if not data:
            raise AdapterError("路径数组是空的")

        first = data[0]
        if isinstance(first, (int, float)) and not isinstance(first, bool):
            if not all(isinstance(v, (int, float)) and not isinstance(v, bool) for v in data):
                raise AdapterError("扁平数组里混入了非数字元素")
            if len(data) % 2 != 0:
                raise AdapterError(f"扁平数组长度必须为偶数，实际 {len(data)}")
            return [
                (as_int(data[i], f"扁平数组[{i}]"), as_int(data[i + 1], f"扁平数组[{i + 1}]"))
                for i in range(0, len(data), 2)
            ]

        return [parse_point(value, f"第 {i + 1} 个坐标") for i, value in enumerate(data)]

    raise AdapterError(f"不支持的 JSON 顶层类型：{type(data).__name__}")


def main(argv: list[str] | None = None) -> int:
    force_utf8_console()
    parser = argparse.ArgumentParser(description="把 JSON 路径转换成 MazeRacer 坐标解")
    parser.add_argument("raw", help="脚本输出的 JSON 文件")
    parser.add_argument("solution", help="输出的坐标解路径")
    args = parser.parse_args(argv)

    try:
        raw_text = Path(args.raw).read_text(encoding="utf-8-sig")
    except OSError as exc:
        print(f"[json_to_path] 错误：读不到 JSON 文件：{exc}", file=sys.stderr)
        return 2
    except UnicodeDecodeError as exc:
        print(f"[json_to_path] 错误：JSON 文件不是 UTF-8：{exc}", file=sys.stderr)
        return 2

    try:
        data = json.loads(raw_text)
        points = extract_points(data)
    except json.JSONDecodeError as exc:
        print(f"[json_to_path] 错误：JSON 解析失败：{exc}", file=sys.stderr)
        return 2
    except AdapterError as exc:
        print(f"[json_to_path] 错误：{exc}", file=sys.stderr)
        return 2

    for index, (x, y) in enumerate(points, 1):
        if x < 0 or y < 0:
            print(
                f"[json_to_path] 错误：第 {index} 个坐标 ({x},{y}) 为负数，"
                "字符网格坐标应从 0 开始",
                file=sys.stderr,
            )
            return 2

    out = Path(args.solution)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("".join(f"{x} {y}\n" for x, y in points), encoding="utf-8")
    print(f"[json_to_path] OK：{len(points)} 个坐标 -> {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
