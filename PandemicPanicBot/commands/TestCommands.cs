using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace PandemicPanicBot.Commands
{
    class TestCommands : BaseCommandModule
    {
        // Uncomment any of the code for debugging purposes

        // cure generation debugging
        /*
        [Command("gcure")]
        [Description("DEBUG: Generates a cure (check console)")]
        public async Task GCure(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                GLStaticWrapper.GLClass.GenerateCure(true);
                Console.WriteLine($"Team 1 cure: {GLStaticWrapper.GLClass.GetTeamCure(1)}");
                Console.WriteLine($"Team 2 cure: {GLStaticWrapper.GLClass.GetTeamCure(2)}");
            });
            await ctx.RespondAsync("Generated cures. Check the console.").ConfigureAwait(false);
        }
        */

        // cure generation debugging #2
        /*
        [Command("D_getcure")]
        [Description("DEBUG: Fetches a cure")]
        public async Task GetCure(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                Console.WriteLine($"Cure: {GLStaticWrapper.GLClass.GetCure()}");
            });
            await ctx.RespondAsync($"Retrieved cure. Check the console.").ConfigureAwait(false);
        }
        */

        /*
        [Command("PlayersOnline")]
        [Description("Check who is currently in queue for a game.")]
        public async Task Players(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                Console.Write("Players: ");
                GLStaticWrapper.GLClass.PrintPlayers();
            });
        }
        */

        /*
        [Command("Join")]
        [Description("Join a queue for a PanicPandemic game")]
        public async Task Join(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                if (GLStaticWrapper.GLClass.AllPlayers.ContainsKey(ctx.Member))
                    await ctx.RespondAsync($"{ctx.Member.Mention}, you are already in the game!").ConfigureAwait(false);
                else
                {
                    GLStaticWrapper.GLClass.AllPlayers.Add(ctx.Member, -1);
                    await ctx.RespondAsync($"Added {ctx.Member.Mention} to the players!\nCurrent have {GLStaticWrapper.GLClass.AllPlayers.Count} players!").ConfigureAwait(false);
                }
            });
        }
        */

        /*
        [Command("Leave")]
        [Description("Leave a queue for a PanicPandemic game")]
        public async Task Leave(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                if (GLStaticWrapper.GLClass.AllPlayers.ContainsKey(ctx.Member))
                {
                    // Return 1/2 if assigned to team, otherwise returns -1
                    int TeamNum = GLStaticWrapper.GLClass.AllPlayers[ctx.Member];

                    GLStaticWrapper.GLClass.AllPlayers.Remove(ctx.Member);
                    if (TeamNum == 1)
                        GLStaticWrapper.GLClass.Team1.Remove(ctx.Member);
                    else if (TeamNum == 2)
                        GLStaticWrapper.GLClass.Team2.Remove(ctx.Member);
                    await ctx.RespondAsync($"{ctx.Member.Mention} have successfully left the game").ConfigureAwait(false);
                }
                else
                    await ctx.RespondAsync($"{ctx.Member.Mention}, you weren't even in the game :upside_down:").ConfigureAwait(false);
            });

        }
        */

        /*
        [Command("AssignTeam")]
        [Description("DEBUG: Shuffle Team")]
        public async Task AssignTeam(CommandContext ctx)
        {
            await ctx.RespondAsync("Assigning Team...").ConfigureAwait(false);
            await Task.Run(async () =>
            {
                GLStaticWrapper.GLClass.GenerateTeam();

                await ctx.Channel.SendMessageAsync("Team1:").ConfigureAwait(false);
                foreach (var member in GLStaticWrapper.GLClass.Team1)
                {
                    Console.WriteLine(member.Username);
                    await ctx.Channel.SendMessageAsync(member.Username).ConfigureAwait(false);
                }

                await ctx.Channel.SendMessageAsync("\nTeam2:").ConfigureAwait(false);
                foreach (var member in GLStaticWrapper.GLClass.Team2)
                {
                    Console.WriteLine(member.Username);
                    await ctx.Channel.SendMessageAsync(member.Username).ConfigureAwait(false);
                }
            });
        }
        */

        [Command("D_getimage")]
        [Description("DEBUG: Retrieve one image given a word")]
        public async Task GetImage(CommandContext ctx,
            [Description("The name of the image you're getting")]  string word)
        {
            await Task.Run(async () =>
            {
                string image = GLStaticWrapper.GLClass.NounClass.FetchImagePath(word);
                Console.WriteLine(image);
                // If image is not invalid
                if (image != "")
                {
                    await ctx.RespondWithFileAsync(image).ConfigureAwait(false);
                    DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                    await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                }
                else
                {
                    DiscordEmoji x = DiscordEmoji.FromName(ctx.Client, ":x:");
                    await ctx.Message.CreateReactionAsync(x).ConfigureAwait(false);
                }
            });
        }

        /*
        [Command("checkv")]
        [Description("DEBUG: check whoever exists in a voice channel")]
        public async Task CheckVoice(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                IReadOnlyDictionary<ulong, DiscordChannel> ServerChannels = ctx.Guild.Channels;
                foreach (DiscordChannel d in ServerChannels.Values)
                {
                    // If the channel's parent belong to the voice channel
                    if (d.Parent != null && d.Parent.Name == "Voice Channels")
                    {
                        Console.WriteLine($"Voice channel: {d.Name}");
                        // This is the voice channel we're checking
                        foreach (DiscordMember m in d.Users)
                        {
                            Console.WriteLine($"Member: {m.DisplayName}");
                        }
                    }
                }
            });
            //await ctx.Member.SendMessageAsync("bruh this is a direct message");
        }
        */

        /*
        [Command("movev")]
        [Description("DEBUG: create a TEMP voice channel (testing) and move any users in voice channel into it")]
        public async Task MoveV(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                int number = 1;
                IReadOnlyDictionary<ulong, DiscordChannel> ServerChannels = ctx.Guild.Channels;
                foreach (DiscordChannel d in ServerChannels.Values)
                {
                    // If the channel's parent belong to the voice channel
                    if (d.Parent != null && d.Parent.Name == "Voice Channels")
                    {
                        Console.WriteLine($"Voice channel: {d.Name}");
                        // This is the voice channel we're checking

                        // Create a new temp voice channel
                        DiscordChannel result = await ctx.Guild.CreateChannelAsync($"TEMP {number}", DSharpPlus.ChannelType.Voice, d.Parent);
                        foreach (DiscordMember m in d.Users)
                        {
                            Console.WriteLine($"Moving member: {m.DisplayName} to new channel");
                            await result.PlaceMemberAsync(m);
                        }
                        number++;
                    }
                }
            });
        }
        */

        [Command("D_deletev")]
        [Description("DEBUG: delete all voice channels that start with TEMP")]
        public async Task DeleteV(CommandContext ctx)
        {
            await Task.Run(async () =>
            {
                IReadOnlyDictionary<ulong, DiscordChannel> ServerChannels = ctx.Guild.Channels;
                foreach (DiscordChannel d in ServerChannels.Values)
                {
                    // If the channel's parent belong to the voice channel
                    if (d.Parent != null && d.Parent.Name == "Voice Channels")
                    {
                        if (d.Name.StartsWith("TEAM"))
                        {
                            await d.DeleteAsync("Deleting team channel b/c it's a temp voice channel.");
                        }
                    }
                }
            });
        }
    }
}
