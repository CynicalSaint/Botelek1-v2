using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Botelek1_v2.Services
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
        private readonly string soundsRoot = GetSoundsRoot() + "\\res\\sounds\\";
        private bool playing = false;
        private List<string> songList = new List<string>();

        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out IAudioClient client))
            {
                return;
            }
            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            var audioClient = await target.ConnectAsync();

            if (ConnectedChannels.TryAdd(guild.Id, audioClient))
            {
                // If you add a method to log happenings from this service,
                // you can uncomment these commented lines to make use of that.
                //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
            }
        }

        public async Task LeaveAudio(IGuild guild)
        {
            if (ConnectedChannels.TryRemove(guild.Id, out IAudioClient client))
            {
                await client.StopAsync();
                //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
            }
        }

        public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string filename)
        {
            string path = soundsRoot + filename;

            if (!File.Exists(path))
            {
                await channel.SendMessageAsync("File does not exist.");
                return;
            }
            if (ConnectedChannels.TryGetValue(guild.Id, out IAudioClient client))
            {
                //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
                using (var ffmpeg = CreateProcess(path))
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        if (songList.Count > 0)
                        {
                            songList.RemoveAt(0);
                        }

                        if (songList.Count > 0)
                        {
                            await channel.SendMessageAsync($"Now playing {filename}, Next Song: {songList[1]}");                            
                        }
                        else
                        {
                            await channel.SendMessageAsync($"Now playing {filename}");
                        }
                        playing = true;
                        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
                    }
                    finally
                    {
                        await stream.FlushAsync();
                        playing = false;

                        if (songList.Count > 0)
                        {
                            await SendAudioAsync(guild, channel, songList[0]);
                        }
                    }
                }
            }
        }

        public async Task AddSongAsync(IGuild guild, IMessageChannel channel, string song)
        {
            songList.Add(song);
            await channel.SendMessageAsync($"{song} has been added to the list. Current List: {string.Join(",", songList.ToArray())}");

            if (!playing)
            {
                await SendAudioAsync(guild, channel, songList[0]);
            }
        }

        private Process CreateProcess(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }

        private static string GetSoundsRoot()
        {
            // Get whether the app is being launched from / (deployed) or /src/Botelek1-v2 (debug)

            var cwd = Directory.GetCurrentDirectory();
            var sln = Directory.GetFiles(cwd).Any(f => f.Contains("Botelek1-v2.sln"));
            return sln ? cwd : Path.Combine(cwd, "..\\..");
        }

        public bool IsPlaying()
        {
            return playing;
        }

        public List<string> getSongList()
        {
            return songList;
        }
    }
}
