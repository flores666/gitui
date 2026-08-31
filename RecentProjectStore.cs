namespace GitUi;

internal static class RecentProjectStore
{
    public const int Limit = 8;

    private static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GitUi",
        "recent-projects.txt");

    public static IEnumerable<string> Load() => File.Exists(FilePath)
        ? File.ReadLines(FilePath).Where(Directory.Exists).Take(Limit)
        : [];

    public static void Save(IEnumerable<string> projects)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
        File.WriteAllLines(FilePath, projects);
    }
}
