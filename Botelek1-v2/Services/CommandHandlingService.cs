using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Botelek1_v2.Entities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.Configuration;

namespace Botelek1_v2.Services
{
    public class CommandHandlingService
    {

        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfiguration _config;
        private IServiceProvider _provider;
        private LiteDatabase _database;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, IConfiguration config, CommandService commands, LiteDatabase database)
        {
            _discord = discord;
            _config = config;
            _commands = commands;
            _provider = provider;
            _database = database;

            _discord.MessageReceived += MessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;
            if (!(message.HasMentionPrefix(_discord.CurrentUser, ref argPos) || message.HasStringPrefix(_config["CmdPrefix"], ref argPos)))
            {
                if (message.Content.Contains("https://") || message.Content.Contains("http://"))
                {
                    var bannedVideos = _database.GetCollection<Video>("videos");

                    var links = message.Content.Split("\t\n ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(s => s.StartsWith("http://") || s.StartsWith("https://"));
                    foreach (string link in links)
                    {
                        if (bannedVideos.Find(Query.Contains("Url", link)).ToList().Count > 0)
                        {
                            await message.DeleteAsync();
                        }
                    }
                }

                return;
            }

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue &&
                result.Error.Value == CommandError.UnknownCommand)
                return;

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ToString());

            // Add points to the user for using the bot
            // Do this asynchronously, on another task, to prevent database access (and levelup notifications?) from halting the bot
            _ = UpdateLevelAsync(context);
        }

        private Task UpdateLevelAsync(SocketCommandContext context)
        {
            var users = _database.GetCollection<User>("users");
            var user = users.FindOne(u => u.Id == context.User.Id) ?? new User { Id = context.User.Id };
            ++user.Points;
            users.Upsert(user);

            // If sending a levelup notification, flag this Task as async and remove the following line
            return Task.CompletedTask;
        }
    }
}
