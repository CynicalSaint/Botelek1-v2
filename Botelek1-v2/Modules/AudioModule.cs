using Botelek1_v2.Services;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Botelek1_v2.Modules
{
    public class AudioModule : ModuleBase<ICommandContext>
    {
        private readonly AudioService _service;

        public AudioModule(AudioService service)
        {
            _service = service;
        }

        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Channel.SendMessageAsync("You must either be in a voice channel or pass one as an argument."); return; }

            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        [Command("leave", RunMode = RunMode.Async)]
        public async Task LeaveCmd()
        {
            await _service.LeaveAudio(Context.Guild);
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task PlayCmd([Remainder] string song)
        {
            if (!song.EndsWith(".mp3"))
            {
                song += ".mp3";
            }

            await _service.AddSongAsync(Context.Guild, Context.Channel, song);
        }

        [Command("songList", RunMode = RunMode.Async)]
        public async Task SendSongList()
        {
            await ReplyAsync($"Song List: {string.Join(",", _service.getSongList().ToArray())}");
        }
    }
}
