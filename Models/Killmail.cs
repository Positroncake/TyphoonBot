// ReSharper disable InconsistentNaming
namespace TyphoonBot.Models;

public class Killmail
{
    public List<Attacker> attackers { get; set; } = new();
    public ulong killmail_id { get; set; }
    public DateTime killmail_time { get; set; }
    public ulong solar_system_id { get; set; }
    public Victim victim { get; set; } = new();
}

public class Attacker
{
    public ulong damage_done { get; set; }
    public ulong faction_id { get; set; }
    public bool final_blow { get; set; }
    public double security_status { get; set; }
    public ulong ship_type_id { get; set; }
    public ulong? character_id { get; set; }
    public ulong? corporation_id { get; set; }
    public ulong? weapon_type_id { get; set; }
}

public class Item
{
    public ulong flag { get; set; }
    public ulong item_type_id { get; set; }
    public ulong quantity_destroyed { get; set; }
    public ulong singleton { get; set; }
    public ulong? quantity_dropped { get; set; }
}

public class Position
{
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
}

public class Victim
{
    public ulong alliance_id { get; set; }
    public ulong character_id { get; set; }
    public ulong corporation_id { get; set; }
    public ulong damage_taken { get; set; }
    public List<Item> items { get; set; } = new();
    public Position position { get; set; } = new();
    public ulong ship_type_id { get; set; }
}