namespace AssemblyStudio.Runtime.Memory;

/// <summary>
/// A contiguous range of physical memory addresses [StartAddress, StartAddress + Length).
/// Used by change highlighting (FR-056) and snapshot memory windows.
/// </summary>
/// <param name="StartAddress">The 20-bit starting physical address (0..0xFFFFF).</param>
/// <param name="Length">The number of bytes in this range (positive).</param>
public readonly record struct AddressRange(int StartAddress, int Length)
{
    /// <summary>
    /// The exclusive end physical address (StartAddress + Length).
    /// </summary>
    public int EndAddress => StartAddress + Length;

    /// <summary>
    /// Checks whether the given physical address falls within this contiguous range.
    /// </summary>
    /// <param name="physicalAddress">20-bit physical address to check.</param>
    /// <returns><see langword="true"/> if StartAddress &lt;= physicalAddress &lt; EndAddress.</returns>
    public bool Contains(int physicalAddress) =>
        physicalAddress >= StartAddress && physicalAddress < EndAddress;

    /// <summary>
    /// Checks whether this range is contiguous with or adjacent to the subsequent address.
    /// </summary>
    public bool CanMergeWith(int physicalAddress) =>
        physicalAddress == EndAddress;
}
