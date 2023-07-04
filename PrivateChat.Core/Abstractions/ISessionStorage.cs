using Blazored.SessionStorage;

namespace PrivateChat.Core.Abstractions;

public interface ISessionStorage
{
    ValueTask SetItemAsync<T>(string key, T item, CancellationToken cancellationToken = default);

    ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);

    T GetItem<T>(string key);

    ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);

    ValueTask<bool> ContainKey(string key, CancellationToken cancellationToken = default);
}

public class BrowserSessionStorage : ISessionStorage
{
    private readonly ISessionStorageService _localStorageService;
    private readonly ISyncSessionStorageService _syncLocalStorageService;

    public BrowserSessionStorage(ISessionStorageService localStorageService, ISyncSessionStorageService syncLocalStorageService)
    {
        _localStorageService = localStorageService;
        _syncLocalStorageService = syncLocalStorageService;
    }

    public ValueTask SetItemAsync<T>(string key, T item, CancellationToken cancellationToken = default)
    {
        return _localStorageService.SetItemAsync(key, item, cancellationToken);
    }

    public ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return _localStorageService.GetItemAsync<T>(key, cancellationToken);
    }

    public T GetItem<T>(string key)
    {
        return _syncLocalStorageService.GetItem<T>(key);
    }

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        return _localStorageService.RemoveItemAsync(key, cancellationToken);
    }

    public ValueTask<bool> ContainKey(string key, CancellationToken cancellationToken = default)
    {
        return _localStorageService.ContainKeyAsync(key, cancellationToken);
    }
}
