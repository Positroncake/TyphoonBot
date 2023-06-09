﻿using System.Security.Cryptography;
using Discord;
using Discord.WebSocket;
using TyphoonBot.Models;

namespace TyphoonBot;

public class Program
{
    private DiscordSocketClient _client = null!;
    private const ulong GeneralChannel = 902830255264366595;
    private const ulong ZkillChannel = 1088866098809679942;

    private readonly List<ulong> _excluded = new()
    {
        910114456569278474, // c2 intel
        1064213496704807052 // c4 intel
    };
    private const string Prefix = "/bot/";
    private readonly List<(int, string)> _images = new()
    {
        (35, "fleetphoon.png"),
        (70, "fastphoon.png"),
        (97, "phoon.png"),
        (100, "tormentor.png")
    };
    private List<(int, int)> _times = null!;
    private readonly ApiService _service = new();
    
    private void GenTimes()
    {
        int rawInterval = R(22, 27);
        int interval = rawInterval > 23 ? rawInterval - 24 : rawInterval;
        _times = new List<(int, int)> { (R(13, 17), R()), (interval , R()) };
    }

    public static Task Main() => new Program().MainAsync();

    public async Task MainAsync()
    {
        // Generate times
        GenTimes();
        foreach ((int h, int m) in _times)
            Console.WriteLine($"{h:00}:{m:00}");

        // Init
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.MessageReceived += MessageReceived;
        await _client.LoginAsync(TokenType.Bot, (await File.ReadAllLinesAsync("/bot/token2"))[0]);
        await _client.StartAsync();
        await _client.SetStatusAsync(UserStatus.Online);
        _client.Ready += InitSlashCommands;
        await _service.Init();

        // Timers
        var are = new AutoResetEvent(false);
        var phoonTimer = new Timer(PhoonTimer, are, 0, 60_000);
        var scramTimer = new Timer(ScramTimer, are, 3_600_000, 3_600_000);
        var zkillTimer = new Timer(ZkillTimer, are, 0, 60_000);
        var tokenTimer = new Timer(TokenTimer, are, 0, 1_100_000);
        are.WaitOne();
        
        // Keep open
        await Task.Delay(-1);
    }

