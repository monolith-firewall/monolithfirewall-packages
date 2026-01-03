using CL.SQLite.Models;

namespace Monolith.Network.Modules.Dhcp;

[SQLiteTable("dhcp_leases")]
[SQLiteIndex(new[] { "mac_address" }, IsUnique = true, Name = "idx_dhcp_leases_mac")]
[SQLiteIndex(new[] { "ip_address" }, Name = "idx_dhcp_leases_ip")]
public class DhcpLeaseEntity
{
    [SQLiteColumn(IsPrimaryKey = true, IsAutoIncrement = true)]
    public int Id { get; set; }

    [SQLiteColumn(IsNotNull = true, ColumnName = "mac_address", Size = 17)]
    public string MacAddress { get; set; } = "";

    [SQLiteColumn(IsNotNull = true, ColumnName = "ip_address", Size = 15)]
    public string IpAddress { get; set; } = "";

    [SQLiteColumn(ColumnName = "hostname", Size = 255)]
    public string? Hostname { get; set; }

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "lease_start", IsNotNull = true)]
    public DateTime LeaseStart { get; set; } = DateTime.UtcNow;

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "lease_end", IsNotNull = true)]
    public DateTime LeaseEnd { get; set; } = DateTime.UtcNow;

    [SQLiteColumn(IsNotNull = true, Size = 20)]
    public string State { get; set; } = "active";

    [SQLiteColumn(DataType = SQLiteDataType.DATETIME, ColumnName = "created_at", IsNotNull = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
