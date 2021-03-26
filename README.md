[botIcon]: InfoAboutProject/PandemicPanicBotLogo.png "The PandemicPanic bot icon"
[botPermission]: InfoAboutProject/BotPermissions.JPG "The permissions you need to enable for PandemicPanicBot"

# PandemicPanicBot

![This is a graphic representing the PandemicPanic game][botIcon]

A multiplayer, monostate game hosting Discord bot created for ICS 163 project Fall 2020.

## Contributers 
William Nguyen (@captnw), Sage Mahmud, Ah Lon Sin (@ahls)

## What is PandemicPanic?
A game intended for audiences of at least **4** people and at most **10** people.

In short, it's a game where two teams work against each other to guess a word first in a 2-3 round game where each team would guess a word. Every round there would be a person or person(s) that would know the word, and depending on the minigame, they must convey the information by explaining the object or answering questions. The team that guesses **2** words first correctly wins the game.

Lorewise, it's basically two scientist groups competiting against each other to discover a cure for the virus ravaging the world.

**tl;dr**: it's like a jackbox's game

## What is the PandemicPanicBot?
This is a Discord bot created with DSharpPlus meant to be the "referee" (keep track of score and determines the winner) for this game, if you download this repo, and set up this bot, you can play a round of PandemicPanic with your friends on your Discord server.

The list of normal commands can be invoked with **!help**, and information about the commands can be invoked with **!help [name of command]**.

This bot will ignore you if you direct message it, although it will send information / direct message you. Yes, this is an intentional design decision.

## Setup

1. Download repo.
2. Open solution with visual studio.
3. Configure the bot by replacing YOUR_DISCORD_TOKEN_HERE in config.json with your Discord bot token.
..1. If you don't have a Discord bot token, you may have to set up a Discord bot, please check out this useful link to learn how to set up a Discord bot and retrieve its token (https://www.writebots.com/discord-bot-token/)
..2. When you configure the Bot permissions, be sure to enable "send messages", "attach files", "read message history", and "add reactions".
4. **OPTIONAL:** set the bot icon with the PandemicPanicBotLogo.png found in the "InfoAboutProject" folder

![Enable "Send Messages", "Attach files", "Read Message History", and "Add Reactions" for the bot][botPermission]

4. Run the bot (run the visual studio project), and your bot should be online in your server.

## Playing a game
(note: the bot assumes that everyone will stay in the voicechannel during the game, so if people leave the game early, bugs will happen)

1. Run the bot
2. At least **4** people will join the same voice channel
3. One person will invoke **!sg**, the bot will reply that its ready to go.
4. Follow the prompts in that chat channel, and be sure to check your DMs for any further instructions, have fun!

## Known bugs

1. Sometimes, the bot may not register that a person is in the voice channel, if you start the game and someone is excluded, stop the bot and restart it. Restarting the bot will make sure that it recognizes everyone in the voice channel, and you can start your game normally.
