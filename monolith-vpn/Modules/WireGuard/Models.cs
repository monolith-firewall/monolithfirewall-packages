namespace Monolith.Vpn.Modules.WireGuard;

public class WireGuardSettings
{
    public bool Enabled { get; set; } = false;
    public int ListenPort { get; set; } = 51820;
    public string PrivateKey { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string[] AllowedIps { get; set; } = Array.Empty<string>();
    public bool ForwardTraffic { get; set; } = false;
    public string LogLevel { get; set; } = "info";
}

public class WireGuardInterface
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Address { get; set; } = "";
    public int ListenPort { get; set; } = 51820;
    public string PrivateKey { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string[] AllowedIps { get; set; } = Array.Empty<string>();
    public string[] DnsServers { get; set; } = Array.Empty<string>();
    public int Mtu { get; set; } = 1420;
    public string Status { get; set; } = "down"; // up, down
    public int ConnectedPeers { get; set; } = 0;
    public long BytesReceived { get; set; } = 0;
    public long BytesSent { get; set; } = 0;
    public DateTime? LastHandshake { get; set; }
}

public class WireGuardPeer
{
    public string Id { get; set; } = "";
    public string InterfaceId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string PublicKey { get; set; } = "";
    public string PresharedKey { get; set; } = "";
    public string AllowedIps { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public int PersistentKeepalive { get; set; } = 25; // seconds
    public string Status { get; set; } = "disconnected"; // connected, disconnected
    public long BytesReceived { get; set; } = 0;
    public long BytesSent { get; set; } = 0;
    public DateTime? LastHandshake { get; set; }
}

public class WireGuardInterfaceConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Address { get; set; } = "";
    public int ListenPort { get; set; } = 51820;
    public string PrivateKey { get; set; } = "";
    public string[] AllowedIps { get; set; } = Array.Empty<string>();
    public string[] DnsServers { get; set; } = Array.Empty<string>();
    public int Mtu { get; set; } = 1420;
}

public class WireGuardPeerConfig
{
    public string Id { get; set; } = "";
    public string InterfaceId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string PublicKey { get; set; } = "";
    public string PresharedKey { get; set; } = "";
    public string AllowedIps { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public int PersistentKeepalive { get; set; } = 25;
}
