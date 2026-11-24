namespace MazeRacer
{
    public sealed class LanguageInfo
    {
        public string Name { get; init; }
        /// <summary>命令模板，占位符：{script} {input} {output} {dir} {name}；null 表示需要手动填写。</summary>
        public string CommandTemplate { get; init; }
        public bool NeedsCustomCommand { get; init; }
        public string Hint { get; init; }
    }

    public static class LanguageCatalog
    {
        private static readonly Dictionary<string, LanguageInfo> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            [".py"] = Info("Python", "python \"{script}\" \"{input}\" \"{output}\""),
            [".pyw"] = Info("Python", "pythonw \"{script}\" \"{input}\" \"{output}\""),
            [".js"] = Info("JavaScript", "node \"{script}\" \"{input}\" \"{output}\""),
            [".mjs"] = Info("JavaScript", "node \"{script}\" \"{input}\" \"{output}\""),
            [".ts"] = Info("TypeScript", "npx ts-node \"{script}\" \"{input}\" \"{output}\""),
            [".ps1"] = Info("PowerShell", "pwsh -NoProfile -ExecutionPolicy Bypass -File \"{script}\" \"{input}\" \"{output}\""),
            [".bat"] = Info("批处理", "cmd /c \"{script}\" \"{input}\" \"{output}\""),
            [".cmd"] = Info("批处理", "cmd /c \"{script}\" \"{input}\" \"{output}\""),
            [".rb"] = Info("Ruby", "ruby \"{script}\" \"{input}\" \"{output}\""),
            [".php"] = Info("PHP", "php \"{script}\" \"{input}\" \"{output}\""),
            [".lua"] = Info("Lua", "lua \"{script}\" \"{input}\" \"{output}\""),
            [".pl"] = Info("Perl", "perl \"{script}\" \"{input}\" \"{output}\""),
            [".go"] = Info("Go", "go run \"{script}\" \"{input}\" \"{output}\""),
            [".java"] = Info("Java", "java \"{script}\" \"{input}\" \"{output}\""),
            [".kt"] = Info("Kotlin", "kotlin \"{script}\" \"{input}\" \"{output}\""),
            [".cpp"] = Info("C++", "g++ \"{script}\" -O2 -std=c++17 -o \"{dir}\\program.exe\" && \"{dir}\\program.exe\" \"{input}\" \"{output}\""),
            [".cc"] = Info("C++", "g++ \"{script}\" -O2 -std=c++17 -o \"{dir}\\program.exe\" && \"{dir}\\program.exe\" \"{input}\" \"{output}\""),
            [".c"] = Info("C", "gcc \"{script}\" -O2 -o \"{dir}\\program.exe\" && \"{dir}\\program.exe\" \"{input}\" \"{output}\""),
            [".rs"] = Info("Rust", "rustc -O \"{script}\" -o \"{dir}\\program.exe\" && \"{dir}\\program.exe\" \"{input}\" \"{output}\""),
            [".sh"] = Info("Shell", "bash \"{script}\" \"{input}\" \"{output}\""),
            [".r"] = Info("R", "Rscript \"{script}\" \"{input}\" \"{output}\""),
            [".cs"] = Custom("C#", "默认没有通用命令，可按你的环境填，例如：dotnet run -- \"{input}\" \"{output}\""),
            [".swift"] = Custom("Swift", "默认没有通用命令，可填：swift \"{script}\" \"{input}\" \"{output}\""),
        };

        private static LanguageInfo Info(string name, string command) =>
            new() { Name = name, CommandTemplate = command, NeedsCustomCommand = false };

        private static LanguageInfo Custom(string name, string hint) =>
            new() { Name = name, CommandTemplate = null, NeedsCustomCommand = true, Hint = hint };

        public static LanguageInfo Detect(string path)
        {
            var ext = Path.GetExtension(path);
            if (Map.TryGetValue(ext, out var info)) return info;
            return new LanguageInfo
            {
                Name = $"其他({ext})",
                CommandTemplate = null,
                NeedsCustomCommand = true,
                Hint = "未知扩展名，请手动填写启动命令，占位符：{script} {input} {output}"
            };
        }

        public static string Expand(string template, string script, string input, string output, string dir)
        {
            return template
                .Replace("{script}", script)
                .Replace("{input}", input)
                .Replace("{output}", output)
                .Replace("{dir}", dir)
                .Replace("{name}", Path.GetFileName(script));
        }
    }
}
