using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json;
using System.Web;
using ESI.NET;
using ESI.NET.Enumerations;
using Microsoft.Extensions.Options;
using TyphoonBot.Models;

namespace TyphoonBot;

public class ApiService
{
    private EsiClient _client;
    private string _accessToken = "";
    private string _refreshToken = "";
    private string _lastKmId = "";
    private string _clientId = "";
    private string _secretKey = "";
    public string TokenPath { get; set; } = "/bot/token";
    public string KmListPath { get; set; } = "/bot/kms";
    public string ClientIdPath { get; set; } = "/bot/client";
    public string SecretKeyPath { get; set; } = "/bot/secret";
    public List<string> Scopes { get; set; } = new()
    {
        "esi-killmails.read_corporation_killmails.v1",
        "esi-killmails.read_killmails.v1"
    };

    public ApiService()
    {
        IOptions<EsiConfig> config = Options.Create(new EsiConfig()
        {
            EsiUrl = "https://esi.evetech.net/",
            DataSource = DataSource.Tranquility,
            ClientId = _clientId,
            SecretKey = _secretKey,
            CallbackUrl = "https://localhost/callback",
            UserAgent = "TyphoonBot"
        });
        _client = new EsiClient(config);

        _refreshToken = File.ReadAllLines(TokenPath)[0];
        _lastKmId = File.ReadAllLines(KmListPath)[0];
        _clientId = File.ReadAllLines(ClientIdPath)[0];
        _secretKey = File.ReadAllLines(SecretKeyPath)[0];
    }

    public async Task Init()
    {
        var dict = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", _refreshToken },
            { "client_id", _clientId },
            { "scope", string.Join(' ', Scopes) }
        };
        var data = new FormUrlEncodedContent(dict);
        
        var http = new HttpClient();
        Task<HttpResponseMessage> response = http.PostAsync("https://login.eveonline.com/v2/oauth/token", data);
        Token token = JsonSerializer.Deserialize<Token>(await (await response).Content.ReadAsStringAsync()) ?? new Token();

        _accessToken = token.access_token;
        _refreshToken = token.refresh_token;
    }

    public async Task<List<KillmailId>> UpdateKills()
    {
        List<KillmailId> kmIds = await GetAllKills();
        if (kmIds.Count == 0) return new List<KillmailId>();
        int index = kmIds.Count - 1;
        for (var i = 0; i < kmIds.Count; ++i)
        {
            var currentId = kmIds[i].killmail_id.ToString();
            if (currentId != _lastKmId) continue;
            index = i;
            break;
        }
        List<KillmailId> newKmIds = kmIds.GetRange(0, index);
        UpdateLastKmId(kmIds[0].killmail_id.ToString());
        return newKmIds;
    }
    
    public async Task<string> GetCharName(ulong charId)
    {
        var http = new HttpClient();
        var query = $"https://esi.evetech.net/latest/characters/{charId}";
        HttpResponseMessage response = await http.GetAsync(query);
        Character character = JsonSerializer.Deserialize<Character>(await response.Content.ReadAsStringAsync()) ?? new Character();
        return character.name;
    }

    public async Task<string> GetCorpName(ulong corpId)
    {
        var http = new HttpClient();
        var query = $"https://esi.evetech.net/latest/corporations/{corpId}";
        HttpResponseMessage response = await http.GetAsync(query);
        Corp corp = JsonSerializer.Deserialize<Corp>(await response.Content.ReadAsStringAsync()) ?? new Corp();
        return corp.name;
    }

    public async Task<string> GetSystemName(ulong sysId)
    {
        var http = new HttpClient();
        var query = $"https://esi.evetech.net/latest/universe/systems/{sysId}";
        HttpResponseMessage response = await http.GetAsync(query);
        Sys system = JsonSerializer.Deserialize<Sys>(await response.Content.ReadAsStringAsync()) ?? new Sys();
        return system.name;
    }

    public async Task<string> GetShipInfo(ulong shipId)
    {
        var http = new HttpClient();
        var nameQuery = $"https://esi.evetech.net/latest/universe/types/{shipId}/";
        HttpResponseMessage response = await http.GetAsync(nameQuery);
        Ship ship = JsonSerializer.Deserialize<Ship>(await response.Content.ReadAsStringAsync()) ?? new Ship();
        return ship.name;
    }

    public async Task<(AttackerInfo, AttackerInfo)> GetAttackerInfo(Killmail km)
    {
        List<Attacker> attackers = km.attackers;
        Attacker finalBlow = attackers.First(attacker => attacker.final_blow);
        Attacker topDmg = attackers.MaxBy(atk => atk.damage_done) ?? new Attacker();

        var finalBlowInfo = new AttackerInfo();
        finalBlowInfo.Ship = await GetShipName(finalBlow.ship_type_id);
        finalBlowInfo.Name = finalBlow.character_id == null ? null : await GetCharName(finalBlow.character_id.Value);
        finalBlowInfo.Corp = finalBlow.corporation_id == null ? null : await GetCorpName(finalBlow.corporation_id.Value);
        finalBlowInfo.NameId = finalBlow.character_id;
        finalBlowInfo.CorpId = finalBlow.corporation_id;

        var topDmgInfo = new AttackerInfo();
        topDmgInfo.Ship = await GetShipName(topDmg.ship_type_id);
        topDmgInfo.Name = topDmg.character_id == null ? null : await GetCharName(topDmg.character_id.Value);
        topDmgInfo.Corp = topDmg.corporation_id == null ? null : await GetCorpName(topDmg.corporation_id.Value);
        topDmgInfo.NameId = topDmg.character_id;
        topDmgInfo.CorpId = topDmg.corporation_id;

        return (finalBlowInfo, topDmgInfo);
    }

    private async Task<string> GetShipName(ulong shipId)
    {
        var http = new HttpClient();
        var nameQuery = $"https://esi.evetech.net/latest/universe/types/{shipId}/";
        HttpResponseMessage response = await http.GetAsync(nameQuery);
        Ship ship = JsonSerializer.Deserialize<Ship>(await response.Content.ReadAsStringAsync()) ?? new Ship();
        return ship.name;
    }

    public async Task<List<Killmail>> GetKillmails(List<KillmailId> kmIds)
    {
        var kms = new List<Killmail>();
        foreach (KillmailId kmId in kmIds)
        {
            var query = $"https://esi.evetech.net/latest/killmails/{kmId.killmail_id}/{kmId.killmail_hash}";
            var http = new HttpClient();
            HttpResponseMessage response = await http.GetAsync(query);
            var km = JsonSerializer.Deserialize<Killmail>(await response.Content.ReadAsStringAsync());
            kms.Add(km ?? new Killmail());
        }

        return kms;
    }

    private async void UpdateLastKmId(string kmId)
    {
        _lastKmId = kmId;
        await File.WriteAllTextAsync(KmListPath, kmId);
    }

    private async Task<List<KillmailId>> GetAllKills()
    {
        NameValueCollection qs = HttpUtility.ParseQueryString("");
        qs.Add("datasource", "tranquility");
        qs.Add("page", "1");
        qs.Add("token", _accessToken);
        var query = $"https://esi.evetech.net/latest/corporations/98729657/killmails/recent/?{qs}";

        var http = new HttpClient();
        HttpResponseMessage response = await http.GetAsync(query);
        Stream stream = await response.Content.ReadAsStreamAsync();
        List<KillmailId> debug;
        try
        {
            debug = await JsonSerializer.DeserializeAsync<List<KillmailId>>(stream) ?? new List<KillmailId>();
        }
        catch (Exception)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            debug = new List<KillmailId>();
        }

        return debug;
    }
}