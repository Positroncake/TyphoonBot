// ReSharper disable InconsistentNaming
public class Planet
{
    public List<ulong> moons { get; set; } = new();
    public ulong planet_id { get; set; }
    public List<ulong> asteroid_belts { get; set; } = new();
}

public class SysPosition
{
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
}

public class Sys
{
    public ulong constellation_id { get; set; }
    public string name { get; set; } = "";
    public List<Planet> planets { get; set; } = new();
    public SysPosition position { get; set; } = new();
    public string security_class { get; set; } = "";
    public double security_status { get; set; }
    public ulong star_id { get; set; }
    public List<ulong> stargates { get; set; } = new();
    public List<ulong> stations { get; set; } = new();
    public ulong system_id { get; set; }
}