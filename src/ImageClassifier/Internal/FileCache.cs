using System.Buffers;
using System.Collections.Concurrent;

namespace ImageClassifier.Internal;

public class FileCache
{
    private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    private readonly int _maxFileSize;
    private readonly int _maxCachedFileCount;

    private readonly ConcurrentDictionary<string, Entry> _cacheEntries = new();

    private readonly object _lockObject = new();

    public FileCache(int maxFileSize, int maxCachedFileCount)
    {
        _maxFileSize = maxFileSize;
        _maxCachedFileCount = maxCachedFileCount;
    }

    public async ValueTask<bool> TryPrefetchAsync(string filePath, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            if (_cacheEntries.TryGetValue(filePath, out var entry))
            {
                return false;
            }
        }

        using var fileStream = File.Open(filePath, FileMode.Open);
        if (fileStream.Length > _maxFileSize) throw new IOException("too large");

        var memoryOwner = _memoryPool.Rent((int)fileStream.Length);
        await fileStream.ReadExactlyAsync(memoryOwner.Memory[..(int)fileStream.Length], cancellationToken);

        lock (_lockObject)
        {
            var entry = new Entry
            {
                MemoryOwner = memoryOwner,
                Size = (int)fileStream.Length,
                UpdatedTime = DateTime.Now,
            };
            _cacheEntries.TryAdd(filePath, entry);

            if (_cacheEntries.Count > _maxCachedFileCount)
            {
                foreach (var oldEntry in _cacheEntries.OrderBy(n => n.Value.UpdatedTime).Take(_cacheEntries.Count - 100))
                {
                    oldEntry.Value.MemoryOwner.Dispose();
                    _cacheEntries.TryRemove(oldEntry);
                }
            }

            return true;
        }
    }

    public async ValueTask<Stream> GetStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await this.TryPrefetchAsync(filePath);

        lock (_lockObject)
        {
            if (_cacheEntries.TryGetValue(filePath, out var entry))
            {
                entry.UpdatedTime = DateTime.Now;

                var memoryStream = new MemoryStream();
                memoryStream.WriteAsync(entry.MemoryOwner.Memory[..entry.Size], cancellationToken);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return memoryStream;
            }

            throw new IOException("Unexpected Error");
        }
    }

    private record Entry
    {
        public required IMemoryOwner<byte> MemoryOwner { get; init; }
        public required int Size { get; init; }
        public required DateTime UpdatedTime { get; set; }
    }
}
