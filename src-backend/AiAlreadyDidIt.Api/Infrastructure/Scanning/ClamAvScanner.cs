using Microsoft.Extensions.Options;
using nClam;

namespace AiAlreadyDidIt.Api.Infrastructure.Scanning;

/// <summary>clamd over TCP (INSTREAM). The compose stack runs the official clamav/clamav image.</summary>
public sealed class ClamAvScanner(IOptions<ClamAvOptions> options, ILogger<ClamAvScanner> logger) : IVirusScanner
{
    private readonly ClamAvOptions _o = options.Value;

    public bool Enabled => _o.Enabled;

    public async Task<ScanOutcome> ScanAsync(Stream content, long length, CancellationToken ct = default)
    {
        if (!_o.Enabled) return new ScanOutcome(ScanVerdict.Skipped, null, "scanner disabled");
        if (length > _o.MaxScanBytes) return new ScanOutcome(ScanVerdict.Skipped, null, $"file larger than {_o.MaxScanBytes} bytes");
        try
        {
            var client = new ClamClient(_o.Host, _o.Port) { MaxStreamSize = Math.Max(_o.MaxScanBytes, length + 1) };
            var result = await client.SendAndScanFileAsync(content, ct);
            return result.Result switch
            {
                ClamScanResults.Clean => new ScanOutcome(ScanVerdict.Clean, null, result.RawResult),
                ClamScanResults.VirusDetected => new ScanOutcome(ScanVerdict.Infected, result.InfectedFiles?.FirstOrDefault()?.VirusName, result.RawResult),
                _ => new ScanOutcome(ScanVerdict.Error, null, result.RawResult)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ClamAV scan failed ({Host}:{Port})", _o.Host, _o.Port);
            return new ScanOutcome(ScanVerdict.Error, null, ex.Message);
        }
    }

    public async Task<(bool Ok, string Detail)> PingAsync(CancellationToken ct = default)
    {
        if (!_o.Enabled) return (false, "disabled");
        try
        {
            var client = new ClamClient(_o.Host, _o.Port);
            var ok = await client.PingAsync(ct);
            if (!ok) return (false, "no PONG");
            var version = await client.GetVersionAsync(ct);
            return (true, version.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
