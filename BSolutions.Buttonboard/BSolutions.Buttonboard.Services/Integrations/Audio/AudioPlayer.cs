using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Integrations.Audio
{
    /// <summary>Starts one headless mpv process per logical audio channel.</summary>
    public sealed class AudioPlayer : IAudioPlayer, IDisposable
    {
        private const string MpvExecutable = "mpv";
        private readonly ILogger<AudioPlayer> _logger;
        private readonly AudioOptions _options;
        private readonly string _mediaRoot;
        private readonly Dictionary<string, Process> _players = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new();

        public AudioPlayer(ILogger<AudioPlayer> logger, ISettingsProvider settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = settings?.Audio ?? throw new ArgumentNullException(nameof(settings));
            _mediaRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _options.MediaFolder));
            Directory.CreateDirectory(_mediaRoot);
        }

        public Task PlayAsync(string file, string channel, int volume, bool loop, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(file)) throw new ArgumentException("file must not be empty", nameof(file));
            if (string.IsNullOrWhiteSpace(channel)) throw new ArgumentException("channel must not be empty", nameof(channel));
            if (volume is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(volume), "volume must be between 0 and 100");

            var path = ResolveMediaPath(file);
            if (!File.Exists(path)) throw new FileNotFoundException($"Audio file '{file}' was not found.", path);

            var startInfo = new ProcessStartInfo
            {
                FileName = MpvExecutable,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--no-video");
            startInfo.ArgumentList.Add("--really-quiet");
            startInfo.ArgumentList.Add($"--volume={volume}");
            startInfo.ArgumentList.Add($"--audio-device={_options.OutputDevice}");
            if (loop) startInfo.ArgumentList.Add("--loop-file=inf");
            startInfo.ArgumentList.Add(path);

            lock (_sync)
            {
                StopChannel(channel);
                var process = Process.Start(startInfo)
                    ?? throw new InvalidOperationException($"Could not start audio player '{MpvExecutable}'.");
                _players[channel] = process;
                process.EnableRaisingEvents = true;
                process.Exited += (_, _) => RemoveExitedPlayer(channel, process);
            }

            _logger.LogInformation(
                "Playing audio file {File} on channel {Channel} (device={OutputDevice}, volume={Volume}, loop={Loop})",
                file, channel, _options.OutputDevice, volume, loop);
            return Task.CompletedTask;
        }

        public Task StopAsync(string channel, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(channel)) throw new ArgumentException("channel must not be empty", nameof(channel));
            lock (_sync) StopChannel(channel);
            return Task.CompletedTask;
        }

        public Task StopAllAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            lock (_sync)
            {
                foreach (var process in _players.Values) StopProcess(process);
                _players.Clear();
            }
            return Task.CompletedTask;
        }

        private string ResolveMediaPath(string file)
        {
            var path = Path.GetFullPath(Path.Combine(_mediaRoot, file));
            var rootPrefix = _mediaRoot.EndsWith(Path.DirectorySeparatorChar)
                ? _mediaRoot
                : _mediaRoot + Path.DirectorySeparatorChar;
            if (!path.StartsWith(rootPrefix, StringComparison.Ordinal))
                throw new ArgumentException("file must be inside the configured audio media folder", nameof(file));
            return path;
        }

        private void StopChannel(string channel)
        {
            if (!_players.Remove(channel, out var process)) return;
            StopProcess(process);
            _logger.LogInformation("Stopped audio channel {Channel}", channel);
        }

        private void RemoveExitedPlayer(string channel, Process process)
        {
            lock (_sync)
            {
                if (_players.TryGetValue(channel, out var current) && ReferenceEquals(current, process))
                    _players.Remove(channel);
            }
            process.Dispose();
        }

        private static void StopProcess(Process process)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            finally { process.Dispose(); }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                foreach (var process in _players.Values) StopProcess(process);
                _players.Clear();
            }
        }
    }
}
