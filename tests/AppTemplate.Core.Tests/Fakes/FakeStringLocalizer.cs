using Microsoft.Extensions.Localization;

namespace AppTemplate.Core.Tests.Fakes;

/// <summary>
/// Localizer whose table the test supplies. An unknown key resolves to the key itself, which
/// keeps unrelated lookups (such as the page title) from needing setup.
/// </summary>
internal sealed class FakeStringLocalizer(IDictionary<string, string>? values = null) : IStringLocalizer
{
    private readonly Dictionary<string, string> _values =
        values is null ? [] : new Dictionary<string, string>(values);

    public LocalizedString this[string name] =>
        new(name, Resolve(name), resourceNotFound: !_values.ContainsKey(name));

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(Resolve(name), arguments), resourceNotFound: !_values.ContainsKey(name));

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _values.Select(pair => new LocalizedString(pair.Key, pair.Value));

    private string Resolve(string name) => _values.TryGetValue(name, out var value) ? value : name;
}
