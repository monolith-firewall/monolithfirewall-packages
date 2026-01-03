namespace Monolith.Vpn.Modules.Ipsec;

public class IpsecSettings
{
    public bool Enabled { get; set; } = false;
    public string Mode { get; set; } = "transport"; // transport, tunnel
    public bool NatTraversal { get; set; } = true;
    public bool DeadPeerDetection { get; set; } = true;
    public int DeadPeerDetectionInterval { get; set; } = 30; // seconds
    public string LogLevel { get; set; } = "info";
}

public class IpsecConnection
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Type { get; set; } = "site-to-site"; // site-to-site, remote-access
    public string LocalAddress { get; set; } = "";
    public string RemoteAddress { get; set; } = "";
    public string LocalSubnet { get; set; } = "";
    public string RemoteSubnet { get; set; } = "";
    public string AuthenticationMethod { get; set; } = "psk"; // psk, certificate
    public string PreSharedKey { get; set; } = "";
    public string EncryptionAlgorithm { get; set; } = "aes256";
    public string HashAlgorithm { get; set; } = "sha256";
    public string DiffieHellmanGroup { get; set; } = "modp2048";
    public int Lifetime { get; set; } = 3600; // seconds
    public string Status { get; set; } = "down"; // up, down, connecting
    public DateTime? LastConnected { get; set; }
}

public class IpsecConnectionConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Type { get; set; } = "site-to-site";
    public string LocalAddress { get; set; } = "";
    public string RemoteAddress { get; set; } = "";
    public string LocalSubnet { get; set; } = "";
    public string RemoteSubnet { get; set; } = "";
    public string AuthenticationMethod { get; set; } = "psk";
    public string PreSharedKey { get; set; } = "";
    public string EncryptionAlgorithm { get; set; } = "aes256";
    public string HashAlgorithm { get; set; } = "sha256";
    public string DiffieHellmanGroup { get; set; } = "modp2048";
    public int Lifetime { get; set; } = 3600;
}
