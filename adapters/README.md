# adapters：把已有脚本的输出转成坐标解

这里有两个纯 Python 3 的转换模板（只用标准库，不执行你的脚本），用来接住那些“算法已经写好、只是输出格式不一样”的脚本。完整契约见 [../PROTOCOL.md](../PROTOCOL.md)。

| 文件 | 输入 | 输出 |
| --- | --- | --- |
| `turns_to_path.py` | `U/D/L/R`（或 `up/down/left/right`）移动序列 | MazeRacer 的 `x y` 坐标解 |
| `json_to_path.py` | 坐标数组 JSON | MazeRacer 的 `x y` 坐标解 |

两个脚本都不会修改你的算法，只做格式搬运；路径是否合法仍由 MazeRacer 统一校验。

## 1. U/D/L/R → 坐标

```powershell
python turns_to_path.py maze.txt moves.txt solution.txt
```

`moves.txt` 支持这些写法，空格、逗号、换行随便加：

```text
UDLR
U D L R
u,d,l,r
up down left right
```

出现无法识别的单词会直接报错（避免把日志当成移动）。转换时会按迷宫走一遍，撞墙、越界、最后没停在 `E` 都会失败并给出第几步出错。

### 接进 MazeRacer

假设适配器放在 `D:\Tools\MazeRacer\adapters\`，你的脚本把移动序列打到 stdout，命令模板写：

```text
python "{script}" > "{dir}\moves.txt" && python "D:\Tools\MazeRacer\adapters\turns_to_path.py" "{input}" "{dir}\moves.txt" "{output}"
```

如果你的脚本自己写 `moves.txt` 到当前目录，可以省掉重定向：

```text
python "{script}" "{input}" && python "D:\Tools\MazeRacer\adapters\turns_to_path.py" "{input}" "{dir}\moves.txt" "{output}"
```

## 2. JSON → 坐标

```powershell
python json_to_path.py raw.json solution.txt
```

支持这些结构：

```json
{"path": [[1,1], [2,1], [2,2]]}
```

```json
{"solution": [[1,1], [2,1]]}
```

```json
[[1,1], [2,1], [2,2]]
```

```json
[1, 1, 2, 1, 2, 2]
```

```json
["1 1", "2 1", "2 2"]
```

```json
[{"x": 1, "y": 1}, {"x": 2, "y": 1}]
```

对象的键名可以是 `path`、`solution`、`route`、`points`、`coordinates`、`answer`。

### 接进 MazeRacer

脚本把 JSON 打到 stdout：

```text
python "{script}" > "{dir}\out.json" && python "D:\Tools\MazeRacer\adapters\json_to_path.py" "{dir}\out.json" "{output}"
```

脚本接受第三个参数（输出文件路径）：

```text
node "{script}" "{input}" "{dir}\out.json" && python "D:\Tools\MazeRacer\adapters\json_to_path.py" "{dir}\out.json" "{output}"
```

## 注意事项

- **适配器路径要写绝对路径**。`{dir}` 是每个脚本独立的临时工作目录，不是 exe 所在目录；把 `D:\Tools\MazeRacer\adapters\` 换成你实际存放的位置。
- 需要 `python` 在 PATH 里；只有 `py` 启动器时，把命令里的 `python` 换成 `py -3`。
- 你的脚本请把日志写到 **stderr**；stdout 要留给移动序列或 JSON，否则重定向出来的文件会被日志污染。
- JSON 文件请保存为 UTF-8。
- 这些模板只做转换，不负责启动你的脚本；启动仍由 MazeRacer 的命令模板负责，所以任何语言都能用。
