using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.Media.Imaging;
using ImageClassifier.Internal;
using ImageClassifier.Shared;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ImageClassifier.Windows.Main;

public class MainWindowModelBase
{
    public required ReactivePropertySlim<string> SourcePath { get; init; }
    public required ReactivePropertySlim<string> RightPath { get; init; }
    public required ReactivePropertySlim<string> LeftPath { get; init; }
    public required ReactivePropertySlim<string> DownPath { get; init; }
    public required ReactivePropertySlim<Bitmap?> ImageSource { get; init; }
    public required ReactiveCommand LoadCommand { get; init; }
    public required ReactiveCommand UndoCommand { get; init; }
    public required ReactiveCommand RightCommand { get; init; }
    public required ReactiveCommand LeftCommand { get; init; }
    public required ReactiveCommand DownCommand { get; init; }
    public required ReactivePropertySlim<string> ProgressText { get; init; }
}

public class MainWindowDesignModel : MainWindowModelBase, IDisposable
{
    private CompositeDisposable _disposable = new();

    public MainWindowDesignModel()
    {
        this.SourcePath = new ReactivePropertySlim<string>(@"C:\").AddTo(_disposable);
        this.LeftPath = new ReactivePropertySlim<string>(@"D:\").AddTo(_disposable);
        this.RightPath = new ReactivePropertySlim<string>(@"E:\").AddTo(_disposable);
        this.ImageSource = new ReactivePropertySlim<Bitmap?>().AddTo(_disposable);
        this.LoadCommand = new ReactiveCommand().AddTo(_disposable);
        this.UndoCommand = new ReactiveCommand().AddTo(_disposable);
        this.RightCommand = new ReactiveCommand().AddTo(_disposable);
        this.LeftCommand = new ReactiveCommand().AddTo(_disposable);
        this.DownCommand = new ReactiveCommand().AddTo(_disposable);
        this.ProgressText = new ReactivePropertySlim<string>().AddTo(_disposable);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}

public class MainWindowModel : MainWindowModelBase, IAsyncDisposable
{
    private Stack<string> _loadedFilePathStack = new();
    private Stack<MovedFileHistory> _movedFileHistoryStack = new();
    private string? _currentFilePath = null;
    private FileCache _fileCache = new(1024 * 1024 * 100, 100);

    private Task _backgroundFileLoadTask;
    private AutoResetEvent _changedEvent = new AutoResetEvent(false);

    private CompositeDisposable _disposable = new();
    private CancellationTokenSource _cancellationTokenSource = new();

    public MainWindowModel(AppConfig config)
    {
        _backgroundFileLoadTask = this.BackgroundFileLoadAsync(_cancellationTokenSource.Token);

        this.SourcePath = new ReactivePropertySlim<string>(config.SourcePath ?? string.Empty).AddTo(_disposable);
        this.RightPath = new ReactivePropertySlim<string>(config.RightPath ?? string.Empty).AddTo(_disposable);
        this.LeftPath = new ReactivePropertySlim<string>(config.LeftPath ?? string.Empty).AddTo(_disposable);
        this.DownPath = new ReactivePropertySlim<string>(config.DownPath ?? string.Empty).AddTo(_disposable);
        this.ImageSource = new ReactivePropertySlim<Bitmap?>().AddTo(_disposable);
        this.LoadCommand = new ReactiveCommand().AddTo(_disposable);
        this.LoadCommand.Subscribe(() => this.Load()).AddTo(_disposable);
        this.UndoCommand = new ReactiveCommand().AddTo(_disposable);
        this.UndoCommand.Subscribe(() => this.Undo()).AddTo(_disposable);
        this.RightCommand = new ReactiveCommand().AddTo(_disposable);
        this.RightCommand.Subscribe(() => this.Right()).AddTo(_disposable);
        this.LeftCommand = new ReactiveCommand().AddTo(_disposable);
        this.LeftCommand.Subscribe(() => this.Left()).AddTo(_disposable);
        this.DownCommand = new ReactiveCommand().AddTo(_disposable);
        this.DownCommand.Subscribe(() => this.Down()).AddTo(_disposable);
        this.ProgressText = new ReactivePropertySlim<string>().AddTo(_disposable);

        this.Load();
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();

        await _backgroundFileLoadTask;

        _cancellationTokenSource.Dispose();

        _disposable.Dispose();
    }

    private async Task BackgroundFileLoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            for (; ; )
            {
                await _changedEvent.WaitAsync(cancellationToken);

                foreach (var path in _loadedFilePathStack.ToArray().Take(32))
                {
                    if (await _fileCache.TryPrefetchAsync(path))
                    {
                        Debug.WriteLine($"Loaded: {path}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    private async void Load()
    {
        var sourcePath = this.SourcePath?.Value;
        if (!Directory.Exists(sourcePath)) return;

        var extSet = new HashSet<string>() { ".png", ".jpg", ".jpeg" };
        var tempList = GetFiles(sourcePath).Where(n => extSet.Contains(Path.GetExtension(n).ToLower())).Take(5000).ToList();
        tempList.Sort((x, y) => y.CompareTo(x));

        _loadedFilePathStack.Clear();
        foreach (var path in tempList)
        {
            _loadedFilePathStack.Push(path);
        }

        this.Next();
    }

    private static IEnumerable<string> GetFiles(string sourcePath)
    {
        if (!Directory.Exists(sourcePath)) yield break;

        var files = Directory.GetFiles(sourcePath, "*", SearchOption.TopDirectoryOnly).ToList();
        files.Sort();
        foreach (var f in files)
        {
            yield return f;
        }

        var dirs = Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly).ToList();
        dirs.Sort();
        foreach (var d in dirs)
        {
            foreach (var f in GetFiles(d))
            {
                yield return f;
            }
        }
    }

    private void Undo()
    {
        if (_movedFileHistoryStack.Count == 0) return;

        if (_currentFilePath is not null)
        {
            _loadedFilePathStack.Push(_currentFilePath);
        }

        var history = _movedFileHistoryStack.Pop();
        File.Move(history.DestinationFilePath, history.SourceFilePath);
        _loadedFilePathStack.Push(history.SourceFilePath);

        this.Next();
    }

    private void Right()
    {
        var rightPath = this.RightPath.Value;
        this.Move(rightPath);
        this.Next();
    }

    private void Left()
    {
        var leftPath = this.LeftPath.Value;
        this.Move(leftPath);
        this.Next();
    }

    private void Down()
    {
        var downPath = this.DownPath.Value;
        this.Move(downPath);
        this.Next();
    }

    private void Move(string dirPath)
    {
        if (_currentFilePath is null) return;
        if (!Directory.Exists(dirPath)) return;

        var sourceFilePath = _currentFilePath;
        var destinationFilePath = GenUniqueDestinationFilePath(dirPath, Path.GetFileName(_currentFilePath));

        var history = new MovedFileHistory { SourceFilePath = sourceFilePath, DestinationFilePath = destinationFilePath };
        _movedFileHistoryStack.Push(history);
        File.Move(history.SourceFilePath, history.DestinationFilePath);
    }

    private string GenUniqueDestinationFilePath(string dirPath, string fileName)
    {
        string filePath = Path.Combine(dirPath, fileName);
        if (!File.Exists(filePath)) return filePath;

        for (int i = 0; i < 1024; i++)
        {
            filePath = Path.Combine(dirPath, Path.GetFileNameWithoutExtension(fileName) + $"_{i}" + Path.GetExtension(fileName));
            if (!File.Exists(filePath)) return filePath;
        }

        throw new NotSupportedException();
    }

    private async void Next()
    {
        _changedEvent.Set();

        this.ProgressText.Value = string.Format($"{_movedFileHistoryStack.Count} / {_movedFileHistoryStack.Count + _loadedFilePathStack.Count}");

        string? nextFilePath = null;
        Bitmap? newImageSource = null;

        for (; ; )
        {
            if (_loadedFilePathStack.Count == 0)
            {
                var imageSource = this.ImageSource.Value;
                this.ImageSource.Value = null;
                imageSource?.Dispose();

                _currentFilePath = null;

                return;
            }

            try
            {
                nextFilePath = _loadedFilePathStack.Pop();
                using var stream = await _fileCache.GetStreamAsync(nextFilePath);
                newImageSource = new Bitmap(stream);
                break;
            }
            catch (Exception)
            {

            }
        }

        var oldImageSource = this.ImageSource!.Value;
        this.ImageSource.Value = newImageSource;
        oldImageSource?.Dispose();

        _currentFilePath = nextFilePath;
    }

    private record class MovedFileHistory
    {
        public required string SourceFilePath { get; init; }
        public required string DestinationFilePath { get; init; }
    }
}
