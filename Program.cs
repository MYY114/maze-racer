using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

namespace MazeRacer
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length >= 3 && args[0] == "--gen")
            {
                RunGenerate(args[1], args[2]);
                return;
            }

            if (args.Length >= 5 && args[0] == "--bench")
            {
                RunBench(args[1], int.Parse(args[2]), args[3], args.Skip(4).ToArray())
                    .GetAwaiter().GetResult();
                return;
            }

            if (args.Length >= 2 && args[0] == "--ui-snapshot")
            {
                RunUiSnapshot(args[1]);
                return;
            }

            if (args.Length >= 3 && args[0] == "--ui-demo")
            {
                RunUiDemo(args[1], args.Skip(2).ToArray());
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        /// <summary>--gen "列,行,种子,算法索引,环路%" 输出文件</summary>
        private static void RunGenerate(string spec, string outputPath)
        {
            var parts = spec.Split(',', StringSplitOptions.TrimEntries);
            var cols = int.Parse(parts[0]);
            var rows = int.Parse(parts[1]);
            var seed = int.Parse(parts[2]);
            var algoIndex = parts.Length > 3 ? int.Parse(parts[3]) : 0;
            var braid = parts.Length > 4 ? int.Parse(parts[4]) : 0;
            var algo = algoIndex == 1 ? MazeGenerator.AlgoPrim : MazeGenerator.AlgoDfs;
            var maze = MazeGenerator.Generate(cols, rows, seed, algo, braid);
            maze.SaveAscii(outputPath);
        }

        /// <summary>--bench 迷宫文件 超时ms 结果文件 脚本1 脚本2 …</summary>
        private static async Task RunBench(string mazeFile, int timeoutMs, string outputFile, string[] scripts)
        {
            var sb = new StringBuilder();
            try
            {
                var maze = Maze.Parse(File.ReadAllText(mazeFile));
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var workRoot = Path.Combine(Path.GetTempPath(), "MazeRacer", "bench_" + stamp);

                var results = new List<(string name, string language, ScriptRunResult result)>();
                var tasks = new List<Task>();
                foreach (var script in scripts)
                {
                    var info = LanguageCatalog.Detect(script);
                    var entry = new ScriptEntry
                    {
                        ScriptPath = script,
                        Language = info.Name,
                        CommandTemplate = info.CommandTemplate,
                        Hint = info.Hint
                    };
                    var index = results.Count + 1;
                    var workDir = Path.Combine(workRoot, $"{index:00}_{Path.GetFileNameWithoutExtension(script)}");
                    results.Add((entry.Name, entry.Language, null));

                    var slot = results.Count - 1;
                    tasks.Add(Task.Run(async () =>
                    {
                        var result = await ScriptRunner.RunAsync(entry, maze, mazeFile, timeoutMs, workDir, CancellationToken.None);
                        results[slot] = (entry.Name, entry.Language, result);
                    }));
                }

                await Task.WhenAll(tasks);

                var ranked = results.Where(r => r.result is { Valid: true })
                    .OrderBy(r => r.result.ElapsedMs).ToList();
                sb.AppendLine($"maze={maze.Cols}x{maze.Rows} scripts={scripts.Length} valid={ranked.Count}");
                for (var i = 0; i < ranked.Count; i++)
                {
                    var r = ranked[i];
                    sb.AppendLine($"#{i + 1}\t{r.name}\t{r.language}\t{r.result.ElapsedMs}ms\t{r.result.Path.Count} steps");
                }
                foreach (var r in results.Where(r => r.result is not { Valid: true }))
                {
                    sb.AppendLine($"--\t{r.name}\t{r.language}\t{r.result.Status}\t{r.result.Message}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("ERROR: " + ex);
            }
            File.WriteAllText(outputFile, sb.ToString(), new UTF8Encoding(false));
        }

        /// <summary>--ui-demo out.png 脚本1 脚本2 …：跑完整 UI 流程并截图，同时把排名写到 out.png.txt</summary>
        private static void RunUiDemo(string outputImage, string[] scripts)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var form = new MainForm();
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, -4000);
            form.Show();
            Application.DoEvents();

            foreach (var script in scripts) form.DemoAddScript(script);
            Application.DoEvents();

            var done = false;
            Exception error = null;
            _ = form.DemoRunAsync().ContinueWith(t =>
            {
                if (t.IsFaulted) error = t.Exception;
                done = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            var deadline = DateTime.Now.AddMinutes(3);
            while (!done && DateTime.Now < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }
            Application.DoEvents();

            using (var bmp = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(outputImage, ImageFormat.Png);
            }

            var report = form.DemoReport() + (error != null ? Environment.NewLine + "ERROR: " + error : string.Empty);
            File.WriteAllText(outputImage + ".txt", report, new UTF8Encoding(false));
            form.Close();
        }

        /// <summary>--ui-snapshot out.png：把主界面画成图片，用于 README</summary>
        private static void RunUiSnapshot(string outputPath)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var form = new MainForm();
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, -4000);
            form.Show();
            Application.DoEvents();

            var samples = Path.Combine(AppContext.BaseDirectory, "samples");
            if (Directory.Exists(samples))
            {
                foreach (var file in Directory.GetFiles(samples).Take(6))
                    form.DemoAddScript(file);
            }
            Application.DoEvents();

            using var bmp = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
            bmp.Save(outputPath, ImageFormat.Png);
            form.Close();
        }
    }
}

