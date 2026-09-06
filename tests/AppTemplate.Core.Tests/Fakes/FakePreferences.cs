using MZikmund.Toolkit.WinUI.Services;

namespace AppTemplate.Core.Tests.Fakes;

/// <summary>In-memory <see cref="IPreferences"/>; complex values are stored as-is, not serialized.</summary>
internal sealed class FakePreferences : IPreferences
{
    private readonly Dictionary<string, object?> _values = [];

    public bool ClearCalled { get; private set; }

    public T Get<T>(string key, T defaultValue) => TryGet<T>(key, out var value) ? value : defaultValue;

    public bool TryGet<T>(string key, out T value)
    {
        if (_values.TryGetValue(key, out var stored) && stored is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    public void Set<T>(string key, T? value) => _values[key] = value;

    public T GetComplex<T>(string key, T defaultValue) => Get(key, defaultValue);

    public bool TryGetComplex<T>(string key, out T value) => TryGet(key, out value);

    public void SetComplex<T>(string key, T? value) => Set(key, value);

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public void Remove(string key) => _values.Remove(key);

    public void Clear()
    {
        ClearCalled = true;
        _values.Clear();
    }
}
