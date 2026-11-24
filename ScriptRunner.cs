using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace MazeRacer
{
    public sealed class ScriptEntry
    {
        public string ScriptPath { get; set; }
        public string Language { get; set; }
        public string CommandTemplate { get; set; }
        public string Hint { get; set; }

        public string Name => Path.GetFileName(ScriptPath);
    }

    public sealed class ScriptRunResult
    {
        public string Status { get; init; } = "等待";
        public string Message { get; init; } = string.Empty;
        public long ElapsedMs { get; init; }
        public bool Valid { get; init; }
        public List<Point> Path { get; init; } = new();
        public int ExitCode { get; init; }
    }

    public static class ScriptRunner
    {
        public static async Task<ScriptRunResult> RunAsync(
            ScriptEntry entry,
            Maze maze,
            string mazeFile,
            int timeoutMs,
            string workDir,
            CancellationToken cancellation)
        {
            Directory.CreateDirectory(workDir);
            var solutionFile = Path.Combine(workDir, "solution.txt");
            if (File.Exists(solutionFile)) File.Delete(solutionFile);

            if (string.IsNullOrWhiteSpace(entry.CommandTemplate))
            {
                return new ScriptRunResult
                {
                    Status = "需配置命令",
                    Message = entry.Hint ?? "该语言没有默认启动命令，请在下方“命令模板”里填写后重试"
                };
            }

            var command = LanguageCatalog.Expand(entry.CommandTemplate, entry.ScriptPath, mazeFile, solutionFile, workDir);
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var stopwatch = Stopwatch.StartNew();

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new ScriptRunResult { Status = "启动失败", Message = ex.Message, ElapsedMs = stopwatch.ElapsedMilliseconds };
            }

            var timedOut = false;
            var canceled = false;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var exitTask = process.WaitForExitAsync(linked.Token);
            var timeoutTask = Task.Delay(timeoutMs, linked.Token);

            try
            {
                var winner = await Task.WhenAny(exitTask, timeoutTask);
                if (winner != exitTask)
                {
                    timedOut = !cancellation.IsCancellationRequested;
                    canceled = cancellation.IsCancellationRequested;
                    KillTree(process);
                    await Task.Delay(60);
                }
                else
                {
                    await exitTask;
                    process.WaitForExit();
                }
            }
            catch (OperationCanceledException)
            {
                canceled = true;
                KillTree(process);
            }
            finally
            {
                stopwatch.Stop();
                linked.Cancel();
            }

            if (timedOut)
            {
                return new ScriptRunResult
                {
                    Status = "超时",
                    Message = $"超过 {timeoutMs} ms 未结束，已强制结束进程",
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                };
            }

            if (cancellation.IsCancellationRequested)
            {
                return new ScriptRunResult
                {
                    Status = "已中止",
                    Message = "用户停止了本轮测速",
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                };
            }

            var elapsed = stopwatch.ElapsedMilliseconds;
            var exitCode = process.ExitCode;

            if (!File.Exists(solutionFile))
            {
                var detail = FirstLine(stderr) ?? FirstLine(stdout) ?? "脚本没有生成 solution.txt";
                return new ScriptRunResult
                {
                    Status = "无结果",
                    Message = detail,
                    ElapsedMs = elapsed,
                    ExitCode = exitCode
                };
            }

            List<Point> path;
            try
            {
                path = SolutionValidator.ReadSolution(solutionFile);
            }
            catch (Exception ex)
            {
                return new ScriptRunResult { Status = "读取失败", Message = ex.Message, ElapsedMs = elapsed, ExitCode = exitCode };
            }

            var validation = SolutionValidator.Validate(maze, path);
            if (!validation.Valid)
            {
                return new ScriptRunResult
                {
                    Status = exitCode == 0 ? "答案错误" : "运行错误",
                    Message = validation.Message + (exitCode != 0 ? $"（退出码 {exitCode}）" : string.Empty),
                    ElapsedMs = elapsed,
                    Path = path,
                    ExitCode = exitCode
                };
            }

            if (exitCode != 0)
            {
                return new ScriptRunResult
                {
                    Status = "答案正确(退出码异常)",
                    Message = $"路径合法，但退出码 {exitCode}",
                    ElapsedMs = elapsed,
                    Valid = true,
                    Path = path,
                    ExitCode = exitCode
                };
            }

            return new ScriptRunResult
            {
                Status = "完成",
                Message = validation.Message,
                ElapsedMs = elapsed,
                Valid = true,
                Path = path,
                ExitCode = exitCode
            };
        }

        private static string FirstLine(StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return null;
            foreach (var line in sb.ToString().Split('\n'))
            {
                var t = line.Trim();
                if (t.Length > 0) return t.Length > 160 ? t[..160] : t;
            }
            return null;
        }

        private static void KillTree(Process process)
        {
            try
            {
                if (!process.HasExited) process.Kill(true);
            }
            catch
            {
                // 进程可能已退出
            }
        }
    }
}
