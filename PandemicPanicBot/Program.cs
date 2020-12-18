using System;

namespace PandemicPanicBot
{
    class Program
    {
        static void Main()
        {
            // Runs the bot below.
            var bot = new Bot();
            bot.RunAsync().GetAwaiter().GetResult();
        }
    }
}
