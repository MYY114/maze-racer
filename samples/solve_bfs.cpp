// C++ BFS 解迷宫（需要 g++，工作流/本机需自行准备）
// 用法：g++ solve_bfs.cpp -O2 -o solve.exe && solve.exe maze.txt solution.txt
#include <bits/stdc++.h>
using namespace std;

int main(int argc, char** argv) {
    if (argc < 3) return 1;
    ifstream in(argv[1]);
    int cols, rows;
    in >> cols >> rows;
    in.ignore(numeric_limits<streamsize>::max(), '\n');

    int w = cols * 2 + 1, h = rows * 2 + 1;
    vector<string> g(h);
    for (int y = 0; y < h; y++) {
        getline(in, g[y]);
        if ((int)g[y].size() < w) g[y].resize(w, ' ');
    }

    int start = -1, end = -1;
    for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++) {
            if (g[y][x] == 'S') start = y * w + x;
            if (g[y][x] == 'E') end = y * w + x;
        }

    vector<int> parent(w * h, -2);
    parent[start] = -1;
    queue<int> q;
    q.push(start);
    const int dx[4] = {1, -1, 0, 0};
    const int dy[4] = {0, 0, 1, -1};

    while (!q.empty()) {
        int cur = q.front(); q.pop();
        if (cur == end) break;
        int x = cur % w, y = cur / w;
        for (int d = 0; d < 4; d++) {
            int nx = x + dx[d], ny = y + dy[d];
            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
            int n = ny * w + nx;
            if (parent[n] != -2 || g[ny][nx] == '#') continue;
            parent[n] = cur;
            q.push(n);
        }
    }

    ofstream out(argv[2]);
    if (parent[end] != -2) {
        vector<int> path;
        for (int cur = end; cur != -1; cur = parent[cur]) path.push_back(cur);
        reverse(path.begin(), path.end());
        for (int idx : path) out << idx % w << ' ' << idx / w << '\n';
    }
    return 0;
}
