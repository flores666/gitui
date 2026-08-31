using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace GitUi;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    private GitRepository? _repository;
    private FileRow? _selectedFile;
    private string _projectPath = "No project selected";
    private bool _loadingChanges;
    private bool _projectsOpen;

    public MainWindow()
    {
        InitializeComponent();
        foreach (string path in RecentProjectStore.Load())
            RecentProjects.Add(path);
        DataContext = this;
    }

    internal ObservableCollection<FileRow> Files { get; } = [];
    internal ObservableCollection<DiffBlock> DiffBlocks { get; } = [];
    internal ObservableCollection<string> RecentProjects { get; } = [];
    internal FileRow? SelectedFile { get => _selectedFile; set => Set(ref _selectedFile, value); }
    public string ProjectPath { get => _projectPath; private set => Set(ref _projectPath, value); }
    public string Summary => Files.Count == 0 ? "No local changes" : $"{Files.Count} changed files";
    public bool HasProject => _repository is not null;
    public bool ProjectsOpen { get => _projectsOpen; private set => Set(ref _projectsOpen, value); }

    private void ToggleProjects(object? sender, RoutedEventArgs e) => ProjectsOpen = !ProjectsOpen;

    private void CloseProjects(object? sender, PointerPressedEventArgs e) => ProjectsOpen = false;

    private async void AddProject(object? sender, RoutedEventArgs e)
    {
        ProjectsOpen = false;
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Choose a Git project", AllowMultiple = false });
        if (folders.Count == 0 || folders[0].TryGetLocalPath() is not { } path)
            return;

        await OpenProjectAsync(path);
    }

    private async void OpenRecentProject(object? sender, RoutedEventArgs e)
    {
        ProjectsOpen = false;
        if (sender is Button { DataContext: string path })
            await OpenProjectAsync(path);
    }

    private async Task OpenProjectAsync(string path)
    {
        try
        {
            _repository = await GitRepository.OpenAsync(path);
            ProjectPath = _repository.Path;
            RecentProjects.Remove(_repository.Path);
            RecentProjects.Insert(0, _repository.Path);
            while (RecentProjects.Count > RecentProjectStore.Limit)
                RecentProjects.RemoveAt(RecentProjects.Count - 1);
            RecentProjectStore.Save(RecentProjects);
            OnPropertyChanged(nameof(HasProject));
            await LoadChangesAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async void Refresh(object? sender, RoutedEventArgs e) => await LoadChangesAsync();

    private async void SelectFile(object? sender, SelectionChangedEventArgs e)
    {
        if (!_loadingChanges && _repository is not null && SelectedFile is not null)
            await LoadDiffAsync();
    }

    private async Task LoadChangesAsync()
    {
        if (_repository is null)
            return;

        try
        {
            FileRow? selected = SelectedFile;
            _loadingChanges = true;
            Files.Clear();
            foreach (ChangedFile file in await _repository.GetChangesAsync())
                Files.Add(FileRow.From(file));

            OnPropertyChanged(nameof(Summary));
            SelectedFile = Files.FirstOrDefault(file => file.File.Name == selected?.File.Name) ?? Files.FirstOrDefault();
            _loadingChanges = false;
            await LoadDiffAsync();
        }
        catch (Exception exception)
        {
            _loadingChanges = false;
            await ShowErrorAsync(exception.Message);
        }
    }

    private async Task LoadDiffAsync()
    {
        DiffBlocks.Clear();
        if (_repository is null || SelectedFile is null)
            return;

        foreach (GitDiffBlock block in await _repository.GetDiffAsync(SelectedFile.File))
            DiffBlocks.Add(DiffBlock.From(block));
    }

    private async void RevertChange(object? sender, RoutedEventArgs e)
    {
        if (_repository is null || sender is not Button { DataContext: DiffBlock { Hunk: { } hunk } })
            return;

        if (!await ConfirmRevertAsync())
            return;

        try
        {
            await _repository.RevertHunkAsync(hunk);
            await LoadChangesAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new Window { Title = "Git Changes", Width = 420, Height = 160, CanResize = false };
        var closeButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        closeButton.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                closeButton
            }
        };
        await dialog.ShowDialog(this);
    }

    private async Task<bool> ConfirmRevertAsync()
    {
        var dialog = new Window { Title = "Undo change", Width = 400, Height = 170, CanResize = false };
        var cancel = new Button { Content = "Cancel" };
        var undo = new Button { Content = "Undo", Background = new SolidColorBrush(Color.Parse("#F8E7E8")) };
        cancel.Click += (_, _) => dialog.Close(false);
        undo.Click += (_, _) => dialog.Close(true);
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = "Undo this change? This action cannot be recovered by the application." },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Children = { cancel, undo }
                }
            }
        };
        return await dialog.ShowDialog<bool>(this);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        OnPropertyChanged(name);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed record FileRow(ChangedFile File, IBrush Foreground, IBrush Background)
{
    public string Name => File.Name;
    public string Status => File.Status.ToString();

    public static FileRow From(ChangedFile file)
    {
        (string foreground, string background) = file.Status switch
        {
            ChangeStatus.New or ChangeStatus.Added => ("#357A49", "#E7F4EA"),
            ChangeStatus.Modified => ("#8A6500", "#F8F0D8"),
            ChangeStatus.Deleted => ("#A34A4E", "#F8E7E8"),
            ChangeStatus.Renamed => ("#386FA4", "#E7F0F8"),
            _ => ("#7357A6", "#EFEAF8")
        };
        return new(file, Brush(foreground), Brush(background));
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
}

internal sealed record DiffBlock(IReadOnlyList<DiffLine> Lines, GitHunk? Hunk, string? Info)
{
    public bool CanUndo => Hunk is not null;

    public static DiffBlock From(GitDiffBlock block) =>
        new(block.Lines.Select(DiffLine.From).ToArray(), block.Hunk, block.Info);
}

internal sealed record DiffLine(string Text, IBrush Foreground, IBrush Background)
{
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#3F3F46"));
    private static readonly IBrush AddedText = new SolidColorBrush(Color.Parse("#426B4B"));
    private static readonly IBrush AddedBackground = new SolidColorBrush(Color.Parse("#EAF4EC"));
    private static readonly IBrush RemovedText = new SolidColorBrush(Color.Parse("#875052"));
    private static readonly IBrush RemovedBackground = new SolidColorBrush(Color.Parse("#F8ECEC"));
    private static readonly IBrush Transparent = Brushes.Transparent;

    public static DiffLine From(string line) => line switch
    {
        ['+', ..] => new(line, AddedText, AddedBackground),
        ['-', ..] => new(line, RemovedText, RemovedBackground),
        _ => new(line, TextBrush, Transparent)
    };
}
