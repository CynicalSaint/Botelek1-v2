using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Botelek1_v2.Services;
using LiteDB;
using System.Linq;

namespace Botelek1_v2
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private IConfiguration _config;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _config = BuildConfig();

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["Token"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                // Logging
                .AddLogging()
                .AddSingleton<LogService>()
                // Extra
                .AddSingleton(_config)
                .AddSingleton(new LiteDatabase("bot.db"))
                // Add additional services here...
                .BuildServiceProvider();
        }

        private IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(GetConfigRoot() + "/res/")
                .AddJsonFile("config.json")
                .Build();
        }

        public static string GetConfigRoot()
        {
            // Get whether the app is being launched from / (deployed) or /src/Botelek1 (debug)

            var cwd = Directory.GetCurrentDirectory();
            var sln = Directory.GetFiles(cwd).Any(f => f.Contains("Botelek1-v2.sln"));
            return sln ? cwd : Path.Combine(cwd, "../..");
        }
    }
}
