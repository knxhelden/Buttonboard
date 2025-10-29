using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace BSolutions.Buttonboard.Services.Extensions
{
    /// <summary>
    /// Convenience helpers to extract typed values from <c>args</c>.
    /// </summary>
    public static class StepArgExtensions
    {
        // ── Existing optional getters (unverändert) ─────────────────────────────
        public static string GetString(this Dictionary<string, JsonElement>? args, string key, string fallback = "")
        {
            if (args is null) return fallback;
            if (!args.TryGetValue(key, out var el)) return fallback;

            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? fallback,
                JsonValueKind.Number => el.ToString(),
                JsonValueKind.True or JsonValueKind.False => el.GetBoolean().ToString(),
                _ => fallback
            };
        }

        public static int GetInt(this Dictionary<string, JsonElement>? args, string key, int fallback = 0)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return fallback;

            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i)) return i;

            if (el.ValueKind == JsonValueKind.String &&
                int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
                return s;

            return fallback;
        }

        public static double GetDouble(this Dictionary<string, JsonElement>? args, string key, double fallback = 0d)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return fallback;

            if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d)) return d;

            if (el.ValueKind == JsonValueKind.String &&
                double.TryParse(el.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
                return s;

            return fallback;
        }

        public static bool GetBool(this Dictionary<string, JsonElement>? args, string key, bool fallback = false)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return fallback;

            if (el.ValueKind is JsonValueKind.True or JsonValueKind.False) return el.GetBoolean();

            if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var b)) return b;

            return fallback;
        }

        public static JsonElement? GetNode(this Dictionary<string, JsonElement>? args, string key)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return null;
            return el.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? el : null;
        }

        /// <summary>
        /// Returns the string at <paramref name="key"/> or throws <see cref="ArgumentException"/> if missing/invalid.
        /// </summary>
        public static string GetRequiredString(this Dictionary<string, JsonElement>? args, string key)
        {
            if (args is null) throw new ArgumentException($"Missing required arg '{key}'", key);
            if (!args.TryGetValue(key, out var el)) throw new ArgumentException($"Missing required arg '{key}'", key);

            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? throw new ArgumentException($"Arg '{key}' must be a non-null string", key),
                JsonValueKind.Number => el.ToString(),
                JsonValueKind.True or JsonValueKind.False => el.GetBoolean().ToString(),
                _ => throw new ArgumentException($"Arg '{key}' must be a string-compatible value", key)
            };
        }

        /// <summary>
        /// Returns the int at <paramref name="key"/> or throws <see cref="ArgumentException"/> if missing/invalid.
        /// Accepts JSON numbers and numeric strings.
        /// </summary>
        public static int GetRequiredInt(this Dictionary<string, JsonElement>? args, string key)
        {
            if (args is null) throw new ArgumentException($"Missing required arg '{key}'", key);
            if (!args.TryGetValue(key, out var el)) throw new ArgumentException($"Missing required arg '{key}'", key);

            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i)) return i;

            if (el.ValueKind == JsonValueKind.String &&
                int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
                return s;

            throw new ArgumentException($"Arg '{key}' must be an integer", key);
        }
    }
}
