namespace Monolith.Network.Modules.Dns;

public class DnsSettings
{
    public bool Enabled { get; set; } = false;
    public bool Recursion { get; set; } = true;
    public bool Forwarding { get; set; } = false;
    public string[] Forwarders { get; set; } = Array.Empty<string>();
    public string LogLevel { get; set; } = "info";
    public bool DnssecValidation { get; set; } = true;
}

public class DnsZone
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "master"; // master, slave, forward, stub
    public bool Enabled { get; set; } = false;
    public string File { get; set; } = "";
    public string[] Masters { get; set; } = Array.Empty<string>(); // For slave zones
    public bool AllowTransfer { get; set; } = false;
    public string[] AllowTransferTo { get; set; } = Array.Empty<string>();
    public int Ttl { get; set; } = 3600;
    public string SoaEmail { get; set; } = "admin@example.com";
    public int Refresh { get; set; } = 86400;
    public int Retry { get; set; } = 7200;
    public int Expire { get; set; } = 604800;
    public int NegativeTtl { get; set; } = 3600;
}

public class DnsRecord
{
    public string Id { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "A"; // A, AAAA, CNAME, MX, TXT, NS, PTR, SRV
    public string Data { get; set; } = "";
    public int Ttl { get; set; } = 3600;
    public int Priority { get; set; } = 0; // For MX and SRV records
    public bool Enabled { get; set; } = true;
}

public class DnsZoneConfig
{
    public string Zone { get; set; } = "";
    public bool Enabled { get; set; }
    public string Type { get; set; } = "master";
    public string File { get; set; } = "";
    public string[] Masters { get; set; } = Array.Empty<string>();
    public bool AllowTransfer { get; set; } = false;
    public string[] AllowTransferTo { get; set; } = Array.Empty<string>();
    public int Ttl { get; set; } = 3600;
    public string SoaEmail { get; set; } = "admin@example.com";
    public int Refresh { get; set; } = 86400;
    public int Retry { get; set; } = 7200;
    public int Expire { get; set; } = 604800;
    public int NegativeTtl { get; set; } = 3600;
}

public class DnsRecordConfig
{
    public string Id { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "A";
    public string Data { get; set; } = "";
    public int Ttl { get; set; } = 3600;
    public int Priority { get; set; } = 0;
    public bool Enabled { get; set; } = true;
}
