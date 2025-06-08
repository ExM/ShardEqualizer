using MongoDB.Driver;

namespace ShardEqualizer.DAL;

public static class AsyncCursorExtensions
{
    public static async Task<IList<T>> ToList<T>(this Task<IAsyncCursor<T>> cursorTask, CancellationToken token)
    {
        var cursor = await cursorTask;
        return await cursor.ToListAsync(token);
    }
}