using System.Text.Json;
using MZikmund.Toolkit.WinUI.Services;
using Windows.Storage;

namespace AppTemplate.Services.Settings;

public sealed class Preferences : IPreferences
{
    private readonly ApplicationDataContainer _container = ApplicationData.Current.LocalSettings;

    public T Get<T>(string key, T defaultValue) =>
        TryGet<T>(key, out var value) ? value : defaultValue;

    public bool TryGet<T>(string key, out T value)
    {
        if (_container.Values.TryGetValue(key, out var stored))
        {
            if (stored is T typed)
            {
                value = typed;
                return true;
            }

            // ApplicationData stores numeric values as object — coerce on read.
            try
            {
                value = (T)Convert.ChangeType(stored, typeof(T));
                return true;
            }
            catch
            {
                // Fall through to the not-found result.
            }
        }

        value = default!;
        return false;
    }

    public void Set<T>(string key, T? value) => _container.Values[key] = value;

    public T GetComplex<T>(string key, T defaultValue) =>
        TryGetComplex<T>(key, out var value) ? value : defaultValue;

    public bool TryGetComplex<T>(string key, out T value)
    {
        if (_container.Values.TryGetValue(key, out var stored) && stored is string json)
        {
            try
            {
                if (JsonSerializer.Deserialize<T>(json) is { } result)
                {
                    value = result;
                    return true;
                }
            }
            catch
            {
                // Fall through to the not-found result.
            }
        }

        value = default!;
        return false;
    }

    public void SetComplex<T>(string key, T? value) => _container.Values[key] = JsonSerializer.Serialize(value);

    public bool ContainsKey(string key) => _container.Values.ContainsKey(key);

    public void Remove(string key) => _container.Values.Remove(key);

    public void Clear() => _container.Values.Clear();
}
