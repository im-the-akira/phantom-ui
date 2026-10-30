using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PhantomUI;

public sealed class DnsProviderResult : INotifyPropertyChanged
{
    public string Name { get; init; } = "";
    public string Primary { get; init; } = "";
    public string Secondary { get; init; } = "";
    public string NotesPt { get; init; } = "";
    public string NotesEn { get; init; } = "";
    private double _avgMs = -1;
    private string _status = "—";

    public string Notes => Loc.Pick(NotesPt, NotesEn);
    public string Pair => $"{Primary}  ·  {Secondary}";

    public double AvgMs
    {
        get => _avgMs;
        set { _avgMs = value; OnChanged(nameof(AvgMs)); OnChanged(nameof(LatencyText)); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnChanged(nameof(Status)); }
    }

    public string LatencyText => AvgMs < 0 ? Status : $"{AvgMs:0.0} ms";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/// <summary>
/// Benchmark paralelo real de resolução UDP DNS + ICMP e troca 1-clique no adaptador ativo (substitui DnsJumper nativamente).
/// </summary>
public static class DnsBenchmark
{
    public static List<DnsProviderResult> DefaultProviders() => new()
    {
        new() { Name = "Cloudflare (Gaming / 1.1.1.1)", Primary = "1.1.1.1", Secondary = "1.0.0.1", NotesPt = "Anycast ultrarrápido global, sem filtros", NotesEn = "Ultra-fast global Anycast, unfiltered" },
        new() { Name = "Google Public DNS", Primary = "8.8.8.8", Secondary = "8.8.4.4", NotesPt = "Alta estabilidade global e rotas diretas", NotesEn = "High global reliability and peering" },
        new() { Name = "Quad9 Security", Primary = "9.9.9.9", Secondary = "149.112.112.112", NotesPt = "Bloqueio de domínios maliciosos com privacidade", NotesEn = "Malware-blocking DNS with privacy" },
        new() { Name = "Cloudflare Malware Block", Primary = "1.1.1.2", Secondary = "1.0.0.2", NotesPt = "Mesma rota Cloudflare + filtro anti-phishing", NotesEn = "Cloudflare speed + anti-malware filter" },
        new() { Name = "OpenDNS (Cisco)", Primary = "208.67.222.222", Secondary = "208.67.220.220", NotesPt = "Clássico estável para jogos competitivos", NotesEn = "Stable classic resolver for competitive gaming" },
        new() { Name = "AdGuard DNS", Primary = "94.140.14.14", Secondary = "94.140.15.15", NotesPt = "Bloqueia anúncios e rastreadores por DNS", NotesEn = "Blocks ads and trackers at DNS level" },
        new() { Name = "Level3 / Lumen", Primary = "4.2.2.1", Secondary = "4.2.2.2", NotesPt = "Backbone Tier-1 direto", NotesEn = "Direct Tier-1 backbone resolver" },
        new() { Name = "CleanBrowsing Security", Primary = "185.228.168.9", Secondary = "185.228.169.9", NotesPt = "Filtro de segurança leve", NotesEn = "Lightweight security filter" }
    };

    public static async Task RunBenchmarkAsync(List<DnsProviderResult> list, CancellationToken ct)
    {
        foreach (var p in list) { p.AvgMs = -1; p.Status = Loc.Pick("Testando…", "Testing…"); }

        var tasks = list.Select(async item =>
        {
            var samples = new List<double>();
            for (int i = 0; i < 3 && !ct.IsCancellationRequested; i++)
            {
                double? ms = await QueryDnsUdpMsAsync(item.Primary, "cloudflare.com", 1200, ct);
                if (!ms.HasValue)
                    ms = await PingMsAsync(item.Primary, 1000);
                if (ms.HasValue) samples.Add(ms.Value);
            }
            if (samples.Count > 0)
            {
                item.AvgMs = samples.Average();
                item.Status = "OK";
            }
            else
            {
                item.AvgMs = -1;
                item.Status = "Timeout";
            }
        });
        await Task.WhenAll(tasks);
    }

