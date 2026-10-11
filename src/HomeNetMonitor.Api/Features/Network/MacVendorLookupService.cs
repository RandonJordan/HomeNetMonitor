using MacToVendorLookup;

namespace HomeNetMonitor.Api.Features.Network;

public class MacVendorLookupService
{
    public string? GetVendor(string? macAddress, bool isLocallyAdministered)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return null;

        if (isLocallyAdministered)
            return null;

        var result = MacVendorLookup.Lookup(macAddress);

        if (!result.IsValid || !result.IsFound)
            return null;

        return result.VendorName;
    }
}