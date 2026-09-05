namespace AiAlreadyDidIt.Api.Infrastructure.Scanning;

public enum ScanVerdict { Clean, Infected, Error, Skipped }

public sealed record ScanOutcome(ScanVerdict Verdict, string? Signature, string? Raw);

public interface IVirusScanner
{
    bool Enabled { get; }
    Task<ScanOutcome> ScanAsync(Stream content, long length, CancellationToken ct = default);
    Task<(bool Ok, string Detail)> PingAsync(CancellationToken ct = default);
}
