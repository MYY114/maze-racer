# PowerShell BFS 解迷宫
# 用法：pwsh -NoProfile -File solve_bfs.ps1 maze.txt solution.txt
param(
    [Parameter(Mandatory = $true)][string]$MazePath,
    [Parameter(Mandatory = $true)][string]$OutPath
)

$lines = [System.IO.File]::ReadAllLines($MazePath)
$head = $lines[0].Trim() -split ' '
$cols = [int]$head[0]
$rows = [int]$head[1]
$w = $cols * 2 + 1
$h = $rows * 2 + 1

$start = -1
$end = -1
for ($y = 0; $y -lt $h; $y++) {
    $line = $lines[$y + 1]
    for ($x = 0; $x -lt $w; $x++) {
        $ch = $line[$x]
        if ($ch -eq 'S') { $start = $y * $w + $x }
        elseif ($ch -eq 'E') { $end = $y * $w + $x }
    }
}

$parent = New-Object 'int[]' ($w * $h)
for ($i = 0; $i -lt $parent.Length; $i++) { $parent[$i] = -2 }
$parent[$start] = -1

$queue = New-Object 'System.Collections.Generic.Queue[int]'
$queue.Enqueue($start)

while ($queue.Count -gt 0) {
    $cur = $queue.Dequeue()
    if ($cur -eq $end) { break }

    $x = $cur % $w
    $y = [int](($cur - $x) / $w)

    foreach ($d in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
        $nx = $x + $d[0]
        $ny = $y + $d[1]
        if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $w -or $ny -ge $h) { continue }
        $n = $ny * $w + $nx
        if ($parent[$n] -ne -2) { continue }
        if ($lines[$ny + 1][$nx] -eq '#') { continue }

        $parent[$n] = $cur
        $queue.Enqueue($n)
    }
}

$sb = New-Object System.Text.StringBuilder
if ($parent[$end] -ne -2) {
    $path = New-Object 'System.Collections.Generic.List[int]'
    $cur = $end
    while ($cur -ne -1) {
        $path.Add($cur)
        $cur = $parent[$cur]
    }
    for ($i = $path.Count - 1; $i -ge 0; $i--) {
        $idx = $path[$i]
        $x = $idx % $w
        $y = [int](($idx - $x) / $w)
        [void]$sb.AppendLine("$x $y")
    }
}

[System.IO.File]::WriteAllText($OutPath, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
