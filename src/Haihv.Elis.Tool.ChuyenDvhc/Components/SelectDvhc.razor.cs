using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;
using Microsoft.AspNetCore.Components;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class SelectDvhc
{
    [Inject] private HybridCache HybridCache { get; set; } = null!;
    [Inject] private IMemoryCache MemoryCache { get; set; } = null!;
    [Inject] private ILogger Logger { get; set; } = null!;
    [Parameter] public bool IsBefore { get; set; }
    [Parameter] public DvhcRecord? CapTinh { get; set; }
    [Parameter] public EventCallback<DvhcRecord?> CapTinhChanged { get; set; }
    [Parameter] public DvhcRecord? CapHuyen { get; set; }
    [Parameter] public EventCallback<DvhcRecord?> CapHuyenChanged { get; set; }
    [Parameter] public IEnumerable<DvhcRecord> CapXas { get; set; } = [];
    [Parameter] public EventCallback<IEnumerable<DvhcRecord>> CapXaChanged { get; set; }

    private string? _connectionString = string.Empty;

    private IEnumerable<DvhcRecord> _capTinhs = [];
    private string _tenTinh = string.Empty;

    private IEnumerable<DvhcRecord> _capHuyens = [];
    private string _tenHuyen = string.Empty;

    private IEnumerable<DvhcRecord> _capXas = [];
    private bool _isConnected;
    private DonViHanhChinhRepository _donViHanhChinhRepository = null!;

    protected override void OnInitialized()
    {
        _connectionString = MemoryCache.Get<string>(CacheDataConnection.ConnectionString);
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _isConnected = true;
            _donViHanhChinhRepository = new DonViHanhChinhRepository(_connectionString, Logger);
            return;
        }

        Logger.Error("Connection string is null or empty");
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!_isConnected || _capTinhs.Any()) return;
        if (CapHuyen == null || _tenHuyen == CapHuyen.Ten) return;
        _tenHuyen = CapHuyen.Ten;
        await GetCapXa();
    }

    private async Task<IEnumerable<DvhcRecord>> GetCapTinh(string? value, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_capTinhs.Any())
                return string.IsNullOrWhiteSpace(value)
                    ? _capTinhs
                    : _capTinhs.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
            var dvhcRecords =
                await HybridCache.GetOrCreateAsync(CacheData.CapTinh,  _ =>
                        _donViHanhChinhRepository.GetCapTinhAsync(), 
                    cancellationToken: cancellationToken);

            _capTinhs = dvhcRecords;
            return string.IsNullOrWhiteSpace(value)
                ? _capTinhs
                : _capTinhs.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Logger.Error(exception, "Lỗi khi lấy danh sách cấp tỉnh");
            throw;
        }

    }

    private async Task<IEnumerable<DvhcRecord>> GetCapHuyen(string? value,
        CancellationToken cancellationToken = default)
    {
        if (CapTinh is not { Ma: > 0 }) return [];
        if (_capHuyens.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capHuyens
                : _capHuyens.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        var dvhcRecords =
            await HybridCache.GetOrCreateAsync(CacheData.CapHuyen(CapTinh.Ma), async _ =>
                    await _donViHanhChinhRepository.GetCapHuyenAsync(CapTinh.Ma),
                cancellationToken: cancellationToken);

        _capHuyens = dvhcRecords;
        return string.IsNullOrWhiteSpace(value)
            ? _capHuyens
            : _capHuyens.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task GetCapXa(CancellationToken cancellationToken = default)
    {
        if (CapHuyen is not { Ma: > 0 }) return;
        var dvhcRecords =
            await HybridCache.GetOrCreateAsync(CacheData.CapXa(CapHuyen.Ma), async _ =>
                    await _donViHanhChinhRepository.GetCapXaAsync(CapHuyen.Ma)
                , cancellationToken: cancellationToken);
        _capXas = dvhcRecords;
    }


    private static string? DvhcToString(DvhcRecord? dvhcRecord)
    {
        return dvhcRecord?.Ten;
    }

    private void SetCapTinh()
    {
        if (CapTinh is not { Ma: > 0 } || CapTinh.Ten == _tenTinh) return;
        _capHuyens = [];
        CapHuyen = null;
        CapXas = [];
        _tenTinh = CapTinh.Ten;
        CapTinhChanged.InvokeAsync(CapTinh);
    }

    private void SetCapHuyen()
    {
        if (CapHuyen is not { Ma: > 0 } || CapHuyen.Ten == _tenHuyen) return;
        _tenHuyen = CapHuyen.Ten;
        CapXas = [];
        CapHuyenChanged.InvokeAsync(CapHuyen);
        _ = GetCapXa();
    }

    private static string? CapXaToString(DvhcRecord? capXa)
    {
        return capXa?.Ten;
    }

    private void SetCapXa(IEnumerable<DvhcRecord?>? dvhcRecords)
    {
        CapXas = dvhcRecords?.Where(x => x != null).Select(x => x!).ToList() ?? [];
        CapXaChanged.InvokeAsync(CapXas);
    }
}