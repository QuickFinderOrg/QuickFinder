# QuickFinder
QuickFinder is a web application that helps students find groups that fit their preferences, communicate with their group members via automatically created Discord channels and for teachers to set rules and manage groups for their courses.

## Setup

1.  Install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
2.  Clone the [QuickFinder Github Repository](https://github.com/QuickFinderOrg/QuickFinder)
3.  Navigate to the location of the cloned repository
4.  Run `"dotnet restore"` to install the necessary packages from NuGet
5.  Run `"dotnet ef database update"` to create and seed a SQLLite database

## Run Locally In Development Mode

Running with hot-reload on file changes:

```bash
dotnet watch
```

Running without hot-reload:

```bash
dotnet run
```

## Run Test Suite

```bash
cd .\group-finder.Tests\
dotnet test
```

## Building for production

Building for to to the Unix system.

```bash
dotnet publish -c Release -r linux-x64 -o publish
```

## Configuration

The project can be configured using [ASP.NET Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0), which includes environment variables, `dotnet user-secrets` and `appsettings.json` files.

**Do not** commit any variables labelled "secret" to your git repository. Use the .NET secrets manager or environment variables instead.

### Discord

Using the Discord integration requires you to have a [Discord](https://discord.com) account, a Discord Application and at least one Discord server for students to join.

Create an application on the [Discord Developer Pages](https://discord.com/developers/applications). Store the client id, secret and bot token for later.

To create a discord server, press the "+" in at the bottom of the server list in the Discord client. To get the server id, you first enable developer mode inside the Discord Client's settings. Afterward, right click the server icon in the server list and click the "Copy Server ID" button.

Add these values to your configuration. Values under *"DiscordAuth"* are required for users to sign up with Discord. Without it you will only be able to log into the admin account and other test accounts. Values under `"Discord"` are used for the Discord server management. See the example below for an example.

```json
"Discord": {
    "ClientId": "",
    "ServerId": "",
    "BotToken": "secret"
  },
  "DiscordAuth": {
    "ClientId": "",
    "ClientSecret": "secret",
  },
```

*Excerpt from the `appsettings.json` file.*

### Disable scheduler

Disabling the scheduler may be necessary to debugging, to have full control over when task are executed. It can be disabled by setting the configuration variable `"DisableScheduler"` to `"true"`. It is recommended to use the .NET user secrets manager for this, to make sure the variable is not pushed to the repository.

## Known Issues

C# sometimes leaves behind temporary files or otherwise caches invalid data after compile.
This can lead to build errors. To fix this, delete the `/bin` and `/obj` folders. These can be found in the root folder and inside the `group_project.Tests` folder.

