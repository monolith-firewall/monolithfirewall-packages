using System.Text.Json;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Platform.Validation;

namespace Monolith.Vpn.Modules.Ipsec;

public class IpsecManager
{
    private readonly IModuleContext? _context;

    public IpsecManager(IModuleContext? context)
    {
        _context = context;
    }

    public async Task<IpsecSettings> GetSettingsAsync()
    {
        // For now, return default settings
        // In future, load from database or config file
        return new IpsecSettings
        {
            Enabled = false,
            Mode = "transport",
            NatTraversal = true,
            DeadPeerDetection = true,
            DeadPeerDetectionInterval = 30,
            LogLevel = "info"
        };
    }

    public async Task<bool> UpdateSettingsAsync(string settingsJson)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<IpsecSettings>(settingsJson);
            if (settings == null)
                return false;

            if (!ValidateSettings(settings, out var error))
            {
                _context?.Logger?.LogWarning($"IPsec settings validation failed: {error}");
                return false;
            }

            // In future, save to database and update strongSwan/ipsec config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<IpsecConnection>> GetConnectionsAsync()
    {
        // For now, return empty list
        // In future, load from database or parse ipsec config
        return new List<IpsecConnection>();
    }

    public async Task<bool> UpdateConnectionAsync(string connectionJson)
    {
        try
        {
            var connection = JsonSerializer.Deserialize<IpsecConnectionConfig>(connectionJson);
            if (connection == null)
                return false;

            if (!ValidateConnection(connection, out var error))
            {
                _context?.Logger?.LogWarning($"IPsec connection validation failed: {error}");
                return false;
            }

            // In future, save to database and update ipsec config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteConnectionAsync(string connectionId)
    {
        // For now, just return success
        // In future, remove from database and update ipsec config
        return true;
    }

    public async Task<bool> StartConnectionAsync(string connectionId)
    {
        // For now, just return success
        // In future, execute: ipsec up <connection-id>
        return true;
    }

    public async Task<bool> StopConnectionAsync(string connectionId)
    {
        // For now, just return success
        // In future, execute: ipsec down <connection-id>
        return true;
    }

    private static bool ValidateSettings(IpsecSettings settings, out string? error)
    {
        error = null;
        if (!string.Equals(settings.Mode, "transport", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(settings.Mode, "tunnel", StringComparison.OrdinalIgnoreCase))
        {
            error = "Mode must be transport or tunnel";
            return false;
        }

        if (settings.DeadPeerDetectionInterval <= 0)
        {
            error = "DPD interval must be greater than zero";
            return false;
        }

        return true;
    }

    private static bool ValidateConnection(IpsecConnectionConfig conn, out string? error)
    {
        error = null;

        if (!string.IsNullOrWhiteSpace(conn.LocalAddress) && !PlatformValidators.IsValidIp(conn.LocalAddress))
        {
            error = "Local address must be a valid IP";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(conn.RemoteAddress) && !PlatformValidators.IsValidIp(conn.RemoteAddress))
        {
            error = "Remote address must be a valid IP";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(conn.LocalSubnet) &&
            !PlatformValidators.TryParseCidr(conn.LocalSubnet, out _, out _))
        {
            error = "Local subnet must be a valid CIDR";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(conn.RemoteSubnet) &&
            !PlatformValidators.TryParseCidr(conn.RemoteSubnet, out _, out _))
        {
            error = "Remote subnet must be a valid CIDR";
            return false;
        }

        if (conn.Lifetime <= 0)
        {
            error = "Lifetime must be greater than zero";
            return false;
        }

        return true;
    }
}
