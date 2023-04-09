// ReSharper disable InconsistentNaming
namespace TyphoonBot.Models;

public class DogmaAttribute
{
    public ulong attribute_id { get; set; }
    public double value { get; set; }
}

public class DogmaEffect
{
    public ulong effect_id { get; set; }
    public bool is_default { get; set; }
}

public class Ship
{
    public double capacity { get; set; }
    public string description { get; set; } = "";
    public List<DogmaAttribute> dogma_attributes { get; set; } = new();
    public List<DogmaEffect> dogma_effects { get; set; } = new();
    public ulong graphic_id { get; set; }
    public ulong group_id { get; set; }
    public ulong market_group_id { get; set; }
    public double mass { get; set; }
    public string name { get; set; } = "";
    public double packaged_volume { get; set; }
    public ulong portion_size { get; set; }
    public bool published { get; set; }
    public double radius { get; set; }
    public ulong type_id { get; set; }
    public double volume { get; set; }
}