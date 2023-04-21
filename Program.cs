using System.Security.Cryptography;
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
        980383907939770388, // lobby
        1003756221632888892, // apply
        1045731395370238012, // llamas friends and spies
        1075156391007887462, // defense
        909202211546996766, // llama pings
        1065738052133208196, // alpaca pings
        1030437368785801256 // logs
    };
    private const string Prefix = "/bot/";
    private readonly List<string> _files = new()
    {
        "fleetphoon.png",
        "fastphoon.png",
        "phoon.png"
    };
    private List<(int, int)> _times = null!;
    private readonly ApiService _service = new();

    public static Task Main() => new Program().MainAsync();

    public async Task MainAsync()
    {
        // Generate times
        _times = new List<(int, int)> { (R(12, 13), R()), (R(14, 16), R()), (R(17, 22), R()), (R(4, 23), R()) };
        foreach ((int h, int m) in _times)
            Console.WriteLine($"{h:00}:{m:00}");

        // Init
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.MessageReceived += MessageReceived;
        await _client.LoginAsync(TokenType.Bot, (await File.ReadAllLinesAsync("/bot/token2"))[0]);
        await _client.StartAsync();
        await _client.SetStatusAsync(UserStatus.Online);
        await _service.Init();
        
        // Timers
        var are = new AutoResetEvent(false);
        var phoonTimer = new Timer(PhoonTimer, are, 0, 5_000);
        var scramTimer = new Timer(ScramTimer, are, 3_600_000, 3_600_000);
        var zkillTimer = new Timer(ZkillTimer, are, 0, 60_000);
        var tokenTimer = new Timer(TokenTimer, are, 0, 1_100_000);
        are.WaitOne();
        
        // Keep open
        await Task.Delay(-1);
    }

    private async void PhoonTimer(object? o)
    {
        DateTime current = DateTime.UtcNow;
        (int, int) currTime = (current.Hour, current.Minute);
        if (_times.Contains(currTime)) await SendPhoonPic();
        
        async Task SendPhoonPic()
        {
            var c = await _client.GetChannelAsync(GeneralChannel) as IMessageChannel;
            int rand = RandomNumberGenerator.GetInt32(0, 100);
            string path = rand switch
            {
                < 45 => _files[0],
                < 75 => _files[1],
                _ => _files[2]
            };

            await c!.SendFileAsync($"{Prefix}{path}");
            Console.WriteLine($"Picture \"{path}\" sent at {DateTime.UtcNow:O}");
        }
    }

    private void ScramTimer(object? o)
    {
        DateTime current = DateTime.UtcNow;
        if (current.Hour != 5) return;
        _times = new List<(int, int)> { (R(12, 13), R()), (R(14, 16), R()), (R(17, 22), R()), (R(4, 23), R()) };
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
            if (message.Channel.Name.Contains("intel") == false) await message.Channel.SendMessageAsync("typhoons! :D");
            await React();
        }
    
        // Check if message from positron
        if (message.Author.ToString().Contains("positroncake") &&
            message.Author.Discriminator.Contains("0001") &&
            message.Channel.Name.Contains("intel") == false &&
            RandomNumberGenerator.GetInt32(0, 12) == 0) await React();

        async Task React()
        {
            GuildEmote? emote = _client.Guilds.SelectMany(y => y.Emotes)
                .FirstOrDefault(z => z.Name.IndexOf("typhoon", StringComparison.OrdinalIgnoreCase) != -1);
            if (emote is null) return;
            Console.WriteLine($"Reacted at {DateTime.UtcNow:O}");
            await message.AddReactionAsync(emote);
        }
    }

    private static int R() => RandomNumberGenerator.GetInt32(0, 60);
    private static int R(int min, int max) => RandomNumberGenerator.GetInt32(min, max + 1);
}