using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace HomeNetMonitor.Api.Features.Network;

public class HostnameResolverService
{
    public async Task<string?> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(ipAddress,cancellationToken);

            return string.IsNullOrWhiteSpace(hostEntry.HostName) ? null : hostEntry.HostName;
        }
        catch(SocketException)
        {
            return null;
        }
    }
}