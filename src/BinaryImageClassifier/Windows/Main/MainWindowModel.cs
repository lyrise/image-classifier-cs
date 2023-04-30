using System.Reactive.Disposables;
using Avalonia.Media.Imaging;
using BinaryImageClassifier.Configuration;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BinaryImageClassifier.Windows.Main;

public interface IMainWindowModel
{
    ReactivePropertySlim<string> SourcePath { get; }
    ReactivePropertySlim<string> LeftPath { get; }
    ReactivePropertySlim<string> RightPath { get; }
    ReactivePropertySlim<Bitmap?> ImageSource { get; }
    ReactiveCommand LoadCommand { get; }
    ReactiveCommand UndoCommand { get; }
    ReactiveCommand LeftCommand { get; }
    ReactiveCommand RightCommand { get; }
    ReactiveCommand DownCommand { get; }
}

public class MainWindowModel : IAsyncDisposable, IMainWindowModel
{
    private Stack<string> _loadedFilePathStack = new();
    private Stack<MovedFileHistory> _movedFileHistoryStack = new();
    private string? _currentFilePath = null;

    private CompositeDisposable _disposable = new();

    public MainWindowModel(AppConfig config)
    {
        this.SourcePath = new ReactivePropertySlim<string>(config.SourcePath ?? string.Empty).AddTo(_disposable);
        this.LeftPath = new ReactivePropertySlim<string>(config.LeftPath ?? string.Empty).AddTo(_disposable);
        this.RightPath = new ReactivePropertySlim<string>(config.RightPath ?? string.Empty).AddTo(_disposable);
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
    }
    public ReactivePropertySlim<string> SourcePath { get; }
    public ReactivePropertySlim<string> LeftPath { get; }
    public ReactivePropertySlim<string> RightPath { get; }
    public ReactivePropertySlim<string> DownPath { get; }
    public ReactivePropertySlim<Bitmap?> ImageSource { get; }
    public ReactiveCommand LoadCommand { get; }
    public ReactiveCommand UndoCommand { get; }
    public ReactiveCommand LeftCommand { get; }
    public ReactiveCommand RightCommand { get; }
    public ReactiveCommand DownCommand { get; }

    private async void Load()
    {
        var sourcePath = this.SourcePath.Value;
        if (!Directory.Exists(sourcePath)) return;

        var extSet = new HashSet<string>() { ".png", ".jpg", ".jpeg" };
        var tempList = GetFiles(sourcePath).Where(n => extSet.Contains(Path.GetExtension(n).ToLower())).Take(10000).ToList();
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

        var history = new MovedFileHistory(sourceFilePath, destinationFilePath);
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

    private void Next()
    {
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
                using var fileStream = File.Open(nextFilePath, FileMode.Open);
                if (fileStream.Length > 1024 * 1024 * 100) continue;
                newImageSource = new Bitmap(fileStream);
                break;
            }
            catch (Exception)
            {

            }
        }

        var oldImageSource = this.ImageSource.Value;
        this.ImageSource.Value = newImageSource;
        oldImageSource?.Dispose();

        _currentFilePath = nextFilePath;
    }

    public async ValueTask DisposeAsync()
    {
        _disposable.Dispose();
    }

    private record class MovedFileHistory
    {
        public MovedFileHistory(string sourceFilePath, string destinationFilePath)
        {
            this.SourceFilePath = sourceFilePath;
            this.DestinationFilePath = destinationFilePath;
        }

        public string SourceFilePath { get; }
        public string DestinationFilePath { get; }
    }
}
