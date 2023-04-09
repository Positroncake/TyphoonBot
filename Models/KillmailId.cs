using System.Diagnostics.CodeAnalysis;

namespace TyphoonBot.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class KillmailId
{
    public string killmail_hash { get; set; } = "";
    public ulong killmail_id { get; set; }
}