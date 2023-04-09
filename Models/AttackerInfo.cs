namespace TyphoonBot.Models;

public class AttackerInfo
{
    public string Ship { get; set; } = "";
    public string? Name { get; set; }
    public string? Corp { get; set; }
    public ulong? NameId { get; set; }
    public ulong? CorpId { get; set; }
}