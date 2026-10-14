using Microsoft.AspNetCore.Http;

namespace FarmaciaSmart.Tests.Fakes;

/// <summary>Minimal in-memory ISession for testing session-backed services without a real request pipeline.</summary>
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = [];

    public bool IsAvailable => true;
    public string Id => "test-session";
    public IEnumerable<string> Keys => _store.Keys;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear() => _store.Clear();
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}
