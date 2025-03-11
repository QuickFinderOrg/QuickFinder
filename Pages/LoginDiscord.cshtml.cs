using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

namespace group_finder.Pages;

public class DiscordModel(IConfiguration configuration, UserService userService, SignInManager<User> signInManager) : PageModel
{
    private const string API_ENDPOINT = "https://discord.com/api/v10";
    private readonly string CLIENT_ID = configuration["Discord:ClientId"] ?? throw new Exception("Discord:ClientId");
    private readonly string CLIENT_SECRET = configuration["Discord:ClientSecret"] ?? throw new Exception("Discord:ClientSecret");
    public async Task<IActionResult> OnGetAsync(string code)
    {
        Console.WriteLine("Code: " + code);
        {
            var responseJSON = await ExchangeCodeAsync(code);
            var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJSON) ?? throw new Exception("responseJSON");
            // within this token lies the power to surpass metal gear
            var token = tokenResponse["access_token"].ToString();

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var identity_response = await client.GetAsync("https://discord.com/api/v10/users/@me");
            var identityJSON = await identity_response.Content.ReadAsStringAsync();

            var identityDict = JsonSerializer.Deserialize<Dictionary<string, object>>(identityJSON) ?? throw new Exception("responseJSON");

            // var user = await mediator.Send(new RegisterDiscordUser.Request()
            // {
            //     DiscordId = identityDict["id"].ToString() ?? throw new Exception("id"),
            //     Email = identityDict["email"].ToString() ?? throw new Exception("email"),
            //     Username = identityDict["username"].ToString() ?? throw new Exception("username"),
            // });
            var user = await userService.CreateDiscordUser(
                identityDict["email"].ToString() ?? throw new Exception("email"),
                identityDict["username"].ToString() ?? throw new Exception("username"),
                identityDict["id"].ToString() ?? throw new Exception("id")
            );

            if (user is not null)
            {
                HttpContext.SetUserId(user.Id!);
                await signInManager.SignInAsync(user, true);
                TempData["AlertSuccess"] = "You have been logged in.";
                // Redirect to the home page or another page
                return Redirect(StudentRoutes.Home());
            }
            else
            {
                TempData["AlertDanger"] = "Something failed. You are not logged in.";
                return Redirect(StudentRoutes.Login());
            }

            // user.register(email, name, etc.)
            // don't create passwords for discord users
            // store discord id and access token
            // user.set profile
        }

    }

    public async Task<string> ExchangeCodeAsync(string code)
    {
        using HttpClient client = new HttpClient();
        var data = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", $"https://{HttpContext.Request.Host}/LoginDiscord")
            });

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

        HttpResponseMessage response = await client.PostAsync($"{API_ENDPOINT}/oauth2/token", data);
        Console.WriteLine(await response.Content.ReadAsStringAsync());

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}