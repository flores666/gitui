using System.Diagnostics;

namespace GitUi;

internal sealed class GitRepository(string path)
{
    public string Path { get; } = path;

    public static async Task<GitRepository> OpenAsync(string path)
    {
        var repository = new GitRepository(path);
        string root = (await repository.RunAsync("rev-parse", "--show-toplevel")).Trim();
        return new GitRepository(root);
    }

    public async Task<IReadOnlyList<ChangedFile>> GetChangesAsync()
    {
        string[] entries = Split(await RunAsync("status", "--porcelain=v1", "-z", "--untracked-files=all"));
        var files = new List<ChangedFile>();

        for (int index = 0; index < entries.Length; index++)
        {
            string entry = entries[index];
            if (entry.Length < 4)
                continue;

            string state = entry[..2];
            files.Add(new ChangedFile(entry[3..], GetStatus(state)));
            if (state.Contains('R') || state.Contains('C'))
                index++;
        }

        return files.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<IReadOnlyList<GitDiffLine>> GetDiffAsync(ChangedFile file)
    {
        if (file.Status == ChangeStatus.New)
            return (await File.ReadAllLinesAsync(System.IO.Path.Combine(Path, file.Name)))
                .Select((line, index) => new GitDiffLine($"+ {index + 1,4}  {line}", null))
                .ToArray();

        string staged = await RunAsync("diff", "--cached", "--no-color", "--", file.Name);
        string working = await RunAsync("diff", "--no-color", "--", file.Name);
        var lines = new List<GitDiffLine>();
        AddPatch(lines, staged, true);
        AddPatch(lines, working, false);
        return lines;
    }

    public async Task RevertHunkAsync(GitHunk hunk)
    {
        var arguments = new List<string> { "apply", "--reverse", "--whitespace=nowarn" };
        if (hunk.IsStaged)
            arguments.Add("--index");
        arguments.Add("-");
        await RunAsync(arguments, hunk.Patch);
    }

    private Task<string> RunAsync(params string[] arguments) => RunAsync(arguments, null);

    private async Task<string> RunAsync(IReadOnlyList<string> arguments, string? input)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = input is not null,
            UseShellExecute = false
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Git.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        if (input is not null)
        {
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
        }
        await process.WaitForExitAsync();
        string standardOutput = await output;
        string standardError = await error;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(standardError.Trim());

        return standardOutput;
    }

    private static void AddPatch(ICollection<GitDiffLine> output, string patch, bool isStaged)
    {
        if (string.IsNullOrWhiteSpace(patch))
            return;

        string[] lines = patch.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n');
        int headerLength = Array.FindIndex(lines, line => line.StartsWith("@@ ", StringComparison.Ordinal));
        if (headerLength < 0)
        {
            foreach (string line in lines)
                output.Add(new GitDiffLine(line, null));
            return;
        }

        for (int index = 0; index < lines.Length; index++)
        {
            GitHunk? hunk = null;
            if (index >= headerLength && lines[index].StartsWith("@@ ", StringComparison.Ordinal))
            {
                int end = Array.FindIndex(lines, index + 1, line => line.StartsWith("@@ ", StringComparison.Ordinal));
                if (end < 0)
                    end = lines.Length;
                string hunkPatch = string.Join('\n', lines[..headerLength].Concat(lines[index..end])) + "\n";
                hunk = new GitHunk(hunkPatch, isStaged);
            }
            output.Add(new GitDiffLine(lines[index], hunk));
        }
    }

    private static string[] Split(string value) => value.Split('\0', StringSplitOptions.RemoveEmptyEntries);

    private static ChangeStatus GetStatus(string state) => state switch
    {
        "??" => ChangeStatus.New,
        _ when state.Contains('D') => ChangeStatus.Deleted,
        _ when state.Contains('R') => ChangeStatus.Renamed,
        _ when state.Contains('A') => ChangeStatus.Added,
        _ when state[0] != ' ' && state[1] == ' ' => ChangeStatus.Staged,
        _ => ChangeStatus.Modified
    };
}

internal sealed record ChangedFile(string Name, ChangeStatus Status);
internal sealed record GitDiffLine(string Text, GitHunk? Hunk);
internal sealed record GitHunk(string Patch, bool IsStaged);

internal enum ChangeStatus { New, Added, Modified, Deleted, Renamed, Staged }
