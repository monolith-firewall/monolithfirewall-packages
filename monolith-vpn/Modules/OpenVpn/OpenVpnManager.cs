using System.Text.Json;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Platform.Validation;

namespace Monolith.Vpn.Modules.OpenVpn;

public class OpenVpnManager
{
    private readonly IModuleContext? _context;

    public OpenVpnManager(IModuleContext? context)
    {
        _context = context;
    }

    public async Task<OpenVpnSettings> GetSettingsAsync()
    {
        // For now, return default settings
        return new OpenVpnSettings
        {
            Enabled = false,
            Port = 1194,
            Protocol = "udp",
            Cipher = "AES-256-GCM",
            Auth = "SHA256",
            Compression = false,
            TlsAuth = true,
            LogLevel = "3",
            PushDns = true,
            DnsServers = new[] { "8.8.8.8", "8.8.4.4" }
        };
    }

    public async Task<bool> UpdateSettingsAsync(string settingsJson)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<OpenVpnSettings>(settingsJson);
            if (settings == null)
                return false;

            if (!ValidateSettings(settings, out var error))
            {
                _context?.Logger?.LogWarning($"OpenVPN settings validation failed: {error}");
                return false;
            }

            // In future, save to database and update OpenVPN config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<OpenVpnServer>> GetServersAsync()
    {
        // For now, return empty list
        // In future, load from database or parse OpenVPN configs
        return new List<OpenVpnServer>();
    }

    public async Task<List<OpenVpnClient>> GetClientsAsync()
    {
        // For now, return empty list
        // In future, load from database or parse OpenVPN configs
        return new List<OpenVpnClient>();
    }

    public async Task<bool> UpdateServerAsync(string serverJson)
    {
        try
        {
            var server = JsonSerializer.Deserialize<OpenVpnServerConfig>(serverJson);
            if (server == null)
                return false;

            if (!ValidateServer(server, out var error))
            {
                _context?.Logger?.LogWarning($"OpenVPN server validation failed: {error}");
                return false;
            }

            // In future, save to database and update OpenVPN config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateClientAsync(string clientJson)
    {
        try
        {
            var client = JsonSerializer.Deserialize<OpenVpnClientConfig>(clientJson);
            if (client == null)
                return false;

            if (!ValidateClient(client, out var error))
            {
                _context?.Logger?.LogWarning($"OpenVPN client validation failed: {error}");
                return false;
            }

            // In future, save to database and update OpenVPN config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteServerAsync(string serverId)
    {
        // For now, just return success
        // In future, remove from database and delete OpenVPN config
        return true;
    }

    public async Task<bool> DeleteClientAsync(string clientId)
    {
        // For now, just return success
        // In future, remove from database and delete OpenVPN config
        return true;
    }

    public async Task<bool> StartServerAsync(string serverId)
    {
        // For now, just return success
        // In future, execute: systemctl start openvpn@<server-id>
        return true;
    }

    public async Task<bool> StopServerAsync(string serverId)
    {
        // For now, just return success
        // In future, execute: systemctl stop openvpn@<server-id>
        return true;
    }

    public async Task<bool> StartClientAsync(string clientId)
    {
        // For now, just return success
        // In future, execute: systemctl start openvpn-client@<client-id>
        return true;
    }

    public async Task<bool> StopClientAsync(string clientId)
    {
        // For now, just return success
        // In future, execute: systemctl stop openvpn-client@<client-id>
        return true;
    }

    private static bool ValidateSettings(OpenVpnSettings settings, out string? error)
    {
        error = null;
        if (settings.Port <= 0 || settings.Port > 65535)
        {
            error = "Port must be 1-65535";
            return false;
        }

        if (!string.Equals(settings.Protocol, "udp", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(settings.Protocol, "tcp", StringComparison.OrdinalIgnoreCase))
        {
            error = "Protocol must be udp or tcp";
            return false;
        }

        if (settings.DnsServers != null && settings.DnsServers.Length > 0 &&
            !PlatformValidators.AreValidDnsServers(settings.DnsServers))
        {
            error = "One or more DNS servers are invalid";
            return false;
        }

        return true;
    }

    private static bool ValidateServer(OpenVpnServerConfig server, out string? error)
    {
        error = null;

        if (!string.IsNullOrWhiteSpace(server.Network) && !PlatformValidators.IsValidIp(server.Network))
        {
            error = "Network must be a valid IP address";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(server.Netmask) && !PlatformValidators.IsValidIpv4(server.Netmask))
        {
            error = "Netmask must be a valid IPv4 mask";
            return false;
        }

        if (server.Port <= 0 || server.Port > 65535)
        {
            error = "Port must be 1-65535";
            return false;
        }

        if (!string.Equals(server.Protocol, "udp", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(server.Protocol, "tcp", StringComparison.OrdinalIgnoreCase))
        {
            error = "Protocol must be udp or tcp";
            return false;
        }

        return true;
    }

    private static bool ValidateClient(OpenVpnClientConfig client, out string? error)
    {
        error = null;

        if (!string.IsNullOrWhiteSpace(client.ServerAddress) && !PlatformValidators.IsValidIp(client.ServerAddress))
        {
            error = "Server address must be a valid IP";
            return false;
        }

        if (client.ServerPort <= 0 || client.ServerPort > 65535)
        {
            error = "Server port must be 1-65535";
            return false;
        }

        if (!string.Equals(client.Protocol, "udp", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(client.Protocol, "tcp", StringComparison.OrdinalIgnoreCase))
        {
            error = "Protocol must be udp or tcp";
            return false;
        }

        return true;
    }
}
