using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MazeRacer
{
    public sealed class MainForm : Form
    {
        private sealed class ScriptRow
        {
            public ScriptEntry Entry;
            public ScriptRunResult Result;
            public int? Rank;
            public DataGridViewRow GridRow;
        }

        private Maze _maze;
        private readonly List<ScriptRow> _rows = new();
        private CancellationTokenSource _cts;
        private bool _running;

        // ---------- 控件 ----------
        private readonly NumericUpDown _numCols = new();
        private readonly NumericUpDown _numRows = new();
        private readonly TextBox _txtSeed = new();
        private readonly Button _btnRandomSeed = new();
        private readonly ComboBox _cmbAlgo = new();
        private readonly NumericUpDown _numBraid = new();
        private readonly Button _btnGenerate = new();
        private readonly Button _btnSaveMaze = new();
        private readonly Button _btnSamples = new();

        private readonly NumericUpDown _numTimeout = new();
        private readonly CheckBox _chkParallel = new();
        private readonly Button _btnAddScripts = new();
        private readonly Button _btnAddFolder = new();
        private readonly Button _btnRemove = new();
        private readonly Button _btnRun = new();
        private readonly Button _btnStop = new();
        private readonly Button _btnClear = new();

        private readonly MazePanel _mazePanel = new();
        private readonly DataGridView _grid = new();
        private readonly TextBox _txtCommand = new();
        private readonly Button _btnApplyCommand = new();
        private readonly Label _lblCommandHint = new();
        private readonly Label _lblSummary = new();
        private readonly Label _lblStatus = new();

        public MainForm()
        {
            Text = "迷宫竞速 · 多语言脚本解迷宫测速";
            ClientSize = new Size(1220, 760);
            MinimumSize = new Size(1040, 660);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9f);
            BackColor = Color.FromArgb(246, 247, 249);

            BuildUi();
            UpdateButtons();
            GenerateMaze();
        }

        // ---------------- 界面 ----------------------------
        private void BuildUi()
        {
            var lblCols = new Label { Text = "列", AutoSize = true, Location = new Point(12, 18) };
            Controls.Add(lblCols);
            _numCols.SetBounds(36, 14, 58, 26);
            _numCols.Minimum = 4;
            _numCols.Maximum = 200;
            _numCols.Value = 25;
            Controls.Add(_numCols);

            var lblRows = new Label { Text = "行", AutoSize = true, Location = new Point(102, 18) };
            Controls.Add(lblRows);
            _numRows.SetBounds(126, 14, 58, 26);
            _numRows.Minimum = 4;
            _numRows.Maximum = 200;
            _numRows.Value = 25;
            Controls.Add(_numRows);

            var lblSeed = new Label { Text = "种子", AutoSize = true, Location = new Point(192, 18) };
            Controls.Add(lblSeed);
            _txtSeed.SetBounds(228, 14, 86, 26);
            _txtSeed.Text = "12345";
            Controls.Add(_txtSeed);

            _btnRandomSeed.Text = "随机";
            _btnRandomSeed.SetBounds(320, 14, 52, 28);
            _btnRandomSeed.Click += (_, _) => _txtSeed.Text = new Random().Next(1, 999999).ToString();
            Controls.Add(_btnRandomSeed);

            var lblAlgo = new Label { Text = "算法", AutoSize = true, Location = new Point(382, 18) };
            Controls.Add(lblAlgo);
            _cmbAlgo.SetBounds(418, 14, 168, 26);
            _cmbAlgo.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbAlgo.Items.AddRange(MazeGenerator.Algorithms);
            _cmbAlgo.SelectedIndex = 0;
            Controls.Add(_cmbAlgo);

            var lblBraid = new Label { Text = "环路%", AutoSize = true, Location = new Point(596, 18) };
            Controls.Add(lblBraid);
            _numBraid.SetBounds(646, 14, 54, 26);
            _numBraid.Minimum = 0;
            _numBraid.Maximum = 80;
            _numBraid.Value = 0;
            Controls.Add(_numBraid);

            _btnGenerate.Text = "生成迷宫";
            _btnGenerate.SetBounds(712, 12, 104, 32);
            _btnGenerate.BackColor = Color.FromArgb(64, 132, 255);
            _btnGenerate.ForeColor = Color.White;
            _btnGenerate.FlatStyle = FlatStyle.Flat;
            _btnGenerate.FlatAppearance.BorderSize = 0;
            _btnGenerate.Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
            _btnGenerate.Click += (_, _) => GenerateMaze();
            Controls.Add(_btnGenerate);

            _btnSaveMaze.Text = "保存迷宫";
            _btnSaveMaze.SetBounds(824, 12, 96, 32);
            _btnSaveMaze.Click += BtnSaveMaze_Click;
            Controls.Add(_btnSaveMaze);

            _btnSamples.Text = "载入示例脚本";
            _btnSamples.SetBounds(928, 12, 120, 32);
            _btnSamples.Click += BtnSamples_Click;
            Controls.Add(_btnSamples);

            var lblTimeout = new Label { Text = "超时(ms)", AutoSize = true, Location = new Point(12, 62) };
            Controls.Add(lblTimeout);
            _numTimeout.SetBounds(80, 58, 72, 26);
            _numTimeout.Minimum = 200;
            _numTimeout.Maximum = 600000;
            _numTimeout.Increment = 1000;
            _numTimeout.Value = 30000;
            Controls.Add(_numTimeout);

            _chkParallel.Text = "多脚本同时跑";
            _chkParallel.SetBounds(164, 60, 128, 22);
            _chkParallel.Checked = true;
            Controls.Add(_chkParallel);

            _btnAddScripts.Text = "添加脚本…";
            _btnAddScripts.SetBounds(300, 54, 104, 34);
            _btnAddScripts.Click += BtnAddScripts_Click;
            Controls.Add(_btnAddScripts);

            _btnAddFolder.Text = "导入文件夹…";
            _btnAddFolder.SetBounds(412, 54, 112, 34);
            _btnAddFolder.Click += BtnAddFolder_Click;
            Controls.Add(_btnAddFolder);

            _btnRemove.Text = "移除选中";
            _btnRemove.SetBounds(532, 54, 96, 34);
            _btnRemove.Click += BtnRemove_Click;
            Controls.Add(_btnRemove);

            _btnRun.Text = "▶ 开始测速";
            _btnRun.SetBounds(640, 50, 110, 40);
            _btnRun.BackColor = Color.FromArgb(52, 199, 89);
            _btnRun.ForeColor = Color.White;
            _btnRun.FlatStyle = FlatStyle.Flat;
            _btnRun.FlatAppearance.BorderSize = 0;
            _btnRun.Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold);
            _btnRun.Click += (_, _) => _ = RunAllAsync();
            Controls.Add(_btnRun);

            _btnStop.Text = "停止";
            _btnStop.SetBounds(758, 50, 90, 40);
            _btnStop.Enabled = false;
            _btnStop.Click += (_, _) =>
            {
                _cts?.Cancel();
                _lblStatus.Text = "正在停止所有脚本…";
            };
            Controls.Add(_btnStop);

            _btnClear.Text = "清空结果";
            _btnClear.SetBounds(856, 54, 96, 34);
            _btnClear.Click += (_, _) =>
            {
                foreach (var r in _rows) { r.Result = null; r.Rank = null; UpdateRow(r); }
                _lblSummary.Text = "尚无成绩";
                _mazePanel.SetSolution(null);
            };
            Controls.Add(_btnClear);

            var hint = new Label
            {
                Text = "同一迷宫、同时开跑，按耗时排名",
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(964, 62)
            };
            Controls.Add(hint);

            _mazePanel.SetBounds(12, 100, 600, 508);
            _mazePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            Controls.Add(_mazePanel);

            _grid.SetBounds(628, 100, 584, 398);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Columns.Add(Column("排名", 48, false));
            _grid.Columns.Add(Column("脚本", 190, false));
            _grid.Columns.Add(Column("语言", 84, false));
            _grid.Columns.Add(Column("状态", 90, false));
            _grid.Columns.Add(Column("耗时(ms)", 76, false));
            _grid.Columns.Add(Column("说明", 260, true));
            _grid.SelectionChanged += Grid_SelectionChanged;
            Controls.Add(_grid);

            var lblCmd = new Label { Text = "命令模板", AutoSize = true, Location = new Point(628, 514) };
            Controls.Add(lblCmd);
            _txtCommand.SetBounds(700, 508, 512, 26);
            _txtCommand.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_txtCommand);

            _btnApplyCommand.Text = "应用到选中";
            _btnApplyCommand.SetBounds(700, 540, 96, 26);
            _btnApplyCommand.Click += BtnApplyCommand_Click;
            Controls.Add(_btnApplyCommand);

            _lblCommandHint.SetBounds(804, 544, 408, 20);
            _lblCommandHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblCommandHint.AutoEllipsis = true;
            _lblCommandHint.ForeColor = Color.FromArgb(120, 120, 120);
            _lblCommandHint.Text = "占位符：{script} {input} {output} {dir} {name}";
            Controls.Add(_lblCommandHint);

            _lblSummary.SetBounds(628, 578, 584, 30);
            _lblSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblSummary.Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
            _lblSummary.ForeColor = Color.FromArgb(40, 120, 60);
            _lblSummary.Text = "尚无成绩";
            Controls.Add(_lblSummary);

            _lblStatus.SetBounds(12, 724, 1200, 24);
            _lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _lblStatus.ForeColor = Color.FromArgb(90, 90, 90);
            _lblStatus.Text = "选择任意语言的脚本 → 同一迷宫同时开跑 → 自动校验路径并按耗时排名。";
            Controls.Add(_lblStatus);
        }

        private static DataGridViewTextBoxColumn Column(string header, int width, bool fill)
        {
            return new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Width = width,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.True
            };
        }

        // ---------------- 迷宫 ----------------------------
        private void GenerateMaze()
        {
            if (_running) return;

            var cols = (int)_numCols.Value;
            var rows = (int)_numRows.Value;
            var seed = ParseSeed();
            var algo = _cmbAlgo.SelectedItem?.ToString() ?? MazeGenerator.AlgoDfs;
            var braid = (int)_numBraid.Value;

            _maze = MazeGenerator.Generate(cols, rows, seed, algo, braid);
            var braidText = braid > 0 ? $" · 环路 {braid}%" : string.Empty;
            _mazePanel.SetMaze(_maze, $"{cols} × {rows} 格 · {algo} · 种子 {seed}{braidText}");
            _lblStatus.Text = $"已生成 {cols}×{rows} 迷宫（{algo}，种子 {seed}{braidText}）。添加脚本后点“开始测速”。";
            _lblStatus.ForeColor = Color.FromArgb(90, 90, 90);
            UpdateButtons();
        }

        private int ParseSeed()
        {
            if (int.TryParse(_txtSeed.Text.Trim(), out var seed)) return seed;
            seed = new Random().Next(1, 999999);
            _txtSeed.Text = seed.ToString();
            return seed;
        }

        private void BtnSaveMaze_Click(object sender, EventArgs e)
        {
            if (_maze == null) return;
            using var dialog = new SaveFileDialog
            {
                Filter = "迷宫文本 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FileName = $"maze_{_maze.Cols}x{_maze.Rows}.txt"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            _maze.SaveAscii(dialog.FileName);
            _lblStatus.Text = "迷宫已保存：" + dialog.FileName;
        }

        private void BtnSamples_Click(object sender, EventArgs e)
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "samples");
            if (!Directory.Exists(dir))
            {
                _lblStatus.Text = "没有找到 samples 目录（应与 exe 放在一起）。";
                _lblStatus.ForeColor = Color.FromArgb(200, 90, 0);
                return;
            }

            var added = 0;
            foreach (var file in Directory.GetFiles(dir).OrderBy(x => x))
            {
                if (IsScriptFile(file) && AddScript(file)) added++;
            }
            _lblStatus.Text = $"已载入 {added} 个示例脚本（运行环境不全会显示启动失败）。";
        }

        // ---------------- 脚本列表 ----------------------------
        private void BtnAddScripts_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "脚本文件|*.py;*.pyw;*.js;*.mjs;*.ts;*.ps1;*.bat;*.cmd;*.rb;*.php;*.lua;*.pl;*.go;*.java;*.kt;*.cpp;*.cc;*.c;*.rs;*.sh;*.r;*.cs;*.swift|所有文件|*.*"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var added = 0;
            foreach (var file in dialog.FileNames) if (AddScript(file)) added++;
            _lblStatus.Text = $"添加了 {added} 个脚本。";
        }

        private void BtnAddFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog { Description = "选择存放脚本的文件夹（会递归扫描）" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var added = 0;
            var skipped = 0;
            foreach (var file in Directory.GetFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories))
            {
                if (!IsScriptFile(file)) { skipped++; continue; }
                if (AddScript(file)) added++;
            }
            _lblStatus.Text = $"从文件夹添加 {added} 个脚本" + (skipped > 0 ? $"（跳过 {skipped} 个非脚本文件）" : string.Empty);
        }

        private static bool IsScriptFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext.Length == 0) return false;
            string[] skip = { ".txt", ".md", ".json", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".pdf", ".exe", ".dll", ".pdb", ".log", ".ini", ".xml", ".csv", ".zip", ".7z", ".gitignore" };
            return !skip.Contains(ext);
        }

        private bool AddScript(string path)
        {
            if (_rows.Any(r => string.Equals(r.Entry.ScriptPath, path, StringComparison.OrdinalIgnoreCase))) return false;

            var info = LanguageCatalog.Detect(path);
            var row = new ScriptRow
            {
                Entry = new ScriptEntry
                {
                    ScriptPath = path,
                    Language = info.Name,
                    CommandTemplate = info.CommandTemplate,
                    Hint = info.Hint
                }
            };
            _rows.Add(row);
            RefreshGrid();
            UpdateButtons();
            return true;
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (SelectedRow() is { } row)
            {
                _rows.Remove(row);
                RefreshGrid();
                UpdateButtons();
            }
        }

        private ScriptRow SelectedRow()
        {
            if (_grid.SelectedRows.Count == 0) return null;
            return _grid.SelectedRows[0].Tag as ScriptRow;
        }

        private void RefreshGrid()
        {
            _grid.Rows.Clear();
            foreach (var row in _rows)
            {
                var index = _grid.Rows.Add();
                row.GridRow = _grid.Rows[index];
                row.GridRow.Tag = row;
                UpdateRow(row);
            }
            if (_grid.Rows.Count == 0)
            {
                _txtCommand.Text = string.Empty;
                _mazePanel.SetSolution(null);
                return;
            }

            if (_grid.Rows.Count > 0 && _grid.SelectedRows.Count == 0)
                _grid.Rows[0].Selected = true;
            Grid_SelectionChanged(null, EventArgs.Empty);
        }

        private void UpdateRow(ScriptRow row)
        {
            if (row.GridRow == null) return;
            var result = row.Result;

            row.GridRow.Cells[0].Value = row.Rank?.ToString() ?? "—";
            row.GridRow.Cells[1].Value = row.Entry.Name;
            row.GridRow.Cells[2].Value = row.Entry.Language;
            row.GridRow.Cells[3].Value = result?.Status ?? "等待";
            row.GridRow.Cells[4].Value = result != null && result.ElapsedMs > 0 ? result.ElapsedMs.ToString() : "—";
            row.GridRow.Cells[5].Value = result?.Message ?? "";

            if (result == null)
            {
                row.GridRow.DefaultCellStyle.ForeColor = Color.FromArgb(80, 80, 80);
                return;
            }

            var color = result.Status switch
            {
                "完成" => Color.FromArgb(40, 130, 60),
                "运行中" => Color.FromArgb(45, 100, 200),
                "等待" => Color.FromArgb(120, 120, 120),
                _ => Color.FromArgb(190, 80, 0)
            };
            row.GridRow.DefaultCellStyle.ForeColor = color;
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            var row = SelectedRow();
            if (row == null) return;
            _txtCommand.Text = row.Entry.CommandTemplate ?? string.Empty;
            if (row.Entry.Hint != null)
                _lblCommandHint.Text = row.Entry.Hint + "   占位符：{script} {input} {output} {dir}";
            else
                _lblCommandHint.Text = "占位符：{script} {input} {output} {dir} {name}";

            if (row.Result?.Path is { Count: > 0 } path)
                _mazePanel.SetSolution(path);
            else
                _mazePanel.SetSolution(null);
        }

        private void BtnApplyCommand_Click(object sender, EventArgs e)
        {
            var row = SelectedRow();
            if (row == null) return;
            row.Entry.CommandTemplate = _txtCommand.Text.Trim();
            _lblStatus.Text = $"已更新 {row.Entry.Name} 的启动命令。";
        }

        // ---------------- 运行 ----------------------------
        private async Task RunAllAsync()
        {
            if (_running) return;

            if (_maze == null) GenerateMaze();
            if (_rows.Count == 0)
            {
                _lblStatus.Text = "先添加至少一个脚本（可点“载入示例脚本”快速体验）。";
                _lblStatus.ForeColor = Color.FromArgb(200, 90, 0);
                return;
            }

            _running = true;
            _cts = new CancellationTokenSource();
            UpdateButtons();

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var workRoot = Path.Combine(Path.GetTempPath(), "MazeRacer", stamp);
            Directory.CreateDirectory(workRoot);
            Directory.CreateDirectory(Path.Combine(workRoot, "scripts"));

            var mazeFile = Path.Combine(workRoot, "maze.txt");
            _maze.SaveAscii(mazeFile);

            foreach (var row in _rows)
            {
                row.Result = new ScriptRunResult { Status = "等待" };
                row.Rank = null;
                UpdateRow(row);
            }
            _lblSummary.Text = "比赛进行中…";
            _mazePanel.SetSolution(null);

            var total = _rows.Count;
            var finished = 0;
            var timeout = (int)_numTimeout.Value;
            var token = _cts.Token;

            async Task RunOne(ScriptRow row, int index)
            {
                var safe = string.Join("_", row.Entry.Name.Split(Path.GetInvalidFileNameChars()));
                var workDir = Path.Combine(workRoot, $"{index:00}_{safe}");
                row.Result = new ScriptRunResult { Status = "运行中", Message = "进程已启动" };
                Ui(() => UpdateRow(row));

                ScriptRunResult result;
                try
                {
                    result = await ScriptRunner.RunAsync(row.Entry, _maze, mazeFile, timeout, workDir, token);
                }
                catch (Exception ex)
                {
                    result = new ScriptRunResult { Status = "异常", Message = ex.Message };
                }

                finished++;
                row.Result = result;
                Ui(() =>
                {
                    UpdateRow(row);
                    if (result.Valid) _mazePanel.SetSolution(result.Path);
                    _lblStatus.Text = $"进度 {finished}/{total} · {row.Entry.Name}：{result.Status} {result.ElapsedMs} ms";
                    _lblStatus.ForeColor = Color.FromArgb(90, 90, 90);
                });
            }

            try
            {
                if (_chkParallel.Checked)
                {
                    var tasks = new List<Task>();
                    for (var i = 0; i < _rows.Count; i++)
                    {
                        var row = _rows[i];
                        var idx = i + 1;
                        tasks.Add(Task.Run(() => RunOne(row, idx)));
                    }
                    await Task.WhenAll(tasks);
                }
                else
                {
                    for (var i = 0; i < _rows.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        await RunOne(_rows[i], i + 1);
                    }
                }
            }
            finally
            {
                var ranked = _rows.Where(r => r.Result is { Valid: true }).OrderBy(r => r.Result.ElapsedMs).ToList();
                for (var i = 0; i < ranked.Count; i++) ranked[i].Rank = i + 1;
                foreach (var row in _rows.Where(r => r.Rank == null)) row.Rank = null;
                Ui(() =>
                {
                    var ordered = _rows.OrderBy(r => r.Rank ?? int.MaxValue).ThenBy(r => r.Entry.Name).ToList();
                    _rows.Clear();
                    _rows.AddRange(ordered);
                    RefreshGrid();
                    ShowSummary(ranked);
                    _running = false;
                    UpdateButtons();
                    _lblStatus.Text = ranked.Count == 0
                        ? "本轮没有有效成绩，检查脚本命令或 solution.txt 格式。"
                        : $"完成：有效 {ranked.Count}/{_rows.Count}" +
                          (ranked.Count > 1 ? $"，亚军 {ranked[1].Entry.Name} {ranked[1].Result.ElapsedMs} ms" : string.Empty) +
                          (ranked.Count > 2 ? $"，季军 {ranked[2].Entry.Name} {ranked[2].Result.ElapsedMs} ms" : string.Empty);
                    _lblStatus.ForeColor = ranked.Count == 0 ? Color.FromArgb(200, 90, 0) : Color.FromArgb(40, 120, 60);
                });
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void ShowSummary(List<ScriptRow> ranked)
        {
            if (ranked.Count == 0)
            {
                _lblSummary.Text = "尚无有效成绩";
                _lblSummary.ForeColor = Color.FromArgb(190, 80, 0);
                return;
            }

            var champion = ranked[0];
            _lblSummary.Text = $"冠军：{champion.Entry.Name}  {champion.Result.ElapsedMs} ms    ·    有效 {ranked.Count}/{_rows.Count}";
            _lblSummary.ForeColor = Color.FromArgb(40, 120, 60);
        }

        private void Ui(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        /// <summary>给命令行截图用：直接添加脚本行</summary>
        public void DemoAddScript(string path) => AddScript(path);

        /// <summary>给自动化测试用：跑一轮完整测速</summary>
        public Task DemoRunAsync() => RunAllAsync();

        /// <summary>给自动化测试用：导出当前表格内容</summary>
        public string DemoReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine(_lblSummary.Text);
            foreach (var r in _rows)
                sb.AppendLine($"{r.Rank?.ToString() ?? "-"}|{r.Entry.Name}|{r.Entry.Language}|{r.Result?.Status}|{r.Result?.ElapsedMs}|{r.Result?.Message}");
            return sb.ToString();
        }

        private void UpdateButtons()
        {
            var idle = !_running;
            _btnGenerate.Enabled = idle;
            _btnSaveMaze.Enabled = idle;
            _btnSamples.Enabled = idle;
            _btnAddScripts.Enabled = idle;
            _btnAddFolder.Enabled = idle;
            _btnRemove.Enabled = idle;
            _btnRun.Enabled = idle && _rows.Count > 0;
            _btnStop.Enabled = !idle;
            _numCols.Enabled = idle;
            _numRows.Enabled = idle;
            _numBraid.Enabled = idle;
            _cmbAlgo.Enabled = idle;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            base.OnFormClosing(e);
        }
    }
}
