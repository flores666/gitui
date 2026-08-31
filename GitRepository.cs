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

    public async Task<string> GetDiffAsync(ChangedFile file)
    {
        if (file.Status == ChangeStatus.New)
            return string.Join('\n', (await File.ReadAllLinesAsync(System.IO.Path.Combine(Path, file.Name)))
                .Select((line, index) => $"+ {index + 1,4}  {line}"));

        string staged = await RunAsync("diff", "--cached", "--no-color", "--", file.Name);
        string working = await RunAsync("diff", "--no-color", "--", file.Name);
        return string.Join('\n', new[] { staged, working }.Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private async Task<string> RunAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Не удалось запустить Git.");
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(error.Trim());

        return output;
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

internal enum ChangeStatus { New, Added, Modified, Deleted, Renamed, Staged }
