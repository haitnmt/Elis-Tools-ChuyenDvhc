using Haihv.Elis.Tool.ChuyenDvhc.Data;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using Serilog;
using Color = MudBlazor.Color;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class ProcessingDataTransfer
{
    [Inject] private HybridCache HybridCache { get; set; } = null!;
    [Inject] private IMemoryCache MemoryCache { get; set; } = null!;
    [Inject] private ILogger Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Parameter] public bool IsAuditEnabled { get; set; } = false;
    [Parameter] public EventCallback<bool> IsFinishedChanged { get; set; }

    private string? _connectionString;
    private bool _isFinished;
    private List<ThamChieuToBanDo> _thamChieuToBanDos = [];
    private string _toBanDoCu = string.Empty;
    private string _ghiChuToBanDo = string.Empty;
    private string _ghiChuThuaDat = string.Empty;
    private string _ghiChuGiayChungNhan = string.Empty;
    private string _ngaySapNhap = string.Empty;
    private bool _renewPrimaryKey = false;
    private int _limit = 100;

    private List<DvhcRecord?>? _capXaTruoc;
    private List<int> _maDvhcBiSapNhap = [];
    private DvhcRecord? _capXaSau;

    protected override async Task OnInitializedAsync()
    {
        _connectionString = MemoryCache.Get<string>(CacheDataConnection.ConnectionString);
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _thamChieuToBanDos = await HybridCache.GetOrCreateAsync(CacheThamSoBanDo.ThamChieuToBanDo,
                _ => ValueTask.FromResult(_thamChieuToBanDos));
            if (_thamChieuToBanDos.Count == 0)
            {
                const string message = "Dữ liệu tham chiếu tờ bản đồ không tồn tại.";
                Logger.Warning(message);
                SetMessage(message);
                return;
            }

            _capXaTruoc =
                await HybridCache.GetOrCreateAsync(CacheThamSoDvhc.CapXaTruoc, _ => ValueTask.FromResult(_capXaTruoc));
            if (_capXaTruoc == null || _capXaTruoc.Count == 0)
            {
                const string message = "Dữ liệu đơn vị hành chính cấp xã trước không tồn tại.";
                Logger.Warning(message);
                SetMessage(message);
                return;
            }

            _capXaSau = await HybridCache.GetOrCreateAsync(CacheThamSoDvhc.CapXaSau,
                _ => ValueTask.FromResult(_capXaSau));
            if (_capXaSau == null)
            {
                const string message = "Dữ liệu đơn vị hành chính cấp xã sau không tồn tại.";
                Logger.Warning(message);
                SetMessage(message);
                return;
            }

            _maDvhcBiSapNhap = _capXaTruoc?.Where(x => x != null)
                .Select(x => x!.MaDvhc)
                .ToList() ?? [];
            _maDvhcBiSapNhap.Remove(_capXaSau?.MaDvhc ?? 0);
            _toBanDoCu = await HybridCache.GetOrCreateAsync(CacheThamSoDuLieu.ToBanDoCu,
                _ => ValueTask.FromResult(ThamSoThayThe.DefaultToBanDoCu));
            _ghiChuToBanDo = await HybridCache.GetOrCreateAsync(CacheThamSoDuLieu.GhiChuToBanDo,
                _ => ValueTask.FromResult(ThamSoThayThe.DefaultGhiChuToBanDo));
            _ghiChuThuaDat = await HybridCache.GetOrCreateAsync(CacheThamSoDuLieu.GhiChuThuaDat,
                _ => ValueTask.FromResult(ThamSoThayThe.DefaultGhiChuThuaDat));
            _ghiChuGiayChungNhan = await HybridCache.GetOrCreateAsync(CacheThamSoDuLieu.GhiChuGiayChungNhan,
                _ => ValueTask.FromResult(ThamSoThayThe.DefaultGhiChuGiayChungNhan));
            _ngaySapNhap = await HybridCache.GetOrCreateAsync(CacheThamSoDvhc.NgaySatNhap,
                _ => ValueTask.FromResult(DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap)));
        }
        else
        {
            const string message = "Kết nối cơ sở dữ liệu không tồn tại.";
            Logger.Warning(message);
            SetMessage(message);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_isFinished) return;
        await StartProcessing();
    }

    private async Task SkipAuditDatabaseAsync()
    {
        IsAuditEnabled = false;
        await StartProcessing();
    }

    private async Task StartProcessing()
    {
        // Tạo hoặc thay đổi bảng audit nếu được kích hoạt
        await CreateAuditTable();

        if (_isCompletedKhoiTaoDuLieu || !IsAuditEnabled)
        {
            Logger.Information("Bắt đầu xử lý dữ liệu chuyển đổi.");
            if (_maDvhcBiSapNhap.Count == 0)
            {
                _messageSuccessThamChieuThuaDat = "Không có đơn vị hành chính nào được sáp nhập.";
                _messageUpdateToBanDoSuccess = "Không có đơn vị hành chính nào được sáp nhập.";
                _colorThamChieuThuaDat = Color.Success;
                _colorUpdateToBanDo = Color.Success;
                _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _isCompletedThamChieuThuaDat = true;
                _isCompletedUpdateToBanDo = true;
                const string message = "Không có đơn vị hành chính nào được sáp nhập.";
                Logger.Warning(message);
                SetMessage(message, Severity.Info, false);
                // Cập nhật dữ liệu Đơn vị hành chính
                await UpdatingDonViHanhChinh();
            }
            else
            {
                // Cập nhật dữ liệu Thửa đất lịch sử
                await CreateThamChieuThuaDat();
            }
        }
    }

    /// <summary>
    /// Đánh dấu quá trình xử lý đã hoàn thành và kích hoạt sự kiện thay đổi trạng thái.
    /// </summary>
    private void SetMessage(string message = "Có lỗi xảy ra", Severity severity = Severity.Error,
        bool isFinished = true)
    {
        Snackbar.Add(message, severity);
        if (!isFinished) return;
        _isFinished = true;
        IsFinishedChanged.InvokeAsync(_isFinished);
    }

    private string _timeKhoiTaoDuLieu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private Color _colorKhoiTaoDuLieu = Color.Default;
    private bool _isCompletedKhoiTaoDuLieu = false;
    private bool _isProcessingKhoiTaoDuLieu = false;
    private string? _errorKhoiTaoDuLieu = string.Empty;

    /// <summary>
    /// Tạo hoặc thay đổi bảng audit nếu được kích hoạt.
    /// </summary>
    /// <returns>Task đại diện cho thao tác không đồng bộ.</returns>
    private async Task CreateAuditTable()
    {
        // Kiểm tra nếu audit được kích hoạt, _dataContext không null, và quá trình chưa hoàn thành hoặc đang xử lý
        if (!IsAuditEnabled || _isCompletedKhoiTaoDuLieu || _isProcessingKhoiTaoDuLieu)
            return;
        _colorKhoiTaoDuLieu = Color.Primary;
        StateHasChanged();
        try
        {
            // Tạo hoặc thay đổi bảng audit
            var dataInitializer = new DataInitializer(_connectionString!);
            await dataInitializer.CreatedOrAlterAuditTable();
            _isCompletedKhoiTaoDuLieu = true;
            _colorKhoiTaoDuLieu = Color.Success;
        }
        catch (Exception ex)
        {
            // Xử lý lỗi nếu có ngoại lệ
            _errorKhoiTaoDuLieu = ex.Message;
            _colorKhoiTaoDuLieu = Color.Error;
            const string message = "Không thể tạo bảng audit.";
            _isCompletedKhoiTaoDuLieu = false;
            Logger.Error(ex, message);
            SetMessage(message);
        }
        finally
        {
            // Cập nhật thời gian, trạng thái hoàn thành và trạng thái xử lý
            _timeKhoiTaoDuLieu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            StateHasChanged();
        }
    }

    private Color _colorThamChieuThuaDat = Color.Default;
    private string _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedThamChieuThuaDat = false;
    private bool _isProcessingThamChieuThuaDat = false;
    private string _messageSuccessThamChieuThuaDat = "Đã hoàn thành";
    private string? _errorThamChieuThuaDat = string.Empty;
    private long _totalThamChieuThuaDat;
    private long _currentThamChieuThuaDat;
    private long _bufferThamChieuThuaDat;

    private async Task CreateThamChieuThuaDat()
    {
        if (_isProcessingThamChieuThuaDat || _isCompletedThamChieuThuaDat || _capXaTruoc == null ||
            _capXaTruoc.Count == 0 || _capXaSau == null)
            return;
        Logger.Information("Bắt đầu tạo dữ liệu thửa đất lịch sử.");
        _colorThamChieuThuaDat = Color.Primary;
        _isProcessingThamChieuThuaDat = true;
        StateHasChanged();
        try
        {
            var thuaDatRepository = new ThuaDatRepository(_connectionString!, Logger);
            // Lấy tổng số thửa đất
            _totalThamChieuThuaDat = await thuaDatRepository.GetCountThuaDatAsync(_maDvhcBiSapNhap);
            if (_totalThamChieuThuaDat == 0)
            {
                const string message = "Không có thửa đất nào được tìm thấy.";
                _messageUpdateToBanDoSuccess = message;
                _colorThamChieuThuaDat = Color.Success;
                _colorUpdateToBanDo = Color.Success;
                _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _isCompletedThamChieuThuaDat = true;
                _isCompletedUpdateToBanDo = true;
                SetMessage(message, Severity.Info, false);
                // Cập nhật dữ liệu Đơn vị hành chính
                await UpdatingDonViHanhChinh();
                return;
            }

            var giayChungNhanRepository = new GiayChungNhanRepository(_connectionString!, Logger);
            Logger.Information("Số lượng thửa đất cần cập nhật: {TotalThamChieuThuaDat}", _totalThamChieuThuaDat);
            _currentThamChieuThuaDat = 0;
            _bufferThamChieuThuaDat = Math.Min(_limit, _totalThamChieuThuaDat);
            StateHasChanged();

            foreach (var dvhcBiSapNhap in _capXaTruoc.Where(x => x != null && _maDvhcBiSapNhap.Contains(x.MaDvhc)))
            {
                if (dvhcBiSapNhap == null)
                    continue;
                var minMaThuaDat = long.MinValue;
                while (true)
                {
                    // Lấy danh sách Thửa Đất cần cập nhật và cập nhật ghi chú Thửa Đất
                    var thuaDatToBanDos =
                        await thuaDatRepository.UpdateAndGetThuaDatToBanDoAsync(dvhcBiSapNhap,
                            minMaThuaDat: minMaThuaDat,
                            limit: _limit,
                            formatGhiChuThuaDat: _ghiChuThuaDat,
                            ngaySapNhap: _ngaySapNhap);
                    if (thuaDatToBanDos.Count == 0)
                        break;

                    // Tạo hoặc cập nhật thông tin Thửa Đất Cũ
                    await thuaDatRepository.CreateOrUpdateThuaDatCuAsync(thuaDatToBanDos, _toBanDoCu);

                    // Cập nhật Ghi chú Giấy chứng nhận
                    await giayChungNhanRepository.UpdateGhiChuGiayChungNhan(thuaDatToBanDos, _ghiChuGiayChungNhan,
                        _ngaySapNhap);

                    minMaThuaDat = thuaDatToBanDos[^1].MaThuaDat;
                    _currentThamChieuThuaDat += thuaDatToBanDos.Count;
                    _bufferThamChieuThuaDat = _currentThamChieuThuaDat +
                                              Math.Min(_limit, _totalThamChieuThuaDat - _currentThamChieuThuaDat);
                    StateHasChanged();
                }
            }

            _colorThamChieuThuaDat = Color.Success;
        }
        catch (Exception ex)
        {
            _errorThamChieuThuaDat = ex.Message;
            _colorThamChieuThuaDat = Color.Error;
            const string message = "Không thể tạo dữ liệu thửa đất lịch sử.";
            Logger.Error(ex, message);
            SetMessage(message);
        }
        finally
        {
            // Cập nhật thời gian, trạng thái hoàn thành và trạng thái xử lý
            _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedThamChieuThuaDat = true;
            StateHasChanged();
        }

        // Nếu không có lỗi xảy ra thì cập nhật dữ liệu Tờ bản đồ
        if (string.IsNullOrWhiteSpace(_errorThamChieuThuaDat))
            // Cập nhật dữ liệu Tờ bản đồ
            await UpdatingToBanDo();
    }


    private Color _colorUpdateToBanDo = Color.Default;
    private string _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateToBanDo = false;
    private bool _isProcessingUpdateToBanDo = false;
    private string _messageUpdateToBanDoSuccess = "Đã hoàn thành";
    private string? _errorUpdateToBanDo = string.Empty;

    private async Task UpdatingToBanDo()
    {
        if (_isProcessingUpdateToBanDo || _isCompletedUpdateToBanDo)
            return;
        _colorUpdateToBanDo = Color.Primary;
        _isProcessingUpdateToBanDo = true;
        StateHasChanged();
        try
        {
            // Cập nhật dữ liệu Tờ bản đồ
            await using var dbConnection = new SqlConnection(_connectionString);
            var toBanDoRepository = new ToBanDoRepository(_connectionString!, Logger);
            await toBanDoRepository.UpdateToBanDoAsync(_thamChieuToBanDos, _ghiChuToBanDo, _ngaySapNhap);
            _colorUpdateToBanDo = Color.Success;
        }
        catch (Exception ex)
        {
            _errorUpdateToBanDo = ex.Message;
            _colorUpdateToBanDo = Color.Error;
            const string message = "Không thể cập nhật dữ liệu tờ bản đồ.";
            _errorUpdateDvhc = message;
            Logger.Error(message);
            SetMessage(message);
        }
        finally
        {
            _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedUpdateToBanDo = true;
            StateHasChanged();
        }

        // Nếu không có lỗi xảy ra thì cập nhật dữ liệu Đơn vị hành chính
        if (string.IsNullOrWhiteSpace(_errorUpdateToBanDo))
            // Cập nhật dữ liệu Đơn vị hành chính
            await UpdatingDonViHanhChinh();
    }


    private Color _colorUpdateDvhc = Color.Default;
    private string _timeUpdateDvhc = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateDvhc = false;
    private bool _isProcessingUpdateDvhc = false;
    private string? _errorUpdateDvhc = string.Empty;

    private async Task UpdatingDonViHanhChinh()
    {
        if (_isProcessingUpdateDvhc || _isCompletedUpdateDvhc)
            return;
        Logger.Information("Bắt đầu cập nhật dữ liệu đơn vị hành chính.");
        _colorUpdateDvhc = Color.Primary;
        _isProcessingUpdateDvhc = true;
        StateHasChanged();
        if (_capXaSau == null)
        {
            _colorUpdateDvhc = Color.Error;
            const string message = "Không tìm thấy đơn vị hành chính cấp xã sau.";
            _errorUpdateDvhc = message;
            Logger.Warning(message);
            SetMessage(message);
            return;
        }

        try
        {
            var tenCapXaMoi =
                await HybridCache.GetOrCreateAsync(CacheThamSoDvhc.TenDvhcSau, _ => ValueTask.FromResult(string.Empty));
            if (!string.IsNullOrWhiteSpace(tenCapXaMoi))
            {
                _capXaSau = _capXaSau with { Ten = tenCapXaMoi };
            }

            await using var dbConnection = new SqlConnection(_connectionString);
            var donViHanhChinhRepository = new DonViHanhChinhRepository(_connectionString!, Logger);
            await donViHanhChinhRepository.UpdateDonViHanhChinhAsync(_capXaSau, _maDvhcBiSapNhap);
            _colorUpdateDvhc = Color.Success;
        }
        catch (Exception ex)
        {
            _colorUpdateDvhc = Color.Error;
            _errorUpdateDvhc = ex.Message;
            const string message = "Không thể cập nhật dữ liệu đơn vị hành chính.";
            Logger.Error(ex, message);
            SetMessage(message);
        }
        finally
        {
            _timeUpdateDvhc = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedUpdateDvhc = true;
            StateHasChanged();
        }

        if (string.IsNullOrWhiteSpace(_errorUpdateDvhc))
        {
            await UpdatePrimaryKeyAsync();
        }
    }

    private Color _colorUpdatePrimaryKey = Color.Default;
    private string _timeUpdatePrimaryKey = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdatePrimaryKey = false;
    private bool _isProcessingUpdatePrimaryKey = false;
    private string _errorUpdatePrimaryKey = string.Empty;
    private string _processingMessageUpdatePrimaryKey = "Đang cập nhật...";
    private long _totalUpdatePrimaryKey;
    private long _currentUpdatePrimaryKey;
    private long _bufferUpdatePrimaryKey;

    private async Task UpdatePrimaryKeyAsync()
    {
        _renewPrimaryKey = await HybridCache.GetOrCreateAsync(CacheThamSoDvhc.RenewPrimaryKey,
            _ => ValueTask.FromResult(_renewPrimaryKey));
        StateHasChanged();
        if (!_renewPrimaryKey)
        {
            Logger.Information("Không cần cập nhật mã thửa đất.");
            const string message = "Đã hoàn thành";
            Logger.Information(message);
            SetMessage(message, Severity.Success);
            return;
        }

        if (_isCompletedUpdatePrimaryKey || _isProcessingUpdatePrimaryKey)
            return;
        _colorUpdatePrimaryKey = Color.Primary;
        _isProcessingUpdatePrimaryKey = true;
        StateHasChanged();
        try
        {
            var maToBanDoTemp = await RenewMaToBanDoAsync();
            var maThuaDatTemp = await RenewMaThuaDatAsync(maToBanDoTemp);
            var maDangKyTemp = await UpdateMaDangKyAsync(maThuaDatTemp);
            await UpdateMaGiayChungNhanAsync(maDangKyTemp);

            //Xóa các bản ghi tạm thời:
            await using var dbConnection = new SqlConnection(_connectionString);

            await ChuSuDungRepository.DeleteChuSuDungTempAsync(dbConnection, logger: Logger);

            await GiayChungNhanRepository.DeleteGiayChungNhanByMaDangKyAsync(dbConnection, maDangKyTemp, Logger);
            await GiayChungNhanRepository.DeleteGiayChungNhanTemp(dbConnection, logger: Logger);

            await DangKyThuaDatRepository.DeleteCayLichSuByMaDangKyAsync(dbConnection, maDangKyTemp, Logger);
            await DangKyThuaDatRepository.DeleteOtherByMaDangKyAsync(dbConnection, maDangKyTemp, Logger);
            await DangKyThuaDatRepository.DeleteDangKyByMaThuaDatAsync(dbConnection, maThuaDatTemp, Logger);
            await DangKyThuaDatRepository.DeleteDangKyTempAsync(dbConnection, maDangKyTemp, Logger);

            await ThuaDatRepository.DeleteThuaDatByMaToBanDoAsync(dbConnection, maToBanDoTemp, Logger);
            await ThuaDatRepository.DeleteThuaDatTempAsync(dbConnection, maThuaDatTemp, Logger);

            await ToBanDoRepository.DeleteToBanDoTempAsync(dbConnection, maToBanDoTemp, Logger);

            // Hoàn thành
            _colorUpdatePrimaryKey = Color.Success;
            const string message = "Đã hoàn thành";
            Logger.Information(message);
            SetMessage(message, Severity.Success);
        }
        catch (Exception ex)
        {
            _colorUpdatePrimaryKey = Color.Error;
            _errorUpdatePrimaryKey = ex.Message;
            const string message = "Lỗi khi mới các bản mã.";
            Logger.Error(ex, message);
            SetMessage(message);
        }
        finally
        {
            _timeUpdatePrimaryKey = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedUpdatePrimaryKey = true;
            StateHasChanged();
        }
    }

    private const int TotalStepUpdatePrimaryKey = 4;

    private async Task<long> RenewMaToBanDoAsync()
    {
        _processingMessageUpdatePrimaryKey = $"[1/{TotalStepUpdatePrimaryKey}]: Làm mới mã tờ bản đồ...";
        StateHasChanged();
        var toBanDoRepository = new ToBanDoRepository(_connectionString!, Logger);
        var tempMaToBanDo = await toBanDoRepository.RenewMaToBanDoAsync(_capXaSau!, limit: _limit);
        Logger.Information("Hoàn thành làm mới mã tờ bản đồ.");
        return tempMaToBanDo;
    }

    private async Task<long> RenewMaThuaDatAsync(long maToBanDoTemp)
    {
        _processingMessageUpdatePrimaryKey = $"[2/{TotalStepUpdatePrimaryKey}]: Làm mới mã thửa đất...";
        StateHasChanged();
        var thuaDatRepository = new ThuaDatRepository(_connectionString!, Logger);
        var tempMaThuaDat =
            await thuaDatRepository.RenewMaThuaDatAsync(_capXaSau!, maToBanDoTemp: maToBanDoTemp, limit: _limit);
        Logger.Information("Hoàn thành làm mới mã thửa đất.");
        return tempMaThuaDat;
    }

    private async Task<long> UpdateMaDangKyAsync(long maThuaDatTemp)
    {
        _processingMessageUpdatePrimaryKey =
            $"[3/{TotalStepUpdatePrimaryKey}]: Làm mới mã đăng ký...";
        StateHasChanged();
        var dangKyThuaDatRepository = new DangKyThuaDatRepository(_connectionString!, Logger);
        var tempMaDangKy =
            await dangKyThuaDatRepository.RenewMaDangKyAsync(_capXaSau!, maThuaDatTemp: maThuaDatTemp, limit: _limit);
        Logger.Information("Hoàn thành làm mới mã đăng ký.");
        return tempMaDangKy;
    }

    private async Task UpdateMaGiayChungNhanAsync(long maDangKyTemp)
    {
        _processingMessageUpdatePrimaryKey = $"[4/{TotalStepUpdatePrimaryKey}]: Làm mới mã Giấy chứng nhận...";
        StateHasChanged();
        var giayChungNhanRepository = new GiayChungNhanRepository(_connectionString!, Logger);
        await giayChungNhanRepository.RenewMaGiayChungNhanAsync(_capXaSau!, maDangKyTemp: maDangKyTemp, limit: _limit);
        Logger.Information("Hoàn thành làm mới mã Giấy chứng nhận.");
    }

    private async Task DeleteTempAsync(SqlConnection dbConnection)
    {
        await ToBanDoRepository.DeleteToBanDoTempAsync(dbConnection, logger: Logger);
        await ThuaDatRepository.DeleteThuaDatTempAsync(dbConnection, logger: Logger);
        await DangKyThuaDatRepository.DeleteDangKyTempAsync(dbConnection, logger: Logger);
        await GiayChungNhanRepository.DeleteGiayChungNhanTemp(dbConnection, logger: Logger);
    }
}