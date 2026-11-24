// Node.js BFS 解迷宫
// 用法：node solve_bfs.js maze.txt solution.txt
const fs = require('fs');

const mazePath = process.argv[2];
const outPath = process.argv[3];

const lines = fs.readFileSync(mazePath, 'utf8').replace(/\r/g, '').split('\n');
const [cols, rows] = lines[0].trim().split(/\s+/).map(Number);
const w = cols * 2 + 1;
const h = rows * 2 + 1;

const grid = [];
let start = null;
let end = null;

for (let y = 0; y < h; y++) {
  const line = (lines[y + 1] || '').padEnd(w);
  grid.push(line.split(''));
  for (let x = 0; x < w; x++) {
    if (line[x] === 'S') start = [x, y];
    if (line[x] === 'E') end = [x, y];
  }
}

const queue = [start];
const parent = new Map();
parent.set(start[0] + ',' + start[1], null);
const dirs = [[1, 0], [-1, 0], [0, 1], [0, -1]];

while (queue.length) {
  const [x, y] = queue.shift();
  if (x === end[0] && y === end[1]) break;
  for (const [dx, dy] of dirs) {
    const nx = x + dx;
    const ny = y + dy;
    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
    const key = nx + ',' + ny;
    if (parent.has(key)) continue;
    if (grid[ny][nx] === '#') continue;
    parent.set(key, [x, y]);
    queue.push([nx, ny]);
  }
}

let pathText = '';
if (parent.has(end[0] + ',' + end[1])) {
  let cur = end;
  const path = [];
  while (cur) {
    path.push(cur);
    cur = parent.get(cur[0] + ',' + cur[1]);
  }
  path.reverse();
  pathText = path.map(([x, y]) => `${x} ${y}`).join('\n') + '\n';
}

fs.writeFileSync(outPath, pathText, 'utf8');
