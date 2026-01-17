namespace Monolith.Network.Modules.Dhcp;

public class DhcpConfig
{
    public bool Enabled { get; set; }
    public string Interface { get; set; } = "";
    public string StartAddress { get; set; } = "192.168.1.100";
    public string EndAddress { get; set; } = "192.168.1.200";
    public string SubnetMask { get; set; } = "255.255.255.0";
    public string Gateway { get; set; } = "192.168.1.1";
    public string[] DnsServers { get; set; } = Array.Empty<string>();
    public int LeaseTime { get; set; } = 3600; // seconds
}

public class DhcpSettings
{
    public bool Enabled { get; set; } = false;
    public int DefaultLeaseTime { get; set; } = 7200;
    public int MaxLeaseTime { get; set; } = 86400;
    public bool DnsRegistration { get; set; } = false;
    public string LogLevel { get; set; } = "info";
}

public class DhcpInterface
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string Subnet { get; set; } = "";
    public string ClientPolicy { get; set; } = "allow-all";
    public string PoolStart { get; set; } = "";
    public string PoolEnd { get; set; } = "";
    public string Dns1 { get; set; } = "";
    public string Dns2 { get; set; } = "";
    public string Dns3 { get; set; } = "";
    public string Dns4 { get; set; } = "";
    public string Gateway { get; set; } = "";
    public string Domain { get; set; } = "";
    public int LeaseTime { get; set; } = 7200;
    public int MaxLeaseTime { get; set; } = 86400;
    public bool StaticArp { get; set; }
}

public class DhcpInterfaceConfig
{
    public string Interface { get; set; } = "";
    public bool Enabled { get; set; }
    public string ClientPolicy { get; set; } = "allow-all";
    public string PoolStart { get; set; } = "";
    public string PoolEnd { get; set; } = "";
    public string Dns1 { get; set; } = "";
    public string Dns2 { get; set; } = "";
    public string Dns3 { get; set; } = "";
    public string Dns4 { get; set; } = "";
    public string Gateway { get; set; } = "";
    public string Domain { get; set; } = "";
    public int LeaseTime { get; set; } = 7200;
    public int MaxLeaseTime { get; set; } = 86400;
    public bool StaticArp { get; set; }
}

public class DhcpLease
{
    public string MacAddress { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string Hostname { get; set; } = "";
    public DateTime LeaseStart { get; set; }
    public DateTime LeaseEnd { get; set; }
    public string State { get; set; } = "active"; // active, expired, reserved
    public string Interface { get; set; } = "";
    public string Status { get; set; } = "active";
}
