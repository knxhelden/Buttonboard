using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace BSolutions.Buttonboard.Services.Extensions
{
    /// <summary>
    /// Provides convenience extension methods to safely extract typed values
    /// (string, int, double, bool, object/array) from a scene step's
    /// <c>args</c> dictionary (<c>Dictionary&lt;string, JsonElement&gt;</c>).
    /// These helpers handle missing keys, incompatible value kinds,
    /// and allow specifying sensible fallback values.
    /// </summary>
    public static class StepArgExtensions
    {
        /// <summary>
        /// Retrieves a string value for the given key.
        /// Falls back to the provided default if the key does not exist
        /// or the value cannot be interpreted as a string.
        /// </summary>
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

        /// <summary>
        /// Retrieves an integer value for the given key.
        /// If the element is not a valid integer, the fallback is returned.
        /// Accepts both JSON numbers and numeric strings.
        /// </summary>
        public static int GetInt(this Dictionary<string, JsonElement>? args, string key, int fallback = 0)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return fallback;

            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i)) return i;

            if (el.ValueKind == JsonValueKind.String &&
                int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
                return s;

            return fallback;
        }

        /// <summary>
        /// Retrieves a double value for the given key.
        /// If the element is not a valid double, the fallback is returned.
        /// Accepts both JSON numbers and numeric strings.
        /// </summary>
        public static double GetDouble(this Dictionary<string, JsonElement>? args, string key, double fallback = 0d)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return fallback;

            if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d)) return d;

            if (el.ValueKind == JsonValueKind.String &&
                double.TryParse(el.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
                return s;

            return fallback;
        }

        /// <summary>
        /// Retrieves a boolean value for the given key.
        /// If the element is not a valid boolean, the fallback is returned.
        /// Accepts both JSON booleans and boolean strings.
        /// </summary>
        public static bool GetBool(this Dictionary<string, JsonElement>? args, string key, bool fallback = false)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return fallback;

            if (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False) return el.GetBoolean();

            if (el.ValueKind == JsonValueKind.String &&
                bool.TryParse(el.GetString(), out var b)) return b;

            return fallback;
        }

        /// <summary>
        /// Returns the JsonElement for an object or array if present at the given key.
        /// Returns null if the key is missing or the value is not an object/array.
        /// </summary>
        public static JsonElement? GetNode(this Dictionary<string, JsonElement>? args, string key)
        {
            if (args is null || !args.TryGetValue(key, out var el)) return null;
            return el.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? el : null;
        }
    }
}
