using System.Data.SqlTypes;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace HomeNetMonitor.Api.Features.Network;

public class ArpTableService
{
    public async Task<IReadOnlyList<ArpEntryResponse>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/usr/sbin/arp",
            Arguments = "-a",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return ParseEntries(output);

    }

    private static IReadOnlyList<ArpEntryResponse> ParseEntries(string output)
    {
        var entries = new List<ArpEntryResponse>();

        var lines = output.Split(
            Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach(var line in lines)
        {
            var match = Regex.Match(
                line,
                @"\((?<ip>\d{1,3}(?:\.\d{1,3}){3})\)\s+at\s+(?<mac>(?:[0-9a-fA-F]{1,2}:){5}[0-9a-fA-F]{1,2})\s+on\s+(?<interface>\S+)"
            );

            if(!match.Success)
            {
                continue;
            }

            var ipAddress = match.Groups["ip"].Value;
            var macAddress = NormalizedMacAddress(
                match.Groups["mac"].Value
            );

            var interfacename = match.Groups["interface"].Value;

            entries.Add(
                new ArpEntryResponse(
                    IpAddress: ipAddress,
                    MacAddress: macAddress,
                    InterfaceName: interfacename
                )
            );
            
        }

        return entries;
    }


    private static string NormalizedMacAddress(string macAddress)
    {
        return string.Join(":", macAddress .Split(':').Select(part => part.PadLeft(2, '0').ToUpperInvariant()));
    }

}