using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using PandemicPanicBot.Commands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;

namespace PandemicPanicBot
{
    static class GLStaticWrapper
    {
        // Only one current game at a time.
        // This is a monostate function b/c the bot can only send so many messages
        // at once before the rate of messages it sends will get throttled (see DsharpPlus rate limit compliancy)
        //
        // The game itself relies upon the bot being able to send many messages at once (distributing the information or just responding
        // to users), so having multiple instances of the game running is not currently feasible with the architecture we have at the moment.
        public static GameLogic GLClass;
    }

    public class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public CommandsNextExtension Commands { get; private set; }

        public async Task RunAsync()
        {
            // Asynchronously runs the bot
            var json = string.Empty;

            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            };

            Client = new DiscordClient(config);
            Client.Ready += OnClientReady;

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(1)
            });

            // Set up commands
            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.Prefix },
                EnableMentionPrefix = true,
                // Only allow invoking the bot in the server
                // but bot can send messages to you
                EnableDms = false,
                DmHelp = true
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            // Register commands
            //Commands.RegisterCommands<TestCommands>(); // used for debugging + experimental purposes
            Commands.RegisterCommands<GameCommands>();

            await Client.ConnectAsync();

            // The await below, will keep the Bot on as long as the program is running.
            await Task.Delay(-1);
        }

        private Task OnClientReady(DiscordClient d, ReadyEventArgs e)
        {
            // The bot is ready, run any additional commands here.

            // Instantiate the GameLogic class
            GLStaticWrapper.GLClass = new GameLogic();
            if (GLStaticWrapper.GLClass.AllMinigames.Count > 0)
                Console.WriteLine("The minigames are loaded from minigames.json file into GameLogic class.");
            if (GLStaticWrapper.GLClass.NounClass.All_nouns.Count > 0)
                Console.WriteLine("The words are loaded from words.json file into Words class.");

            Console.WriteLine("The bot is ready.");

            return Task.CompletedTask;
        }
    }
}
