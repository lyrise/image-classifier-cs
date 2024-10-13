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
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
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

            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var image = new Bitmap(memoryStream);

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
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.TryPrefetchAsync(filePath).ConfigureAwait(false);

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
