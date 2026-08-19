using LiteDB;

namespace Rogarion.Services.Persistence;

public sealed class LiteDbContext : IDisposable
{
    private readonly LiteDatabase _database;
    private bool _disposed;

    public LiteDbContext(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _database = new LiteDatabase(databasePath);
    }

    public ILiteCollection<T> GetCollection<T>(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _database.GetCollection<T>(name);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _database.Dispose();
        _disposed = true;
    }
}
