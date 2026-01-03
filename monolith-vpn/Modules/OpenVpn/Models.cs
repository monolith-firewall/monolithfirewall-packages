namespace Monolith.Vpn.Modules.OpenVpn;

public class OpenVpnSettings
{
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 1194;
    public string Protocol { get; set; } = "udp"; // udp, tcp
    public string Cipher { get; set; } = "AES-256-GCM";
    public string Auth { get; set; } = "SHA256";
    public bool Compression { get; set; } = false;
    public bool TlsAuth { get; set; } = true;
    public string LogLevel { get; set; } = "3";
    public bool PushDns { get; set; } = true;
    public string[] DnsServers { get; set; } = new[] { "8.8.8.8", "8.8.4.4" };
}

public class OpenVpnServer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Mode { get; set; } = "server"; // server, client
    public string Interface { get; set; } = "tun0";
    public string Network { get; set; } = "10.8.0.0";
    public string Netmask { get; set; } = "255.255.255.0";
    public int Port { get; set; } = 1194;
    public string Protocol { get; set; } = "udp";
    public string Status { get; set; } = "down"; // up, down, starting
    public int ConnectedClients { get; set; } = 0;
    public DateTime? LastStarted { get; set; }
}

public class OpenVpnClient
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string ServerAddress { get; set; } = "";
    public int ServerPort { get; set; } = 1194;
    public string Protocol { get; set; } = "udp";
    public string Status { get; set; } = "down"; // up, down, connecting
    public string LocalIp { get; set; } = "";
    public string RemoteIp { get; set; } = "";
    public DateTime? LastConnected { get; set; }
    public long BytesReceived { get; set; } = 0;
    public long BytesSent { get; set; } = 0;
}

public class OpenVpnServerConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Interface { get; set; } = "tun0";
    public string Network { get; set; } = "10.8.0.0";
    public string Netmask { get; set; } = "255.255.255.0";
    public int Port { get; set; } = 1194;
    public string Protocol { get; set; } = "udp";
    public string Cipher { get; set; } = "AES-256-GCM";
    public string Auth { get; set; } = "SHA256";
    public bool Compression { get; set; } = false;
    public bool TlsAuth { get; set; } = true;
}

public class OpenVpnClientConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string ServerAddress { get; set; } = "";
    public int ServerPort { get; set; } = 1194;
    public string Protocol { get; set; } = "udp";
    public string Cipher { get; set; } = "AES-256-GCM";
    public string Auth { get; set; } = "SHA256";
    public bool Compression { get; set; } = false;
}
