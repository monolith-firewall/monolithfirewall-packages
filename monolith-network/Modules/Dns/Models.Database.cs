using CL.SQLite.Models;

namespace Monolith.Network.Modules.Dns;

/// <summary>
/// Database entity for DNS global settings.
/// </summary>
[SQLiteTable("dns_settings")]
public class DnsSettingsEntity
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(IsNotNull = true, ColumnName = "enabled")]
    public bool Enabled { get; set; } = false;

    [SQLiteColumn(ColumnName = "recursion", IsNotNull = true)]
    public bool Recursion { get; set; } = true;

    [SQLiteColumn(ColumnName = "forwarding", IsNotNull = true)]
    public bool Forwarding { get; set; } = false;

    [SQLiteColumn(ColumnName = "forwarders", DataType = SQLiteDataType.TEXT)] // JSON array
    public string? Forwarders { get; set; } // JSON: ["8.8.8.8", "8.8.4.4"]

    [SQLiteColumn(ColumnName = "log_level", DataType = SQLiteDataType.TEXT, Size = 20)]
    public string LogLevel { get; set; } = "info";

    [SQLiteColumn(ColumnName = "dnssec_validation", IsNotNull = true)]
    public bool DnssecValidation { get; set; } = true;

    [SQLiteColumn(ColumnName = "local_domain", DataType = SQLiteDataType.TEXT, Size = 255)]
    public string? LocalDomain { get; set; } // e.g., "local"

    [SQLiteColumn(ColumnName = "listen_interfaces", DataType = SQLiteDataType.TEXT)] // JSON array
    public string? ListenInterfaces { get; set; } // JSON: ["eth0", "eth1"]

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "updated_at", IsNotNull = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database entity for DNS zones.
/// </summary>
[SQLiteTable("dns_zones")]
[SQLiteIndex(new[] { "zone_name" }, IsUnique = true, Name = "idx_dns_zones_name")]
public class DnsZoneEntity
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(IsNotNull = true, ColumnName = "zone_name", DataType = SQLiteDataType.TEXT, Size = 255)]
    public string ZoneName { get; set; } = "";

    [SQLiteColumn(ColumnName = "zone_type", DataType = SQLiteDataType.TEXT, Size = 20)]
    public string ZoneType { get; set; } = "master"; // master, slave, forward, stub

    [SQLiteColumn(IsNotNull = true, ColumnName = "enabled")]
    public bool Enabled { get; set; } = false;

    [SQLiteColumn(ColumnName = "zone_file", DataType = SQLiteDataType.TEXT, Size = 512)]
    public string? ZoneFile { get; set; }

    [SQLiteColumn(ColumnName = "masters", DataType = SQLiteDataType.TEXT)] // JSON array for slave zones
    public string? Masters { get; set; } // JSON: ["192.168.1.10"]

    [SQLiteColumn(ColumnName = "allow_transfer", IsNotNull = true)]
    public bool AllowTransfer { get; set; } = false;

    [SQLiteColumn(ColumnName = "allow_transfer_to", DataType = SQLiteDataType.TEXT)] // JSON array
    public string? AllowTransferTo { get; set; } // JSON: ["192.168.1.0/24"]

    [SQLiteColumn(ColumnName = "ttl", IsNotNull = true)]
    public int Ttl { get; set; } = 3600;

    [SQLiteColumn(ColumnName = "soa_email", DataType = SQLiteDataType.TEXT, Size = 255)]
    public string? SoaEmail { get; set; }

    [SQLiteColumn(ColumnName = "refresh", IsNotNull = true)]
    public int Refresh { get; set; } = 86400;

    [SQLiteColumn(ColumnName = "retry", IsNotNull = true)]
    public int Retry { get; set; } = 7200;

    [SQLiteColumn(ColumnName = "expire", IsNotNull = true)]
    public int Expire { get; set; } = 604800;

    [SQLiteColumn(ColumnName = "negative_ttl", IsNotNull = true)]
    public int NegativeTtl { get; set; } = 3600;

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "updated_at", IsNotNull = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database entity for DNS records.
/// </summary>
[SQLiteTable("dns_records")]
[SQLiteIndex(new[] { "zone_name", "record_name" }, Name = "idx_dns_records_zone_name")]
public class DnsRecordEntity
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(IsNotNull = true, ColumnName = "zone_name", DataType = SQLiteDataType.TEXT, Size = 255)]
    public string ZoneName { get; set; } = "";

    [SQLiteColumn(IsNotNull = true, ColumnName = "record_name", DataType = SQLiteDataType.TEXT, Size = 255)]
    public string RecordName { get; set; } = "";

    [SQLiteColumn(IsNotNull = true, ColumnName = "record_type", DataType = SQLiteDataType.TEXT, Size = 10)]
    public string RecordType { get; set; } = "A"; // A, AAAA, CNAME, MX, TXT, NS, PTR, SRV

    [SQLiteColumn(IsNotNull = true, ColumnName = "record_data", DataType = SQLiteDataType.TEXT)]
    public string RecordData { get; set; } = "";

    [SQLiteColumn(ColumnName = "ttl", IsNotNull = true)]
    public int Ttl { get; set; } = 3600;

    [SQLiteColumn(ColumnName = "priority", IsNotNull = true)] // For MX and SRV records
    public int Priority { get; set; } = 0;

    [SQLiteColumn(IsNotNull = true, ColumnName = "enabled")]
    public bool Enabled { get; set; } = true;

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "updated_at", IsNotNull = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
