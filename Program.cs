using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace discordbot
{
    class Program
    {
        public static DiscordSocketClient _client = new DiscordSocketClient();
        public static CommandService _commands = new CommandService();

        static async Task Main(string[] args)
        {
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), null);
            _client.Log += Log;
            _client.MessageReceived += HandleCommands;

            await _client.LoginAsync(TokenType.Bot, args[0]);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async static Task HandleCommands(SocketMessage msg)
        {
            var message = msg as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) ||
            message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
            message.Author.IsBot)
                return;

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);

        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async static Task Ping(SocketMessage msg)
        {
            if (msg.Content == "!ping")
                await msg.Channel.SendMessageAsync("Pong!");
        }
    }

    public class CommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("square")]
        [Summary("Squares a number.")]
        public async Task SquareAsync(
        [Summary("The number to square.")]
        int num)
        {
            // We can also access the channel from the Command Context.
            await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
        }

        [Command("userinfo")]
        [Summary
    ("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync(
        [Summary("The (optional) user to get info from")]
        SocketUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }
    }
}
