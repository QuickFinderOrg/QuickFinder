using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace group_finder;

public class DiscordBotService
{
    private readonly DiscordSocketClient _client;

    public DiscordBotService()
    {
        _client = new DiscordSocketClient();
    }

    public async Task StartAsync(string token)
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task SendDM(ulong userId, string message)
    {
        var user = await _client.GetUserAsync(userId);
        if (user != null)
        {
            await user.SendMessageAsync(message);
            Console.WriteLine($"Sent DM to {user.Username}: {message}");
        }
        else
        {
            Console.WriteLine("User not found.");
        }
    }
}
