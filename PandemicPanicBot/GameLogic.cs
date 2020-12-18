using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Timers;

namespace PandemicPanicBot
{
    // Round noun category
    public enum RoundCategory
    {
        Animals,
        Artificial,
        Bodyparts,
        FruitVegetables,
        NaturalWorld,
        Occupations,
        NoCategory
    }

    // Information sharing type
    // a.k.a who should know the "cure" for the round
    public enum InfoShare
    {
        HeadScientist,
        RegularScientists,
        Everybody,
        Nobody
    }

    // Type of information being shared
    // primarily image/ vs chat
    public enum InfoType
    {
        None,
        Chat,
        Image
    }

    public class GameLogic
    {
        // Handles the scoring and logic for a PanicPandemic Game

        // The current timer object
        private static int Second; // the current second in a round
        private static Timer roundTimer; // keep tracks of the time length (max: 3 minutes)
        private static Timer genericTimer; // for pregame (~5 seconds), instruction (~15 seconds) and intermissions (~30 seconds?)
        private static int RoundResult = -1; // -1 tie, 0 in progress, 1 Team 1, 2 Team 2

        // The current DiscordChannel associated with this game
        public DiscordChannel VoiceChannel { get; private set; }

        // Stores the noun class
        public Nouns NounClass { get; private set; }

        // The color that was used in the last embed (this indicates that this is a different 
        private DiscordColor LastColorUsed;
        private int LastColorIndexUsed;

        // Every single Discord color
        public List<DiscordColor> AllColors { get; private set; }

        // Current roundBool, round, cure position (0->2), who recieves secret info, and minigame
        public bool GenericTimerActive { get; private set; } = false;
        public bool RoundInProgress { get; private set; } = false;
        public int Round { get; private set; } = 0;
        public int CurrentCurePosition { get; private set; } = -1;
        public Minigame CurrentMinigame { get; private set; } = null;

        private readonly int MaxRounds = 3; // how many rounds there are

        // All_players store player name and the team they're on, -1 
        // if they're not assigned to any team
        public Dictionary<DiscordMember, int> AllPlayers { get; private set; } = new Dictionary<DiscordMember, int>();

        // Store all minigames
        public List<Minigame> AllMinigames { get; private set; } = new List<Minigame>();
        public HashSet<Minigame> PlayedMinigames { get; private set; } = new HashSet<Minigame>();

        // Team variables
        public List<DiscordMember> Team1 { get; private set; } = new List<DiscordMember>();
        public List<DiscordMember> Team2 { get; private set; } = new List<DiscordMember>();
        public int Team1Score { get; private set; } = 0;
        public int Team2Score { get; private set; } = 0;
        public List<Tuple<string, RoundCategory>> Cure { get; private set; } = new List<Tuple<string, RoundCategory>>();

        public DiscordMember Team1Lead { get; private set; } = null;
        public DiscordMember Team2Lead { get; private set; } = null;

