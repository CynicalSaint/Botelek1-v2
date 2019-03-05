using System.Threading.Tasks;
using Discord.Commands;

namespace Botelek1_v2.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net 2.0.1\n");
    }
}
