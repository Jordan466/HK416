using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace discordbot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var client = new DiscordSocketClient();
            client.Log += Log;

            await client.LoginAsync(TokenType.Bot, args[0]);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
