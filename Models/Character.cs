using System.Diagnostics.CodeAnalysis;

namespace TyphoonBot.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Character
{
    public ulong alliance_id { get; set; }
    public DateTime birthday { get; set; }
    public ulong bloodline_id { get; set; }
    public ulong corporation_id { get; set; }
    public string description { get; set; } = "";
    public string gender { get; set; } = "";
    public string name { get; set; } = "";
    public ulong race_id { get; set; }
    public double security_status { get; set; }
}