using CL.SQLite.Models;

namespace Monolith.Network.Modules.Dhcp;

[SQLiteTable("dhcp_leases")]
[SQLiteIndex(new[] { "mac_address" }, IsUnique = true, Name = "idx_dhcp_leases_mac")]
[SQLiteIndex(new[] { "ip_address" }, Name = "idx_dhcp_leases_ip")]
public class DhcpLeaseEntity
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(IsNotNull = true, ColumnName = "mac_address", DataType = SQLiteDataType.TEXT, Size = 17)]
    public string MacAddress { get; set; } = "";

    [SQLiteColumn(IsNotNull = true, ColumnName = "ip_address", DataType = SQLiteDataType.TEXT, Size = 15)]
    public string IpAddress { get; set; } = "";

    [SQLiteColumn(ColumnName = "hostname", DataType = SQLiteDataType.TEXT, Size = 255)]
    public string? Hostname { get; set; }

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "lease_start", IsNotNull = true)]
    public DateTime LeaseStart { get; set; } = DateTime.UtcNow;

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "lease_end", IsNotNull = true)]
    public DateTime LeaseEnd { get; set; } = DateTime.UtcNow;

    [SQLiteColumn(IsNotNull = true, DataType = SQLiteDataType.TEXT, Size = 20)]
    public string State { get; set; } = "active";

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "created_at", IsNotNull = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "updated_at", IsNotNull = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database entity for DHCP interface configuration.
/// Stores DHCP settings per network interface.
/// </summary>
[SQLiteTable("dhcp_interfaces")]
[SQLiteIndex(new[] { "interface_name" }, IsUnique = true, Name = "idx_dhcp_interfaces_name")]
public class DhcpInterfaceEntity
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(IsNotNull = true, ColumnName = "interface_name", DataType = SQLiteDataType.TEXT, Size = 32)]
    public string InterfaceName { get; set; } = "";

    [SQLiteColumn(IsNotNull = true, ColumnName = "enabled")]
    public bool Enabled { get; set; } = false;

    [SQLiteColumn(ColumnName = "subnet", DataType = SQLiteDataType.TEXT, Size = 18)] // e.g., "192.168.1.0/24"
    public string? Subnet { get; set; }

    [SQLiteColumn(ColumnName = "pool_start", DataType = SQLiteDataType.TEXT, Size = 15)]
    public string? PoolStart { get; set; }

    [SQLiteColumn(ColumnName = "pool_end", DataType = SQLiteDataType.TEXT, Size = 15)]
    public string? PoolEnd { get; set; }

    [SQLiteColumn(ColumnName = "gateway", DataType = SQLiteDataType.TEXT, Size = 15)]
    public string? Gateway { get; set; }

    [SQLiteColumn(ColumnName = "dns_servers", DataType = SQLiteDataType.TEXT)] // JSON array
    public string? DnsServers { get; set; } // JSON: ["8.8.8.8", "8.8.4.4"]

    [SQLiteColumn(ColumnName = "domain", DataType = SQLiteDataType.TEXT, Size = 255)]
    public string? Domain { get; set; }

    [SQLiteColumn(ColumnName = "lease_time", IsNotNull = true)]
    public int LeaseTime { get; set; } = 7200; // seconds

    [SQLiteColumn(ColumnName = "max_lease_time", IsNotNull = true)]
    public int MaxLeaseTime { get; set; } = 86400; // seconds

    [SQLiteColumn(ColumnName = "client_policy", DataType = SQLiteDataType.TEXT, Size = 20)]
    public string ClientPolicy { get; set; } = "allow-all"; // allow-all, deny-all, allow-known

    [SQLiteColumn(ColumnName = "static_arp", IsNotNull = true)]
    public bool StaticArp { get; set; } = false;

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "updated_at", IsNotNull = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database entity for DHCP global settings.
/// </summary>
[SQLiteTable("dhcp_settings")]
public class DhcpSettingsEntity
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(IsNotNull = true, ColumnName = "enabled")]
    public bool Enabled { get; set; } = false;

    [SQLiteColumn(ColumnName = "default_lease_time", IsNotNull = true)]
    public int DefaultLeaseTime { get; set; } = 7200; // seconds

    [SQLiteColumn(ColumnName = "max_lease_time", IsNotNull = true)]
    public int MaxLeaseTime { get; set; } = 86400; // seconds

    [SQLiteColumn(ColumnName = "dns_registration", IsNotNull = true)]
    public bool DnsRegistration { get; set; } = false;

    [SQLiteColumn(ColumnName = "log_level", DataType = SQLiteDataType.TEXT, Size = 20)]
    public string LogLevel { get; set; } = "info";

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "updated_at", IsNotNull = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
