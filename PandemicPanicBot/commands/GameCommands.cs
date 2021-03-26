using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PandemicPanicBot.Commands
{
    class GameCommands : BaseCommandModule
    {
        // A helper function in which we check for a voice channel with 4 or more players
        // if the condition is fulfilled, then we start a game
        public async Task CheckVoiceAndInvokeStartGame(CommandContext ctx)
        {
            DiscordChannel ChosenChannel = null;
            foreach (DiscordChannel d in ctx.Guild.Channels.Values)
            {
                // If the channel's parent belong to the voice channel
                if (d.Parent != null && d.Parent.Name == "Voice Channels")
                {
                    Console.WriteLine($"Voice channel ({d.Users.Count()} members): {d.Name}");

                    // If this voice channel has 4 or more users, then invoke PlayGame
                    if (d.Users.Count() >= 4)
                    {
                        Console.WriteLine("There are 4 or more players, can start game.");
                        ChosenChannel = d;
                        break;
                    }
                }
            }
            if (ChosenChannel != null)
            {
                // Can start a game
                Console.WriteLine("Starting game...");
                DiscordEmoji check = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                await ctx.Message.CreateReactionAsync(check).ConfigureAwait(false);
                await GLStaticWrapper.GLClass.PlayGame(ctx, ChosenChannel);
            }
            else
            {
                // Unable to start a game.
                Console.WriteLine("Not enough players to start game");
                DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                await ctx.RespondAsync("Not enough players in voice channels to start a game.").ConfigureAwait(false);
            }
        }

        [Command("sg")]
        [Aliases("startgame")]
        [Description("Start a game of PanicPandemic as long as there are at least 4 people in a voice channel." +
            "\nOnly one game can be ran at a time.")]
        public async Task StartG(CommandContext ctx)
        {
            if (GLStaticWrapper.GLClass.Round == 0)
            {
                await Task.Run(async () =>
                {
                    await CheckVoiceAndInvokeStartGame(ctx);
                });
            }
            else
            {
                // Game is currently in session; invalid command.
                Console.WriteLine("Game has already started!");
                DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                await ctx.RespondAsync("The game is currently in session.").ConfigureAwait(false);
            }
        }

        [Command("srg")]
        [Aliases("stopresetgame")]
        [Description("Restart the game of PanicPandemic if the game is in progress. Include the word 'false' afterwards to stop the game.")]
        public async Task StopResetG(CommandContext ctx,
            [Description("True/False value on whether the game will be restarted after stopping")] bool ResetAfterStop = true)
        {
            if (GLStaticWrapper.GLClass.Round != 0)
            {
                await Task.Run(async () =>
                {
                    // Game is currently in session; stop the game.
                    Console.WriteLine("Resetting game...");
                    GLStaticWrapper.GLClass.StopGame();

                    // Restart the game if true
                    if (ResetAfterStop)
                        await CheckVoiceAndInvokeStartGame(ctx);
                    else
                    {
                        // Otherwise send an successful emote
                        DiscordEmoji check = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                        await ctx.Message.CreateReactionAsync(check).ConfigureAwait(false);
                    }
                });
            }
            else
            {
                await Task.Run(async () =>
                {
                    // No game is currently in session; invalid command.
                    Console.WriteLine("No game is in session");
                    DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                    await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                    await ctx.RespondAsync("No game is in session.").ConfigureAwait(false);
                });
            }
        }

        [Command("c")]
        [Aliases("check")]
        [Description("Check if you've guessed the right word.")]
        public async Task Check(CommandContext ctx,
            [Description("The 1 word you're checking.")] string guess)
        {
            if (GLStaticWrapper.GLClass.Round != 0 && GLStaticWrapper.GLClass.CurrentMinigame != null && GLStaticWrapper.GLClass.GenericTimerActive == false)
            {
                if (GLStaticWrapper.GLClass.IsPlayer(ctx))
                {
                    await Task.Run(async () =>
                    {
                        if (((ctx.Member == GLStaticWrapper.GLClass.Team1Lead || ctx.Member == GLStaticWrapper.GLClass.Team2Lead) && GLStaticWrapper.GLClass.CurrentMinigame.WhoHasInfo == InfoShare.RegularScientists ||
                            ((ctx.Member != GLStaticWrapper.GLClass.Team1Lead && ctx.Member != GLStaticWrapper.GLClass.Team2Lead) && GLStaticWrapper.GLClass.CurrentMinigame.WhoHasInfo == InfoShare.HeadScientist)))
                        {
                            bool correct = GLStaticWrapper.GLClass.CheckWord(ctx, guess);
                            if (correct)
                            {
                                DiscordEmoji check = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                                await ctx.Message.CreateReactionAsync(check).ConfigureAwait(false);
                            }
                            else
                            {
                                DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                                await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                            await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                            await ctx.Message.RespondAsync("You can't guess the word if you were given the word.");
                        }
                    });
                }
                else
                {
                    DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                    await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                    await ctx.RespondAsync("You're not a player in the current game.").ConfigureAwait(false);
                }
            }
            else
            {
                // No game is currently in session; invalid command.
                Console.WriteLine("The game hasn't started yet!");
                DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                await ctx.RespondAsync("The game hasn't started yet!").ConfigureAwait(false);
            }
        }
    }
}
