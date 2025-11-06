using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BSolutions.Buttonboard.Services.Loaders
{
    /// <summary>
    /// Parser für die Minimal-DSL:
    /// 
    /// Syntax:
    ///   name: <Freitext>                // optional
    ///   group <Name> = item1, item2, ... // beliebig viele Gruppen
    ///   <time> <action> [target] [key=value ...]
    ///
    /// Regeln:
    ///   - <time>: "ss" oder "mm:ss"
    ///   - Kommentare: Zeilen beginnend mit "//" oder "#"
    ///   - Gleiche Zeitstempel -> parallel
    ///   - target kann Einzel-Item, Gruppenname oder Slice "Group[a-b]" (1-basiert, inkl.) sein
    ///   - MQTT: target -> args.topic
    ///   - Video: target -> args.player
    ///   - GPIO: wenn kein led= gesetzt ist und target vorhanden -> args.led = target
    /// </summary>
    public static class SceneDslParser
    {
        private static readonly Regex RxGroupDef = new(@"^\s*group\s+(?<name>[A-Za-z0-9_\-]+)\s*=\s*(?<rest>.+)$", RegexOptions.Compiled);
        private static readonly Regex RxNameHeader = new(@"^\s*name\s*:\s*(?<name>.+)$", RegexOptions.Compiled);
        private static readonly Regex RxSlice = new(@"^(?<g>[A-Za-z0-9_\-]+)\[(?<a>\d+)\s*-\s*(?<b>\d+)\]$", RegexOptions.Compiled);

        /// <summary>
        /// Parst den DSL-Text in ein <see cref="ScenarioAssetDefinition"/> mit expandierten Steps.
        /// </summary>
        /// <param name="text">DSL-Inhalt.</param>
        /// <param name="key">Dateiname ohne Extension (Fallback für Name/Kind-Erkennung).</param>
        /// <param name="setupKey">Setup-Key (z. B. "setup"), um Kind zu bestimmen.</param>
        /// <param name="logger">Optionaler Logger.</param>
        public static ScenarioAssetDefinition ParseToAssetDefinition(
            string text,
            string key,
            string setupKey,
            ILogger? logger = null)
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var steps = new List<ScenarioStepDefinition>();
            string? sceneName = null;

            var lines = NormalizeMultilineGroups(text).Split('\n');

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("//") || line.StartsWith("#")) continue;

                // "name:"-Header
                var mName = RxNameHeader.Match(line);
                if (mName.Success)
                {
                    sceneName = mName.Groups["name"].Value.Trim();
                    continue;
                }

                // Group-Definition
                var mg = RxGroupDef.Match(line);
                if (mg.Success)
                {
                    var name = mg.Groups["name"].Value.Trim();
                    var rest = mg.Groups["rest"].Value.Trim();
                    var items = SplitCsv(rest).ToList();
                    groups[name] = items;
                    continue;
                }

                // Timeline-Zeile: "<time> <action> [target] [k=v ...]"
                var tokens = Tokenize(line);
                if (tokens.Count < 2)
                    continue;

                var tStr = tokens[0];
                var action = tokens[1];
                var atMs = ParseTimeToMs(tStr);

                string? targetToken = null;
                int argStartIndex;

                // 3. Token ist optionales target (nur wenn es kein "key=value" ist)
                if (tokens.Count >= 3 && !tokens[2].Contains('='))
                {
                    targetToken = tokens[2];
                    argStartIndex = 3;
                }
                else
                {
                    argStartIndex = 2;
                }

                var args = ParseArgs(tokens, argStartIndex);

                var expandedTargets = ExpandTarget(targetToken, groups);
                if (expandedTargets == null || expandedTargets.Count == 0)
                {
                    // Kein Target -> Einzel-Step (Args wie angegeben)
                    steps.Add(MakeStep(action, atMs, args, inferFromAction: true, singleTarget: null));
                }
                else
                {
                    // Für jedes expandierte Ziel eigener Step mit abgeleiteten Args
                    foreach (var item in expandedTargets)
                    {
                        var perItemArgs = new Dictionary<string, JsonElement>(args, StringComparer.OrdinalIgnoreCase);

                        var domain = GetDomain(action);
                        switch (domain)
                        {
                            case "mqtt":
                                perItemArgs["topic"] = JsonSerializer.SerializeToElement(item);
                                break;
                            case "video":
                                perItemArgs["player"] = JsonSerializer.SerializeToElement(item);
                                break;
                            case "gpio":
                                if (!perItemArgs.ContainsKey("led"))
                                    perItemArgs["led"] = JsonSerializer.SerializeToElement(item);
                                break;
                            default:
                                if (!perItemArgs.ContainsKey("target"))
                                    perItemArgs["target"] = JsonSerializer.SerializeToElement(item);
                                break;
                        }

                        steps.Add(MakeStep(action, atMs, perItemArgs, inferFromAction: false, singleTarget: item));
                    }
                }
            }

            // Sortierung & Form
            steps = steps
                .Where(s => !string.IsNullOrWhiteSpace(s.Action))
                .OrderBy(s => s.AtMs)
                .ToList();

            return new ScenarioAssetDefinition
            {
                Name = string.IsNullOrWhiteSpace(sceneName) ? key : sceneName,
                Kind = string.Equals(key, setupKey, StringComparison.OrdinalIgnoreCase)
                    ? ScenarioAssetKind.Setup
                    : ScenarioAssetKind.Scene,
                Steps = steps
            };
        }

        // --------- Helpers ---------

        private static ScenarioStepDefinition MakeStep(
            string action,
            int atMs,
            IDictionary<string, JsonElement> args,
            bool inferFromAction,
            string? singleTarget)
        {
            var step = new ScenarioStepDefinition
            {
                Name = BuildFriendlyName(action, args, singleTarget),
                AtMs = atMs,
                Action = action,
                Args = new Dictionary<string, JsonElement>(args, StringComparer.OrdinalIgnoreCase)
            };

            if (inferFromAction)
            {
                var domain = GetDomain(action);
                if (domain == "mqtt")
                {
                    EnsureKey(step.Args!, "topic");
                    EnsureKey(step.Args!, "payload");
                }
                else if (domain == "video")
                {
                    if (action.EndsWith(".playitem", StringComparison.OrdinalIgnoreCase))
                    {
                        EnsureKey(step.Args!, "player");
                        EnsureKey(step.Args!, "position");
                    }
                    else if (action.EndsWith(".pause", StringComparison.OrdinalIgnoreCase))
                    {
                        EnsureKey(step.Args!, "player");
                    }
                }
                else if (domain == "audio")
                {
                    EnsureKey(step.Args!, "url");
                    // player optional je nach Implementierung
                }
                else if (domain == "gpio")
                {
                    // gpio.on/off -> led
                    EnsureKey(step.Args!, "led");
                }
            }

            return step;
        }

        private static void EnsureKey(IDictionary<string, JsonElement> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                // Null-JsonElement erzeugen
                dict[key] = JsonSerializer.SerializeToElement((string?)null);
            }
        }

        private static string BuildFriendlyName(string action, IDictionary<string, JsonElement> args, string? item)
        {
            var domain = GetDomain(action);

            if (!string.IsNullOrEmpty(item))
            {
                if (domain == "video")
                    return $"{item}: {action.Replace("video.", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant()}";
                if (domain == "mqtt")
                    return $"{action} → {item}";
                if (domain == "gpio")
                    return $"{action} {item}";
            }

            return action;
        }

        private static List<string>? ExpandTarget(string? token, IDictionary<string, List<string>> groups)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            // Exakter Gruppenname
            if (groups.TryGetValue(token, out var full))
                return full;

            // Slice: Group[a-b]
            var m = RxSlice.Match(token);
            if (m.Success)
            {
                var g = m.Groups["g"].Value;
                if (!groups.TryGetValue(g, out var arr))
                    throw new InvalidOperationException($"Unknown group '{g}' in slice.");

                var a = int.Parse(m.Groups["a"].Value, CultureInfo.InvariantCulture);
                var b = int.Parse(m.Groups["b"].Value, CultureInfo.InvariantCulture);
                if (a < 1 || b < a || b > arr.Count)
                    throw new InvalidOperationException($"Invalid slice {token} for group '{g}' with {arr.Count} elements.");

                return arr.Skip(a - 1).Take(b - a + 1).ToList();
            }

            // Kein Gruppenname -> einzelnes Item
            return new List<string> { token };
        }

        private static Dictionary<string, JsonElement> ParseArgs(List<string> tokens, int startIndex)
        {
            var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            for (int i = startIndex; i < tokens.Count; i++)
            {
                var t = tokens[i];
                var eq = t.IndexOf('=');
                if (eq <= 0) continue;

                var k = t[..eq].Trim();
                var v = t[(eq + 1)..].Trim();

                // Quotes entfernen
                if ((v.StartsWith('"') && v.EndsWith('"')) || (v.StartsWith('\'') && v.EndsWith('\'')))
                    v = v[1..^1];

                JsonElement je;
                if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv))
                    je = JsonSerializer.SerializeToElement(iv);
                else if (bool.TryParse(v, out var bv))
                    je = JsonSerializer.SerializeToElement(bv);
                else if (string.Equals(v, "null", StringComparison.OrdinalIgnoreCase))
                    je = JsonSerializer.SerializeToElement((string?)null);
                else
                    je = JsonSerializer.SerializeToElement(v);

                dict[k] = je;
            }

            return dict;
        }

        private static int ParseTimeToMs(string t)
        {
            // "ss" oder "mm:ss"
            if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secondsOnly))
                return secondsOnly * 1000;

            var parts = t.Split(':');
            if (parts.Length == 2)
            {
                var m = int.Parse(parts[0], CultureInfo.InvariantCulture);
                var s = int.Parse(parts[1], CultureInfo.InvariantCulture);
                return (m * 60 + s) * 1000;
            }

            throw new FormatException($"Invalid time format: '{t}'. Use 'ss' or 'mm:ss'.");
        }

        private static string NormalizeMultilineGroups(string text)
        {
            // Erlaubt, Gruppen über mehrere Zeilen zu schreiben (Ende mit Komma).
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var acc = new List<string>();
            string? current = null;

            foreach (var l in lines)
            {
                var line = l;
                if (current == null)
                {
                    current = line;
                }
                else
                {
                    if (current.TrimEnd().EndsWith(","))
                        current = current + " " + line.Trim();
                    else
                    {
                        acc.Add(current);
                        current = line;
                    }
                }
            }

            if (current != null)
                acc.Add(current);

            return string.Join("\n", acc);
        }

        private static IEnumerable<string> SplitCsv(string s)
        {
            // Simple CSV (keine escaped Kommas innerhalb von Quotes nötig für diese DSL)
            return s.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0);
        }

        private static List<string> Tokenize(string line)
        {
            // Split by whitespace, aber Quotes zusammenhalten
            var tokens = new List<string>();
            bool inQuotes = false;
            char? quoteChar = null;
            var cur = new List<char>();

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    cur.Add(c);
                    if (c == quoteChar) { inQuotes = false; quoteChar = null; }
                }
                else
                {
                    if (c == '"' || c == '\'')
                    {
                        inQuotes = true; quoteChar = c; cur.Add(c);
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        if (cur.Count > 0) { tokens.Add(new string(cur.ToArray())); cur.Clear(); }
                    }
                    else
                    {
                        cur.Add(c);
                    }
                }
            }

            if (cur.Count > 0)
                tokens.Add(new string(cur.ToArray()));

            return tokens;
        }

        private static string GetDomain(string action)
        {
            var dot = action.IndexOf('.');
            return dot > 0 ? action[..dot].ToLowerInvariant() : action.ToLowerInvariant();
        }
    }
}
