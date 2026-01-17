using System.Text.Json;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Platform.Validation;

namespace Monolith.Vpn.Modules.WireGuard;

public class WireGuardManager
{
    private readonly IModuleContext? _context;

    public WireGuardManager(IModuleContext? context)
    {
        _context = context;
    }

    public async Task<WireGuardSettings> GetSettingsAsync()
    {
        // For now, return default settings
        return new WireGuardSettings
        {
            Enabled = false,
            ListenPort = 51820,
            PrivateKey = "",
            PublicKey = "",
            AllowedIps = Array.Empty<string>(),
            ForwardTraffic = false,
            LogLevel = "info"
        };
    }

    public async Task<bool> UpdateSettingsAsync(string settingsJson)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<WireGuardSettings>(settingsJson);
            if (settings == null)
                return false;

            if (!ValidateWireGuardSettings(settings, out var error))
            {
                _context?.Logger?.LogWarning($"WireGuard settings validation failed: {error}");
                return false;
            }

            // In future, save to database and update WireGuard config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<WireGuardInterface>> GetInterfacesAsync()
    {
        // For now, return empty list
        // In future, load from database or parse WireGuard configs
        return new List<WireGuardInterface>();
    }

    public async Task<List<WireGuardPeer>> GetPeersAsync(string? interfaceId = null)
    {
        // For now, return empty list
        // In future, load from database or parse WireGuard configs
        return new List<WireGuardPeer>();
    }

    public async Task<bool> UpdateInterfaceAsync(string interfaceJson)
    {
        try
        {
            var interfaceConfig = JsonSerializer.Deserialize<WireGuardInterfaceConfig>(interfaceJson);
            if (interfaceConfig == null)
                return false;

            if (!ValidateInterface(interfaceConfig, out var error))
            {
                _context?.Logger?.LogWarning($"WireGuard interface validation failed: {error}");
                return false;
            }

            // In future, save to database and update WireGuard config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdatePeerAsync(string peerJson)
    {
        try
        {
            var peer = JsonSerializer.Deserialize<WireGuardPeerConfig>(peerJson);
            if (peer == null)
                return false;

            if (!ValidatePeer(peer, out var error))
            {
                _context?.Logger?.LogWarning($"WireGuard peer validation failed: {error}");
                return false;
            }

            // In future, save to database and update WireGuard config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteInterfaceAsync(string interfaceId)
    {
        // For now, just return success
        // In future, remove from database and delete WireGuard config
        return true;
    }

    public async Task<bool> DeletePeerAsync(string peerId)
    {
        // For now, just return success
        // In future, remove from database and delete peer from WireGuard config
        return true;
    }

    public async Task<bool> StartInterfaceAsync(string interfaceId)
    {
        // For now, just return success
        // In future, execute: wg-quick up <interface-name>
        return true;
    }

    public async Task<bool> StopInterfaceAsync(string interfaceId)
    {
        // For now, just return success
        // In future, execute: wg-quick down <interface-name>
        return true;
    }

    public async Task<string> GenerateKeyPairAsync()
    {
        // For now, return placeholder
        // In future, execute: wg genkey | tee privatekey | wg pubkey > publickey
        return "{\"privateKey\":\"\",\"publicKey\":\"\"}";
    }

    private static bool ValidateWireGuardSettings(WireGuardSettings settings, out string? error)
    {
        error = null;
        if (settings.ListenPort <= 0 || settings.ListenPort > 65535)
        {
            error = "Listen port must be 1-65535";
            return false;
        }

        if (settings.AllowedIps != null && settings.AllowedIps.Length > 0)
        {
            foreach (var cidr in settings.AllowedIps)
            {
                if (string.IsNullOrWhiteSpace(cidr) || !PlatformValidators.TryParseCidr(cidr, out _, out _))
                {
                    error = $"Invalid allowed IP: {cidr}";
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ValidateInterface(WireGuardInterfaceConfig cfg, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(cfg.Name) || !PlatformValidators.IsValidInterfaceName(cfg.Name))
        {
            error = "Interface name is required and must be valid";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(cfg.Address) &&
            !PlatformValidators.TryParseCidr(cfg.Address, out _, out _))
        {
            error = "Address must be a valid CIDR";
            return false;
        }

        if (cfg.ListenPort <= 0 || cfg.ListenPort > 65535)
        {
            error = "Listen port must be 1-65535";
            return false;
        }

        if (cfg.DnsServers != null && cfg.DnsServers.Length > 0 &&
            !PlatformValidators.AreValidDnsServers(cfg.DnsServers))
        {
            error = "One or more DNS servers are invalid";
            return false;
        }

        if (cfg.AllowedIps != null)
        {
            foreach (var cidr in cfg.AllowedIps)
            {
                if (!PlatformValidators.TryParseCidr(cidr, out _, out _))
                {
                    error = $"Invalid allowed IP: {cidr}";
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ValidatePeer(WireGuardPeerConfig peer, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(peer.PublicKey))
        {
            error = "Peer public key is required";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(peer.AllowedIps))
        {
            var entries = peer.AllowedIps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var cidr in entries)
            {
                if (!PlatformValidators.TryParseCidr(cidr, out _, out _))
                {
                    error = $"Invalid allowed IP: {cidr}";
                    return false;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(peer.Endpoint))
        {
            var parts = peer.Endpoint.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !PlatformValidators.IsValidIp(parts[0]) ||
                !int.TryParse(parts[1], out var port) || port <= 0 || port > 65535)
            {
                error = "Endpoint must be host:port with a valid IP and port";
                return false;
            }
        }

        return true;
    }
}
