# TyphoonBot
TyphoonBot (also known as ./typhoon.sh) is a Discord KmListPathbot for retrieving lossmails from the MMORPG EVE Online
and displaying them in your server.  It also sends images of the Typhoon battleship periodically in a channel of your choice.

# Installation and configuration
Clone:
```
git clone https://github.com/Positroncake/TyphoonBot.git
```

Place the discord bot token in a file and set the path in `Program.cs.MainAsync()`.

Required frameworks: .NET 7.0.202

NOTE: You will need a ESI API refresh token with the following scopes (and director permissions within the corp you wish to retrieve lossmails for):
```
esi-killmails.read_corporation_killmails.v1
esi-killmails.read_killmails.v1
```
Place the refresh token (and nothing else) inside a file, then update the path in `ApiService.TokenPath`.
You should also create a text file containing the ID of the killmail you wish the bot to start reading from (all killmails after the ID supplied will be copied and displayed on first run). Place the path of this file in `ApiService.KmListPath`.
The paths to the ESI application's client ID and secret key files should be placed in `ApiService.ClientIdPath` and `ApiService.SecretKeyPath`, respectively.

Run: `cd TyphoonBot` then `dotnet run`