        // Default constructor
        public GameLogic()
        {
            // Add all words from words.json
            string json = File.ReadAllText("nouns.json");
            NounClass = JsonConvert.DeserializeObject<Nouns>(json);
            NounClass.AppendAllNouns();

            // Add all minigames from minigames.json
            json = File.ReadAllText("minigames.json");
            AllMinigames = JsonConvert.DeserializeObject<List<Minigame>>(json);

            // Initialize ALL colors
            AllColors = new List<DiscordColor>{
                DiscordColor.Aquamarine,
                DiscordColor.Azure,
                DiscordColor.Black,
                DiscordColor.Blue,
                DiscordColor.Blurple,
                DiscordColor.Brown,
                DiscordColor.Chartreuse,
                DiscordColor.CornflowerBlue,
                DiscordColor.Cyan,
                DiscordColor.DarkBlue,
                DiscordColor.DarkButNotBlack,
                DiscordColor.DarkGray,
                DiscordColor.DarkGreen,
                DiscordColor.DarkRed,
                DiscordColor.Gold,
                DiscordColor.Goldenrod,
                DiscordColor.Gray,
                DiscordColor.Grayple,
                DiscordColor.Green,
                DiscordColor.HotPink,
                DiscordColor.IndianRed,
                DiscordColor.LightGray,
                DiscordColor.Lilac,
                DiscordColor.Magenta,
                DiscordColor.MidnightBlue,
                DiscordColor.NotQuiteBlack,
                DiscordColor.Orange,
                DiscordColor.PhthaloBlue,
                DiscordColor.PhthaloGreen,
                DiscordColor.Purple,
                DiscordColor.Red,
                DiscordColor.Rose,
                DiscordColor.SapGreen,
                DiscordColor.Sienna,
                DiscordColor.SpringGreen,
                DiscordColor.Teal,
                DiscordColor.Turquoise,
                DiscordColor.VeryDarkGray,
                DiscordColor.Violet,
                DiscordColor.Wheat,
                DiscordColor.White,
                DiscordColor.Yellow
            };
        }

        // Stops a game and resets all variables
        public void StopGame()
        {
            StopRoundTimer();
            StopGenericTimer();
            Round = Team1Score = Team2Score = 0;
            RoundResult = 0;
            CurrentCurePosition = -1;
            AllPlayers.Clear();
            Team1.Clear();
            Team2.Clear();
            Cure.Clear();
            PlayedMinigames.Clear();
            Team1Lead = Team2Lead = null;
            VoiceChannel = null;
            CurrentMinigame = null;
            RoundInProgress = GenericTimerActive = false;
            LastColorIndexUsed = -1;
            LastColorUsed = DiscordColor.None;
        }

        // The main play function, which assumes there are enough players to play (work in progress!!!)
        public async Task PlayGame(CommandContext ctx, DiscordChannel voice)
        {
            CurrentCurePosition++; // CurrentCurePosition is now 0
            Round++; // round is now 1
            VoiceChannel = voice;

            // Add the users in the current voice channel
            foreach (DiscordMember m in VoiceChannel.Users)
                AllPlayers.Add(m, -1);

            GenerateTeam();
            GenerateCure(true);

            string T1 = "**Team1:** ";
            string T2 = "**Team2:** ";

            foreach (DiscordMember m in Team1)
                T1 += $"{m.DisplayName} ";
            T1.TrimEnd(' ');
            foreach (DiscordMember m in Team2)
                T2 += $"{m.DisplayName} ";
            T2.TrimEnd(' ');

            // Send an embedded message describing the teams
            await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Title = $"<Welcome to Pandemic Panic!>",
                Description = $"As a deadly disease ravages the globe, you and your team of scientists must work together to find The Cure.\n" +
                              $"You will be split into two teams, each with an even number of people. During each round, one person from each team " +
                              $"will be chosen as the Head Scientist, who must coordinate their team to find a word that represents the next part of The Cure.\n" +
                              $"Guess the word before the other team and you win the round! The first to guess three words wins!\n" +
                              $"__**Be sure to check your DMs for rules of each round**__ (click the discord icon on the top left and look in your messages).\n" +
                              $"If you have a guess and are on the answering side, submit your answer with\n**!c ANSWER** in the chat. All words are singular.",
                Color = DiscordColor.Aquamarine
            }.Build());