    private static async Task<double?> QueryDnsUdpMsAsync(string ip, string domain, int timeoutMs, CancellationToken ct)
    {
        try
        {
            byte[] packet = BuildDnsQuery(domain);
            using var udp = new UdpClient();
            var ep = new IPEndPoint(IPAddress.Parse(ip), 53);
            var sw = Stopwatch.StartNew();
            await udp.SendAsync(packet, packet.Length, ep);
            var recvTask = udp.ReceiveAsync(ct).AsTask();
            var completed = await Task.WhenAny(recvTask, Task.Delay(timeoutMs, ct));
            sw.Stop();
            if (completed == recvTask && recvTask.Result.Buffer.Length > 12)
                return sw.Elapsed.TotalMilliseconds;
        }
        catch { }
        return null;
    }

    private static async Task<double?> PingMsAsync(string ip, int timeoutMs)
    {
        try
        {
            using var ping = new Ping();
            var r = await ping.SendPingAsync(ip, timeoutMs);
            if (r.Status == IPStatus.Success) return r.RoundtripTime;
        }
        catch { }
        return null;
    }

    private static byte[] BuildDnsQuery(string domain)
    {
        var bytes = new List<byte>
        {
            0xAB, 0xCD, // ID
            0x01, 0x00, // Standard recursive query
            0x00, 0x01, // QDCOUNT = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        foreach (var label in domain.Split('.'))
        {
            bytes.Add((byte)label.Length);
            foreach (char c in label) bytes.Add((byte)c);
        }
        bytes.Add(0x00);
        bytes.AddRange(new byte[] { 0x00, 0x01, 0x00, 0x01 }); // Type A, Class IN
        return bytes.ToArray();
    }

    public static string? GetActiveInterfaceName()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                         && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                         && n.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();
            return nic?.Name;
        }
        catch { return null; }
    }

    public static (bool Ok, string Message) ApplyDnsToActiveNic(string primary, string secondary)
    {
        string? nic = GetActiveInterfaceName();
        if (string.IsNullOrWhiteSpace(nic))
            return (false, Loc.Pick("Nenhum adaptador de rede ativo com gateway IPv4 encontrado.", "No active network adapter with IPv4 gateway found."));

        var r1 = TweakJournal.RunProcess("netsh.exe", $"interface ipv4 set dnsservers name=\"{nic}\" static {primary} primary validate=no", 10_000);
        var r2 = TweakJournal.RunProcess("netsh.exe", $"interface ipv4 add dnsservers name=\"{nic}\" {secondary} index=2 validate=no", 10_000);
        TweakJournal.RunProcess("ipconfig.exe", "/flushdns", 6000);

        return r1.ExitCode == 0
            ? (true, Loc.Pick($"DNS {primary} / {secondary} aplicado em '{nic}' + cache limpo!", $"DNS {primary} / {secondary} applied on '{nic}' + DNS flushed!"))
            : (false, $"Falha ao aplicar DNS em '{nic}': {r1.Error} {r1.Output}");
    }

    public static (bool Ok, string Message) RestoreDhcpDns()
    {
        string? nic = GetActiveInterfaceName();
        if (string.IsNullOrWhiteSpace(nic))
            return (false, Loc.Pick("Nenhum adaptador ativo encontrado.", "No active adapter found."));

        var r = TweakJournal.RunProcess("netsh.exe", $"interface ipv4 set dnsservers name=\"{nic}\" source=dhcp", 10_000);
        TweakJournal.RunProcess("ipconfig.exe", "/flushdns", 6000);
        return r.ExitCode == 0
            ? (true, Loc.Pick($"DNS restaurado para Automático (DHCP) em '{nic}'.", $"DNS restored to Automatic (DHCP) on '{nic}'."))
            : (false, r.Error);
    }
}
