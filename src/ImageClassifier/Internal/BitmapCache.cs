using System.Buffers;
using System.Collections.Concurrent;
using Avalonia.Media.Imaging;

namespace ImageClassifier.Internal;

public class BitmapCache
{
    private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    private readonly int _maxFileSize;
    private readonly int _maxCachedFileCount;

    private readonly ConcurrentDictionary<string, Entry> _cacheEntries = new();

    private readonly NeoSmart.AsyncLock.AsyncLock _asyncLock = new NeoSmart.AsyncLock.AsyncLock();
    private readonly object _lockObject = new();

    public BitmapCache(int maxFileSize, int maxCachedFileCount)
    {
        _maxFileSize = maxFileSize;
        _maxCachedFileCount = maxCachedFileCount;
    }

    public async ValueTask<bool> TryPrefetchAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
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

            var image = new Bitmap(fileStream);

            lock (_lockObject)
            {
                var entry = new Entry
                {
                    Image = image,
                    UpdatedTime = DateTime.Now,
                };
                _cacheEntries.TryAdd(filePath, entry);

                if (_cacheEntries.Count > _maxCachedFileCount)
                {
                    foreach (var oldEntry in _cacheEntries.OrderBy(n => n.Value.UpdatedTime).Take(_cacheEntries.Count - 100))
                    {
                        oldEntry.Value.Image.Dispose();
                        _cacheEntries.TryRemove(oldEntry);
                    }
                }

                return true;
            }
        }
    }

    public async ValueTask<Bitmap> GetImageAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            await this.TryPrefetchAsync(filePath);

            lock (_lockObject)
            {
                if (_cacheEntries.TryGetValue(filePath, out var entry))
                {
                    entry.UpdatedTime = DateTime.Now;
                    return entry.Image;
                }

                throw new IOException("Unexpected Error");
            }
        }
    }

    private record Entry
    {
        public required Bitmap Image { get; init; }
        public required DateTime UpdatedTime { get; set; }
    }
}