            // Send an embedded message describing the teams
            await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Title = $"<TEAM FORMATIONS>",
                Description = $"{T1}\n{T2}",
                Color = DiscordColor.Aquamarine
            }.Build());

            // Starts pre-game timer (for players to get their bearings ~30 seconds)
            Console.WriteLine("Pre-game timer started.");

            StartGenericTimer(ctx,
                            " before game starts.",
                            "The game is now starting.", 30);
            GenericTimerActive = true;

            while (GenericTimerActive)
            { } // Blocks until Intermission ends

            Console.WriteLine("Pre-game period ended.");

            // Stop when either team has a score of MaxRounds (default: 3-1 = 2)
            while (Team1Score < (MaxRounds - 1) && Team2Score < (MaxRounds - 1))
            {
                PickMinigame();

                // Choose the head scientists
                PickHeadScientists(); // comment this if you're testing with only one person

                PickRoundColor();

                // Move everyone into seperate channels
                DiscordChannel T1Channel = await ctx.Guild.CreateChannelAsync("TEAM 1", DSharpPlus.ChannelType.Voice, VoiceChannel.Parent);
                DiscordChannel T2Channel = await ctx.Guild.CreateChannelAsync("TEAM 2", DSharpPlus.ChannelType.Voice, VoiceChannel.Parent);

                foreach (DiscordMember m in VoiceChannel.Users)
                {
                    int ChannelNum = AllPlayers[m];
                    Console.WriteLine($"Moving member: {m.DisplayName} to Team channel {ChannelNum}");
                    if (AllPlayers[m] == 1)
                        await T1Channel.PlaceMemberAsync(m);
                    else if (AllPlayers[m] == 2)
                        await T2Channel.PlaceMemberAsync(m);
                }

                Console.WriteLine("Distributing info");

                // Send an embedded message describing the minigame and the round
                await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = $"<Round: {Round} | Category: {NounClass.TypeOfWord(Cure[CurrentCurePosition].Item2)}>",
                    Description = $"__Gamemode: {CurrentMinigame.Name}__\n" +
                                  $"**Team 1's head scientist:** {Team1Lead.Mention}\n" + //comment the following if testing with only one person
                                  $"**Team 2's head scientist:** {Team2Lead.Mention}\n" + //comment the following if testing with only one person
                                  CurrentMinigame.Description,
                    Color = LastColorUsed
                }.Build());

                // Distribute infomation and instructions for each player
                foreach (DiscordMember m in AllPlayers.Keys)
                {
                    // Determine who recieves what information with CurrentMinigame.WhoHasInfo and CurrentMinigame.TypeOfInfo
                    if (m == Team1Lead)
                    {
                        // Team1 Head scientist
                        if (CurrentMinigame.WhoHasInfo == InfoShare.HeadScientist)
                        {
                            // Team1 Head scientist have information
                            if (CurrentMinigame.TypeOfInfo == InfoType.Chat)
                            {
                                // Team1 Head scientist have information (chat)
                                await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                {
                                    Title = $"You are Team1's head scientist. Your word is '{Cure[CurrentCurePosition].Item1}'",
                                    Description = CurrentMinigame.InformedInstructions,
                                    Color = LastColorUsed
                                }.Build());
                            }
                            else if (CurrentMinigame.TypeOfInfo == InfoType.Image)
                            {
                                // Team1 Head scientist have information (image)
                                await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                {
                                    Title = $"You are Team1's head scientist. Your word ({NounClass.TypeOfWord(Cure[CurrentCurePosition].Item2)}) is" +
                                    " associated with the image below",
                                    Description = CurrentMinigame.InformedInstructions,
                                    Color = LastColorUsed
                                }.Build());
                                await m.SendFileAsync(NounClass.FetchImagePath(Cure[CurrentCurePosition].Item1));
                            }
                        }
                        else if (CurrentMinigame.WhoHasInfo == InfoShare.RegularScientists)
                        {
                            // Team1 Head scientist don't have information
                            await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                            {
                                Title = $"You are Team1's head scientist.",
                                Description = CurrentMinigame.UninformedInstructions,
                                Color = LastColorUsed
                            }.Build());
                        }
                    }
                    else if (m == Team2Lead)
                    {
                        // Team2 Head scientist
                        if (CurrentMinigame.WhoHasInfo == InfoShare.HeadScientist)
                        {
                            // Team2 Head scientist have information
                            if (CurrentMinigame.TypeOfInfo == InfoType.Chat)
                            {
                                // Team2 Head scientist have information (chat)
                                await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                {
                                    Title = $"You are Team2's head scientist. Your word is '{Cure[CurrentCurePosition].Item1}'",
                                    Description = CurrentMinigame.InformedInstructions,
                                    Color = LastColorUsed
                                }.Build());
                            }
                            else if (CurrentMinigame.TypeOfInfo == InfoType.Image)
                            {
                                // Team2 Head scientist have information (image)
                                await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                {
                                    Title = $"You are Team2's head scientist. Your word ({NounClass.TypeOfWord(Cure[CurrentCurePosition].Item2)}) is" +
                                    " associated with the image below",
                                    Description = CurrentMinigame.InformedInstructions,
                                    Color = LastColorUsed
                                }.Build());
                                await m.SendFileAsync(NounClass.FetchImagePath(Cure[CurrentCurePosition].Item1));
                            }
                        }
                        else if (CurrentMinigame.WhoHasInfo == InfoShare.RegularScientists)
                        {
                            // Team2 Head scientist doesn't have information
                            await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                            {
                                Title = $"You are Team2's head scientist.",
                                Description = CurrentMinigame.UninformedInstructions,
                                Color = LastColorUsed
                            }.Build());
                        }
                    }
                    else
                    {
                        // Regular scientist
                        if (CurrentMinigame.WhoHasInfo == InfoShare.HeadScientist)
                        {
                            // Regular scientist doesn't have info
                            await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                            {
                                Title = $"You are a regular scientist on Team {AllPlayers[m]}.",
                                Description = CurrentMinigame.UninformedInstructions,
                                Color = LastColorUsed
                            }.Build());
                        }
                        else if (CurrentMinigame.WhoHasInfo == InfoShare.RegularScientists)
                        {
                            if (AllPlayers[m] == 1 && m != Team1Lead)
                            {
                                // Regular scientist in team 1 have info
                                if (CurrentMinigame.TypeOfInfo == InfoType.Chat)
                                {
                                    // Regular scientist in team 1 have info (chat)
                                    await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                    {
                                        Title = $"You are one of Team1's many scientists. Your word is '{Cure[CurrentCurePosition].Item1}'",
                                        Description = CurrentMinigame.InformedInstructions,
                                        Color = LastColorUsed
                                    }.Build());
                                }
                                else if (CurrentMinigame.TypeOfInfo == InfoType.Image)
                                {
                                    // Regular scientist in team 1 have info (image)
                                    await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                    {
                                        Title = $"You are one of Team1's many scientists. Your word ({NounClass.TypeOfWord(Cure[CurrentCurePosition].Item2)}) is" +
                                                " associated with the image below",
                                        Description = CurrentMinigame.InformedInstructions,
                                        Color = LastColorUsed
                                    }.Build());
                                    await m.SendFileAsync(NounClass.FetchImagePath(Cure[CurrentCurePosition].Item1));
                                }
                            }
                            else if (AllPlayers[m] == 2 && m != Team2Lead)
                            {
                                // Regular scientist in team 2 have info
                                if (CurrentMinigame.TypeOfInfo == InfoType.Chat)
                                {
                                    // Regular scientist in team 2 have info (chat)
                                    await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                    {
                                        Title = $"You are one of Team2's many scientists. Your word is '{Cure[CurrentCurePosition].Item1}'",
                                        Description = CurrentMinigame.InformedInstructions,
                                        Color = LastColorUsed
                                    }.Build());
                                }
                                else if (CurrentMinigame.TypeOfInfo == InfoType.Image)
                                {
                                    // Regular scientist in team 2 have info (image)
                                    await m.SendMessageAsync(embed: new DiscordEmbedBuilder
                                    {
                                        Title = $"You are one of Team2's many scientists. Your word ({NounClass.TypeOfWord(Cure[CurrentCurePosition].Item2)}) is" +
                                                " associated with the image below",
                                        Description = CurrentMinigame.InformedInstructions,
                                        Color = LastColorUsed
                                    }.Build());
                                    await m.SendFileAsync(NounClass.FetchImagePath(Cure[CurrentCurePosition].Item1));
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Finished distributing info");

                // Starts Instruction timer (for players to get their bearings ... ~60 seconds)
                Console.WriteLine("Instruction timer started.");

                StartGenericTimer(ctx,
                                "seconds remaining to read your instructions.",
                                "The round is now starting.",
                                60);
                GenericTimerActive = true;

                while (GenericTimerActive)
                { } // Blocks until Intermission ends

                Console.WriteLine("Instruction period ended.");

                // Starts Round timer
                Console.WriteLine("Round timer started.");
                StartRoundTimer(ctx);
                RoundResult = 0;
                RoundInProgress = true;

                while (RoundInProgress)
                { } // Blocks until round ends

                if (RoundResult == 0)
                {
                    // We resetted the game, just return here.
                    Console.WriteLine("Finishing one game abruptly.");
                    return;
                }
                else if (RoundResult == -1)
                {
                    // Tie round
                    Console.WriteLine("Round ended with a tie.\nGenerating new but similar cures for the next round.");
                    NextRoundTIE();
                }
                else if (RoundResult != 0)
                {
                    // Update the values, then print a message
                    if (RoundResult == 1)
                        await NextRoundWIN(ctx, true);
                    else
                        await NextRoundWIN(ctx, false);
                }

                // Move everyone back into the same voice channel
                foreach (DiscordMember m in T1Channel.Users)
                {
                    Console.WriteLine($"Moving member: {m.DisplayName} to back to main voice channel");
                    await VoiceChannel.PlaceMemberAsync(m);
                }
                foreach (DiscordMember m in T2Channel.Users)
                {
                    Console.WriteLine($"Moving member: {m.DisplayName} to back to main voice channel");
                    await VoiceChannel.PlaceMemberAsync(m);
                }

                // Delete the old voice channels
                foreach (DiscordChannel d in ctx.Guild.Channels.Values)
                {
                    // If the channel's parent belong to the voice channel
                    if (d.Parent != null && d.Parent.Name == "Voice Channels")
                    {
                        if (d.Name.StartsWith("TEAM"))
                            await d.DeleteAsync("Deleting TEAM channel.");
                    }
                }

                // Starts Intermission timer
                Console.WriteLine("Intermission timer started.");
                StartGenericTimer(ctx,
                                "seconds remaining for this intermission.",
                                "Intermission is over.", 20); // debug was 15
                GenericTimerActive = true;

                while (GenericTimerActive)
                { } // Blocks until Intermission ends

                Console.WriteLine("Intermission ended.");
            }

            string TitleMessage;
            if (Team1Score >= (MaxRounds - 1))
                TitleMessage = "GAMEOVER. Team 1 wins!";
            else
                TitleMessage = "GAMEOVER. Team 2 wins!";
            await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Title = TitleMessage,
                Description = $"__Current scores:__ **Team1: {Team1Score} | Team2: {Team2Score}**\n" +
                                  $"__The cure was:__ {GetCure()}",
                Color = LastColorUsed
            }.Build());
            await ctx.Channel.SendMessageAsync("The game is now over, type !sg while in a voice channel with at least 4 players to start a new game!");
            StopGame();
        }

        // Generate two (roughly even) teams from the existing players in Allplayers
        public void GenerateTeam()
        {
            Random rand = new Random();
            int counter = 0;
            int counterLim = AllPlayers.Count / 2;
            int randomIndex;
            Console.WriteLine($"Assigning {AllPlayers.Keys.Count} Players...");

            // Reset team states
            Console.WriteLine("Resetting current team...");
            DiscordMember[] keys = new DiscordMember[AllPlayers.Keys.Count];
            AllPlayers.Keys.CopyTo(keys, 0);
            foreach (DiscordMember key in keys)
            {
                Console.WriteLine($"{key.Username}'s being removed from team");
                AllPlayers[key] = -1;
                Console.WriteLine($"{key.Username}'s team has been removed.");
            }
            Console.WriteLine("Clearing team lists");
            Team1.Clear();
            Team2.Clear();

            //Randomly assigns 1 to the Half of all players
            Console.WriteLine($"Team 1 will have {counterLim} Players.");
            while (counter < counterLim)
            {
                randomIndex = rand.Next(AllPlayers.Count);
                DiscordMember key = keys[randomIndex];
                if (AllPlayers[key] == -1)
                {
                    AllPlayers[key] = 1;
                    Team1.Add(key);
                    Console.WriteLine($"{key.Username} has been added to Team 1");
                    counter++;
                }
            }

            //Adds the remaining Users to the Team 2
            Console.WriteLine($"Team 2 will have {AllPlayers.Count - counterLim} Players.");
            foreach (DiscordMember key in keys)
            {
                if (!Team1.Contains(key))
                {
                    AllPlayers[key] = 2;
                    Team2.Add(key);
                    Console.WriteLine($"{key.Username} has been added to Team 2");
                }
            }
        }

        // Choose a minigame from the minigames we haven't played and have a WIN round on yet
        public void PickMinigame()
        {
            Console.WriteLine("Picking random minigame...");
            List<Minigame> UnplayedMinigames = new List<Minigame>();
            foreach (Minigame m in AllMinigames)
            {
                if (!PlayedMinigames.Contains(m))
                {
                    UnplayedMinigames.Add(m);
                }
            }

            // Randomly choose a minigame
            var Random = new Random();
            CurrentMinigame = UnplayedMinigames[Random.Next(UnplayedMinigames.Count)];
            PlayedMinigames.Add(CurrentMinigame);
        }

        // Select head scientists from teams (head scientists can be chosen again from same heads from previous rounds)
        public void PickHeadScientists()
        {
            Console.WriteLine("Picking head scientists...");
            var Random = new Random();
            Team1Lead = Team1[Random.Next(Team1.Count)];
            Team2Lead = Team2[Random.Next(Team2.Count)];
        }

        // Check if a guess is correct (not caps sensitive)
        // Return true if guess was correct, otherwise false.
        public bool CheckWord(CommandContext ctx, string phrase)
        {
            // Check what team the player is on
            if (phrase == Cure[CurrentCurePosition].Item1)
            {
                if (AllPlayers[ctx.Member] == 1)
                    RoundResult = 1;
                else
                    RoundResult = 2;
                // Guess is correct
                StopRoundTimer();
                RoundInProgress = false;
                return true;
            }
            return false;
        }

        // Pick the appropriate color for the round (different colors every round)
        private void PickRoundColor()
        {
            Console.WriteLine("Choosing Round color");
            var Random = new Random();
            if (LastColorIndexUsed == -1)
                LastColorIndexUsed = Random.Next(AllColors.Count);
            else
            {
                int TempColorIndex = Random.Next(AllColors.Count);
                // Only choose a new color if that color's index is at least 2
                // away from the previous LastColorIndexUsed
                while (Math.Abs(TempColorIndex - LastColorIndexUsed) < 2)
                    TempColorIndex = Random.Next(AllColors.Count);
                LastColorIndexUsed = TempColorIndex;
            }
            LastColorUsed = AllColors[LastColorIndexUsed];
        }

        // Check if a person who sent a message is a player or not
        public bool IsPlayer(CommandContext ctx)
        {
            DiscordMember user = ctx.Member;
            foreach (DiscordMember player in AllPlayers.Keys)
            {
                if (player == user)
                    return true;
            }
            return false;
        }

        // Progress the round after a win
        public async Task NextRoundWIN(CommandContext ctx, bool Team1Won)
        {
            if (Team1Won)
                Team1Score++;
            else
                Team2Score++;
            // Send message after updating the scores
            await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Title = $"Round ended. Team {RoundResult} won!",
                Description = $"__Current scores:__ **Team1: {Team1Score} | Team2: {Team2Score}**\n" +
                              $"__The category for this round was:__ {NounClass.TypeOfWord(Cure[CurrentCurePosition].Item2)}\n" +
                              $"__The word for this round was:__ {Cure[CurrentCurePosition].Item1}",
                Color = LastColorUsed
            }.Build());

            CurrentCurePosition++;
            Round++;
            Team1Lead = null;
            Team2Lead = null;
        }

        // Progress the round after a tie
        public void NextRoundTIE()
        {
            Round++;
            // Generate new cure relating to round category
            GenerateCure(false);
            Team1Lead = null;
            Team2Lead = null;
        }

        // Return the cure in string form
        public string GetCure()
        {
            string temp = string.Empty;
            foreach (var s in Cure)
                temp += s;
            return temp;
        }

        // Randomly generate the cure for both teams
        public void GenerateCure(bool StartGame)
        {
            var random = new Random();
            if (StartGame)
            {
                // Clear any previous solution
                Cure.Clear();
                for (var ind = 0; ind < MaxRounds; ++ind)
                    Cure.Add(RandomNoun(random));
            }
            else
            {
                // TIE round
                // Generate cure word for both teams depending on current round
                // because we tied, and people got stuck on the current words.
                Cure[CurrentCurePosition] = RandomNoun(random);
            }
        }

        // Generate a random word from All_nouns
        private Tuple<string, RoundCategory> RandomNoun(Random rand)
        {
            int RanIndex;
            string RanNoun;
            RoundCategory RanCategory;
            // Choose a random category
            RanCategory = (RoundCategory)rand.Next(0, NounClass.Categories);
            // Choose a random noun from the nouns within the category
            switch (RanCategory)
            {
                case RoundCategory.Animals:
                    RanIndex = rand.Next(NounClass.Animals.Count);
                    RanNoun = NounClass.Animals[RanIndex];
                    break;
                case RoundCategory.Bodyparts:
                    RanIndex = rand.Next(NounClass.Body_parts.Count);
                    RanNoun = NounClass.Body_parts[RanIndex];
                    break;
                case RoundCategory.FruitVegetables:
                    RanIndex = rand.Next(NounClass.Fruit_vegetables.Count);
                    RanNoun = NounClass.Fruit_vegetables[RanIndex];
                    break;
                case RoundCategory.Artificial:
                    RanIndex = rand.Next(NounClass.Artificial.Count);
                    RanNoun = NounClass.Artificial[RanIndex];
                    break;
                case RoundCategory.NaturalWorld:
                    RanIndex = rand.Next(NounClass.Natural_world.Count);
                    RanNoun = NounClass.Natural_world[RanIndex];
                    break;
                case RoundCategory.Occupations:
                    RanIndex = rand.Next(NounClass.Occupations.Count);
                    RanNoun = NounClass.Occupations[RanIndex];
                    break;
                default:
                    RanNoun = "INVALID_NOUN";
                    RanCategory = RoundCategory.NoCategory;
                    break;
            }
            // Create the tuple and return it
            return Tuple.Create(RanNoun, RanCategory);
        }

        // Start the timer (2 minutes)
        private void StartRoundTimer(CommandContext ctx, int seconds = 120)
        {
            Second = seconds; // reset Second ( default 3 minutes)
            DiscordMessage TimeMessage = ctx.Channel.SendMessageAsync($"{Second} seconds remaining for this round.").Result;

            // Initialize Round timer
            roundTimer = new Timer
            {
                Interval = (1000), // Ticks every second
                AutoReset = true,
                Enabled = true
            };
            roundTimer.Elapsed += async (sender, e) => await PrintSecondOrCheckTie(TimeMessage);
        }

        // Start the generic timer given a tickMessage that sends a message to discord every second
        // and an endMessage that is send when the time is up
        private void StartGenericTimer(CommandContext ctx, string tickMessage, string endMessage, int seconds = 10)
        {
            Second = seconds;
            DiscordMessage TimeMessage = ctx.Channel.SendMessageAsync($"{Second}" + tickMessage).Result;

            // Initialize Intermission timer
            genericTimer = new Timer
            {
                Interval = (1000), // Ticks every second
                AutoReset = true,
                Enabled = true
            };
            genericTimer.Elapsed += async (sender, e) => await GenericCallback(TimeMessage, tickMessage, endMessage);
        }

        // Stop the existing round timer if it exists
        private void StopRoundTimer()
        {
            if (roundTimer != null)
            {
                roundTimer.Stop();
                roundTimer.Dispose();
            }
        }

        // Stop the existing Generic timer if it exists
        private void StopGenericTimer()
        {
            if (genericTimer != null)
            {
                genericTimer.Stop();
                genericTimer.Dispose();
            }
        }

        // If Second > 0, display tickMessage
        // Otherwise, stop the GenericTimer and display endMessage
        private async Task GenericCallback(DiscordMessage msg, string tickMessage, string endMessage)
        {
            if (Second <= 0)
            {
                StopGenericTimer();
                await msg.ModifyAsync(endMessage);
                GenericTimerActive = false;
            }
            else
            {
                Second--;
                // Update the message every 15 seconds, or if seconds is 7 or under
                // to avoid rate-limit problem
                if (Second <= 5 | Second % 15 == 0)
                    await msg.ModifyAsync($"{Second} " + tickMessage);
            }
        }

        // If Second > 0, print the current second
        // Otherwise, stop the current round and declare a tie
        private async Task PrintSecondOrCheckTie(DiscordMessage msg)
        {
            if (Second <= 0)
            {
                StopRoundTimer();
                // Send an embedded message describing the results of the round
                // and what words each team had
                await msg.ModifyAsync("", embed: new DiscordEmbedBuilder
                {
                    Title = "Round ended with a TIE.",
                    Description = $"__Current scores:__ **Team1: {Team1Score} | Team2: {Team2Score}**\n" +
                                  $"__The category for this round was:__ {NounClass.TypeOfWord(Cure[CurrentCurePosition].Item2)}\n" +
                                  $"__The word for this round was:__ {Cure[CurrentCurePosition].Item1}",
                    Color = LastColorUsed
                }.Build());
                RoundResult = -1;
                RoundInProgress = false;
            }
            else
            {
                Second--;
                // Update the message every 15 seconds, or if seconds is 7 or under
                // to avoid rate-limit problem
                if (Second <= 5 || Second % 15 == 0)
                    await msg.ModifyAsync($"{Second} seconds remaining for this round.");
            }
        }

        // DEBUG: print everyone in Allplayers
        public void PrintPlayers()
        {
            foreach (DiscordMember m in AllPlayers.Keys)
                Console.WriteLine(m.DisplayName);
        }
    }

    public class Minigame
    {
        // Stores the information for a minigame
        [JsonProperty("Name")]
        public string Name { get; private set; } = string.Empty;
        [JsonProperty("Description")]
        public string Description { get; private set; } = string.Empty;

        // Instructions for those without the secret information
        [JsonProperty("UninformedInstructions")]
        public string UninformedInstructions { get; private set; } = string.Empty;

        // Instructions for those with the secret information
        [JsonProperty("InformedInstructions")]
        public string InformedInstructions { get; private set; } = string.Empty;

        [JsonProperty("WhoHasInfo")]
        public InfoShare WhoHasInfo { get; private set; } = InfoShare.Nobody;

        [JsonProperty("TypeOfInfo")]
        public InfoType TypeOfInfo { get; private set; } = InfoType.None;
    }
}
