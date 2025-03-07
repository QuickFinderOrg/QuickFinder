using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace group_finder;

public class DiscordBotService(ulong serverId, ulong groupChannelId)
{
    private readonly DiscordSocketClient _client = new DiscordSocketClient();

    public async Task StartAsync(string token)
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task<bool> SendDM(ulong userId, string message)
    {
        var user = await _client.GetUserAsync(userId);
        if (user == null)
        {
            Console.WriteLine("User not found.");
            return false;

        }


        try
        {
            await user.SendMessageAsync(message);
            Console.WriteLine($"Sent DM to {user.Username}: {message}");
            return true;
        }
        catch (Discord.Net.HttpException e)
        {
            Console.WriteLine($"Failed to send DM to {user.Username}: DiscordErrorCode: {e.DiscordCode}");
            return false;
        }
    }

    public async Task<ulong?> CreateChannel(string channelName)
    {
        var server = _client.GetGuild(serverId);
        if (server == null)
        {
            return null;
        }
        Console.WriteLine($"groupChannelId {groupChannelId}");

        var channel = await server.CreateTextChannelAsync(channelName, p => p.CategoryId = groupChannelId);
        Console.WriteLine(channel.ToString());
        return channel.Id;
    }
}