    private async Task InitSlashCommands()
    {
        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("typhoonctl");
        globalCommand.WithDescription("Controls various aspects of ./typhoon.sh");
        globalCommand.AddOption(new SlashCommandOptionBuilder()
            .WithName("image")
            .WithDescription("Controls manual and automatic phooning")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("send")
                .WithDescription("Manually sends a specified image (name without .png extension)")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String,
                    "The name of the image (without .png extension)", true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("scram")
                .WithDescription(
                    "Scrambles automatic phooning times, 0 for all or 1-4 for a specific interval (4 intervals total)")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("interval", ApplicationCommandOptionType.Integer,
                    "The interval to scramble (0 for all or 1-4 for a specific interval (4 intervals total))", true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("get-times")
                .WithDescription("Shows scheduled automatic phooning times for today")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("whatever", ApplicationCommandOptionType.String,
                    "Optional, put whatever in here, it has no effect", false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("add-time")
                .WithDescription("Schedules a phoon to be sent today at a specific time")));
        await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
        _client.SlashCommandExecuted += CommandReceived;
    }

    private async Task CommandReceived(SocketSlashCommand command)
    {
        await command.RespondAsync("Test");
    }

    private async void PhoonTimer(object? o)
    {
        DateTime current = DateTime.UtcNow;
        (int, int) currTime = (current.Hour, current.Minute);
        if (_times.Contains(currTime)) await SendPhoonPic();
        
        async Task SendPhoonPic()
        {
            var c = await _client.GetChannelAsync(GeneralChannel) as IMessageChannel;
            int random = RandomNumberGenerator.GetInt32(0, 100);
            var path = "";
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach ((int, string) image in _images)
            {
                if (random >= image.Item1) continue;
                path = image.Item2;
                break;
            }

            await c!.SendFileAsync($"{Prefix}{path}");
            Console.WriteLine($"Picture \"{path}\" sent at {DateTime.UtcNow:O}");
        }
    }

    private void ScramTimer(object? o)
    {
        DateTime current = DateTime.UtcNow;
        if (current.Hour != 5) return;
        GenTimes();
        Console.WriteLine("-- Times scrambled --");
        foreach ((int h, int m) in _times)
            Console.WriteLine($"{h.ToString("00")}:{m.ToString("00")}");
    }

    private async void ZkillTimer(object? o)
    {
        var c = await _client.GetChannelAsync(ZkillChannel) as IMessageChannel;
        List<KillmailId> response = await _service.UpdateKills();
        Console.WriteLine($"Got {response.Count} new kills at {DateTime.UtcNow:O}");
        if (response.Count == 0) return;
        response.Reverse();
        List<Killmail> killmails = await _service.GetKillmails(response);
        foreach (Killmail km in killmails)
        {
            string system = await _service.GetSystemName(km.solar_system_id);
            string victimShip = await _service.GetShipInfo(km.victim.ship_type_id);
            string victim = await _service.GetCharName(km.victim.character_id);
            string victimCorp = await _service.GetCorpName(km.victim.corporation_id);
            (AttackerInfo finalBlow, AttackerInfo topDmg) = await _service.GetAttackerInfo(km);
            DateTime time = km.killmail_time;

            string title;
            Color colour;
            if (victimCorp == finalBlow.Corp || victimCorp == topDmg.Corp)
            {
                title = "Ship awox'ed in ";
                colour = Color.Orange;
            }
            else if (victimCorp == "Alpaca Baseballcap Conglomerate")
            {
                title = "Ship lost in ";
                colour = Color.Purple;
            }
            else
            {
                title = RandomNumberGenerator.GetInt32(0, 10) == 0 ? "Ship gnawed in " : "Ship destroyed in ";
                colour = Color.DarkGreen;
            }

            var embed = new EmbedBuilder()
            {
                Title = title + system,
                Description = $"https://zkillboard.com/kill/{km.killmail_id.ToString()}",
                Color = colour,
                ImageUrl = $"https://images.evetech.net/types/{km.victim.ship_type_id}/icon"
            };
            embed.AddField("Victim",
                string.IsNullOrEmpty(victim) == false
                    ? $"[{victim}](https://zkillboard.com/character/{km.victim.character_id}) ([{victimCorp}](https://zkillboard.com/corporation/{km.victim.corporation_id})) in a {victimShip}"
                    : $"NPC in a {victimShip}");
            embed.AddField("Top damage",
                string.IsNullOrEmpty(topDmg.Name) == false
                    ? $"[{topDmg.Name}](https://zkillboard.com/character/{topDmg.NameId}) ([{topDmg.Corp}](https://zkillboard.com/corporation/{topDmg.CorpId})) in a {topDmg.Ship}"
                    : $"NPC in a {topDmg.Ship}");
            embed.AddField("Final blow",
                string.IsNullOrEmpty(finalBlow.Name) == false
                    ? $"[{finalBlow.Name}](https://zkillboard.com/character/{finalBlow.NameId}) ([{finalBlow.Corp}](https://zkillboard.com/corporation/{finalBlow.CorpId})) in a {finalBlow.Ship}"
                    : $"NPC in a {finalBlow.Ship}");
            embed.WithFooter(footer => footer.Text = $"on {time:yyyy MMM dd} at time {time:hh:mm:ss}");
            await c!.SendMessageAsync(embed: embed.Build());
        }
    }

    private async void TokenTimer(object? o)
    {
        await _service.Init();
        Console.WriteLine($"ESI tokens refreshed at {DateTime.UtcNow:O}");
    }

    private Task Log(LogMessage lM)
    {
        Console.WriteLine(lM.ToString());
        return Task.CompletedTask;
    }

    private async Task MessageReceived(SocketMessage message)
    {
        // Validation
        if (message.Author.IsBot) return;
        if (_excluded.Any(i => message.Channel.Id == i)) return;
        if (message.Channel.Name.Contains("ticket")) return;

        // Check if message contains phoon
        var c = await _client.GetChannelAsync(message.Channel.Id) as IMessageChannel;
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> x = c!.GetMessagesAsync(limit: 1, mode: CacheMode.AllowDownload, options: null);
        IMessage? lastMessage = (await x.FlattenAsync()).First();
        if (lastMessage.ToString()!.ToLower().Contains("phoon"))
        {
            await message.Channel.SendMessageAsync("typhoons! :D");
            await React("typhoon");
        }
    
        // Check if message from positron
        if (message.Author.ToString().Contains("positroncake") &&
            message.Author.Discriminator.Contains("0001") &&
            RandomNumberGenerator.GetInt32(0, 15) == 0) await React("typhoon");

        async Task React(string emojiName)
        {
            GuildEmote? emote = _client.Guilds.SelectMany(y => y.Emotes)
                .FirstOrDefault(z => z.Name.IndexOf(emojiName, StringComparison.OrdinalIgnoreCase) != -1);
            if (emote is null) return;
            Console.WriteLine($"Reacted at {DateTime.UtcNow:O}");
            await message.AddReactionAsync(emote);
        }
    }

    private static int R() => RandomNumberGenerator.GetInt32(0, 60);
    private static int R(int min, int max) => RandomNumberGenerator.GetInt32(min, max + 1);
}