using Botelek1_v2.Entities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Botelek1_v2.Modules
{
    public class VideoBanModule : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command("banVideo")]
        public Task BanVideoAsync([Remainder]string url)
        => AddVideoToBanList(url);

        [Command("unbanVideo")]
        public Task UnbanVideoAsync([Remainder]string url)
        => RemoveVideoFromBanList(url);

        private async Task AddVideoToBanList(string url)
        {
            if(!url.Contains("http://") && !url.Contains("https://")) {
                await ReplyAsync($"\"{url}\" is not a valid url.");
                return;
            }

            if(!url.Contains("youtube.com/") && !url.Contains("youtu.be/"))
            {
                await ReplyAsync($"\"{url}\" is not a youtube url.");
                return;
            }

            var videos = Database.GetCollection<Video>("videos");
            var video = videos.FindOne(u => (u.Url == url || u.Url.Contains(url))) ?? new Video { Url = url };

            videos.Upsert(video);

            var messages = await Context.Channel.GetMessagesAsync(1).FlattenAsync();
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

            if (url.Contains("https://"))
            {
                url = url.Substring(8);
            } else
            {
                url = url.Substring(7);
            }

            await ReplyAsync($"Video {url} has been added to the ban list.");
        }

        private async Task RemoveVideoFromBanList(string url)
        {
            var videos = Database.GetCollection<Video>("videos");
            var video = videos.FindOne(u => (u.Url == url || u.Url.Contains(url))) ?? new Video { Id = -1 };

            if (video.Id != -1)
            {
                videos.Delete(video.Id);
                await ReplyAsync($"Video {url} has been removed from the ban list.");
                return;
            }

            await ReplyAsync($"Video {url} was not found in the ban list.");
        }
    }
}
