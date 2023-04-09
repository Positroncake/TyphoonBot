using System.Diagnostics.CodeAnalysis;

namespace TyphoonBot.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Token
{
    public string access_token { get; set; } = "";
    public string token_type { get; set; } = "";
    public int expires_in { get; set; } = 1199;
    public string refresh_token { get; set; } = "";
}