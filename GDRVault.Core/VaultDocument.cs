namespace GDRVault.Core;

public class VaultDocument
{
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public List<VaultEntry> Entries { get; set; } = new();
}