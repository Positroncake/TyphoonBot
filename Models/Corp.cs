using System.Diagnostics.CodeAnalysis;

namespace TyphoonBot.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Corp
{
    public ulong alliance_id { get; set; }
    public ulong ceo_id { get; set; }
    public ulong creator_id { get; set; }
    public DateTime date_founded { get; set; }
    public string description { get; set; } = "";
    public ulong home_station_id { get; set; }
    public ulong member_count { get; set; }
    public string name { get; set; } = "";
    public ulong shares { get; set; }
    public double tax_rate { get; set; }
    public string ticker { get; set; } = "";
    public string url { get; set; } = "";
}