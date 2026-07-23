using System.Globalization;
using System.Text;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// A module's input state in the neutral form used by every persistence route:
/// shareable links today, cloud-saved calculations later.
///
/// Two rules shape this type:
///
/// 1. Only *inputs* are stored, never results. A restored state is re-run through
///    the engine, so a calculation opened from a months-old link reflects the
///    current engine rather than a frozen snapshot. This matters because several
///    engines are still marked IsVerified = false and will change.
///
/// 2. Nothing positional is stored. Materials go in by name, not by their index in
///    MaterialService, because inserting one material would otherwise silently
///    repoint every existing link at the wrong steel.
///
/// Values are held as invariant-culture strings: one code path for doubles, ints
/// and enum-ish strings, and no type ambiguity on the wire.
/// </summary>
public sealed class CalculationState
{
    public const int CurrentVersion = 1;

    /// <summary>Module key, e.g. "key-connection". Matches ModuleInfo.Route without the slash.</summary>
    public string Module { get; }

    /// <summary>State schema version, so a future format change can migrate old links.</summary>
    public int Version { get; }

    private readonly Dictionary<string, string> _values;

    public CalculationState(string module, int version = CurrentVersion)
    {
        Module = module;
        Version = version;
        _values = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private CalculationState(string module, int version, Dictionary<string, string> values)
    {
        Module = module;
        Version = version;
        _values = values;
    }

    public IReadOnlyDictionary<string, string> Values => _values;

    /// <summary>
    /// Rebuilds a state from a plain key/value map — the shape stored in the
    /// `calculations.inputs` jsonb column. The inverse of <see cref="Values"/>.
    /// </summary>
    public static CalculationState FromValues(string module, IDictionary<string, string>? values, int version = CurrentVersion)
    {
        var state = new CalculationState(module, version);
        if (values != null)
            foreach (var (key, value) in values)
                state._values[key] = value;
        return state;
    }

    // ============ WRITING ============

    public CalculationState Set(string key, double value)
    {
        _values[key] = value.ToString("R", CultureInfo.InvariantCulture);
        return this;
    }

    public CalculationState Set(string key, int value)
    {
        _values[key] = value.ToString(CultureInfo.InvariantCulture);
        return this;
    }

    public CalculationState Set(string key, bool value)
    {
        _values[key] = value ? "1" : "0";
        return this;
    }

    public CalculationState Set(string key, string? value)
    {
        _values[key] = value ?? "";
        return this;
    }

    // ============ READING ============
    // Every getter takes a fallback. A link may come from an older build that did
    // not write a key yet, and a missing key must never break the restore.

    public double GetDouble(string key, double fallback = 0)
        => _values.TryGetValue(key, out var raw)
           && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value : fallback;

    public int GetInt(string key, int fallback = 0)
        => _values.TryGetValue(key, out var raw)
           && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : fallback;

    public bool GetBool(string key, bool fallback = false)
        => _values.TryGetValue(key, out var raw) ? raw == "1" : fallback;

    public string GetString(string key, string fallback = "")
        => _values.TryGetValue(key, out var raw) && raw.Length > 0 ? raw : fallback;

    // ============ WIRE FORMAT ============
    //
    //     <version>~<module>~<key>=<value>;<key>=<value>...
    //
    // Deliberately not JSON. The payload is a flat string map, this encoding is
    // roughly half the size once base64'd, and it survives IL trimming without
    // needing a serialiser context.

    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append(Version).Append('~').Append(Uri.EscapeDataString(Module)).Append('~');

        var first = true;
        foreach (var (key, value) in _values)
        {
            if (!first) sb.Append(';');
            sb.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
            first = false;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses the wire format. Returns null for anything malformed — a hand-edited
    /// or truncated link should fall back to an empty form, never throw.
    /// </summary>
    public static CalculationState? Deserialize(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;

        var parts = payload.Split('~', 3);
        if (parts.Length < 3) return null;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
            return null;

        // Reject versions from the future; a newer build wrote a format this one
        // cannot read, and guessing would restore a wrong calculation.
        if (version < 1 || version > CurrentVersion) return null;

        string module;
        try { module = Uri.UnescapeDataString(parts[1]); }
        catch (UriFormatException) { return null; }

        if (module.Length == 0) return null;

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (parts[2].Length > 0)
        {
            foreach (var pair in parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var split = pair.Split('=', 2);
                if (split.Length != 2) continue;

                try
                {
                    values[Uri.UnescapeDataString(split[0])] = Uri.UnescapeDataString(split[1]);
                }
                catch (UriFormatException)
                {
                    // Skip the damaged pair; the rest of the state is still usable.
                }
            }
        }

        return new CalculationState(module, version, values);
    }
}
