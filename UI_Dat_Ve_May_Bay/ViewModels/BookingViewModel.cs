using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Services;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class BookingViewModel : ObservableObject
    {
        private readonly ApiClient _apiClient;
        private readonly SemaphoreSlim _loadSeatsGate = new(1, 1);
        private readonly SemaphoreSlim _loadVouchersGate = new(1, 1);
        private DateTime _lastSeatInteractionUtc = DateTime.MinValue;
        private static readonly TimeSpan AutoRefreshSeatInteractionCooldown = TimeSpan.FromSeconds(3);

        // ===== BACKEND ENDPOINTS (KHÔNG SỬA BE) =====
        private const string EP_GET_SEATS = "/api/ChuyenBay/DanhSachGheTheoChuyenBay"; // POST
        private const string EP_HOLD_SEATS = "/api/ChuyenBay/SetTrangThaiGheNgoi";     // POST
        private const string EP_RELEASE_SEATS = "/api/ChuyenBay/ReleaseSeat";          // POST
        private const string EP_DAT_VE = "/api/ChuyenBay/DatVe";                       // POST

        private const string EP_VOUCHER_LIST = "/api/PhieuGiamGia/LayDanhSachChiTietPhieuGiamGia"; // POST
        private const string EP_VOUCHER_FIND = "/api/PhieuGiamGia/TimKiemMaGiamGia";               // POST?maGiamGia=
        private const string EP_VOUCHER_APPLY = "/api/PhieuGiamGia/ApplyVoucher";                  // POST?idMaGiamGia=
        private const string EP_GET_THANH_TOAN = "/api/ThanhToan/GetThanhToan";                        // GET

        // ===== Input =====
        public FlightViewModel.LichBayItemVm SelectedSchedule { get; }

        // ===== Seats =====
        public ObservableCollection<SeatVm> LoaiVe1Seats { get; } = new();
        public ObservableCollection<SeatVm> LoaiVe3Seats { get; } = new();
        public ObservableCollection<SeatVm> LoaiVe4Seats { get; } = new();

        // alias (nếu code cũ còn dùng tên này)
        public ObservableCollection<SeatVm> SeatsLoaiVe1 => LoaiVe1Seats;
        public ObservableCollection<SeatVm> SeatsLoaiVe3 => LoaiVe3Seats;
        public ObservableCollection<SeatVm> SeatsLoaiVe4 => LoaiVe4Seats;

        // ===== Seat row maps (UI cabin 2 bên + lối đi) =====
        public ObservableCollection<SeatRowVm> SeatRowsLoai1 { get; } = new();
        public ObservableCollection<SeatRowVm> SeatRowsLoai3 { get; } = new();
        public ObservableCollection<SeatRowVm> SeatRowsLoai4 { get; } = new();

        private readonly HashSet<long> _heldSeatIds = new();
        private readonly HashSet<long> _myBookedSeatIds = new();

        public int SelectedSeatCount => _heldSeatIds.Count;

        private string GetBookedSeatsFilePath()
        {
            var userId = _apiClient.GetUserIdFromToken() ?? "anonymous";
            // Clean userId for filename
            userId = string.Join("_", userId.Split(Path.GetInvalidFileNameChars()));
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UI_Dat_Ve_May_Bay");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"booked_seats_{userId}.json");
        }

        private void LoadMyBookedSeats()
        {
            _myBookedSeatIds.Clear();
            try
            {
                var userId = _apiClient.GetUserIdFromToken();
                if (string.IsNullOrWhiteSpace(userId) || userId == "anonymous") return;

                var path = GetBookedSeatsFilePath();
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var ids = JsonSerializer.Deserialize<List<long>>(json);
                if (ids != null)
                {
                    foreach (var id in ids) _myBookedSeatIds.Add(id);
                }
            }
            catch { }
        }

        private void SaveMyBookedSeats()
        {
            try
            {
                var path = GetBookedSeatsFilePath();
                var json = JsonSerializer.Serialize(_myBookedSeatIds.ToList());
                File.WriteAllText(path, json);
            }
            catch { }
        }

        // ===== Voucher =====
        public ObservableCollection<VoucherItemVm> Vouchers { get; } = new();
        public ObservableCollection<VoucherItemVm> MyVouchers => Vouchers;

        private VoucherItemVm? _selectedVoucher;
        public VoucherItemVm? SelectedVoucher
        {
            get => _selectedVoucher;
            set
            {
                if (SetProperty(ref _selectedVoucher, value))
                {
                    if (value != null && !string.IsNullOrWhiteSpace(value.MaGiamGia))
                        VoucherCode = value.MaGiamGia;

                    RecalcTotals();
                }
            }
        }

        private string _voucherCode = "";
        public string VoucherCode
        {
            get => _voucherCode;
            set => SetProperty(ref _voucherCode, value);
        }

        // ===== Payment =====
        public ObservableCollection<PaymentOption> PaymentOptions { get; } = new()
        {
            new PaymentOption(1, "Thanh toán trực tiếp"),
            new PaymentOption(2, "Thanh toán VNPAY"),
            new PaymentOption(3, "Thanh toán PayPal"),
        };

        private PaymentOption _selectedPayment;
        public PaymentOption SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        // ===== Summary =====
        private decimal _subtotal;
        public decimal Subtotal { get => _subtotal; set => SetProperty(ref _subtotal, value); }

        private decimal _discount;
        public decimal Discount { get => _discount; set => SetProperty(ref _discount, value); }

        private decimal _total;
        public decimal Total { get => _total; set => SetProperty(ref _total, value); }

        // ===== Payment state =====
        private bool _isWaitingPayment;
        public bool IsWaitingPayment { get => _isWaitingPayment; set => SetProperty(ref _isWaitingPayment, value); }

        private string _paymentUrl = "";
        public string PaymentUrl { get => _paymentUrl; set => SetProperty(ref _paymentUrl, value); }

        private string _latestPaymentStatus = "";
        public string LatestPaymentStatus { get => _latestPaymentStatus; set => SetProperty(ref _latestPaymentStatus, value); }

        public ObservableCollection<PaymentHistoryItemVm> PaymentHistory { get; } = new();

        // ===== UI state =====
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private string _error = "";
        public string Error { get => _error; set => SetProperty(ref _error, value); }

        private string _info = "";
        public string Info { get => _info; set => SetProperty(ref _info, value); }

        // ===== Commands =====
        public AsyncRelayCommand RefreshSeatsCommand { get; }
        public AsyncRelayCommand AutoRefreshSeatsCommand { get; }
        public AsyncRelayCommand LoadVouchersCommand { get; }
        public AsyncRelayCommand ApplyVoucherCommand { get; }
        public RelayCommand OpenPaymentLinkCommand { get; }
        public AsyncRelayCommand ConfirmPaymentCommand { get; }
        public AsyncRelayCommand CheckPaymentStatusCommand { get; }
        public RelayCommand ClearVoucherCommand { get; }
        public AsyncRelayCommand BookCommand { get; }
        public RelayCommand BackCommand { get; }

        public BookingViewModel(ApiClient apiClient, FlightViewModel.LichBayItemVm selectedSchedule, Action? goBack = null)
        {
            _apiClient = apiClient;
            SelectedSchedule = selectedSchedule;

            if (string.IsNullOrWhiteSpace(_apiClient.Token))
            {
                try { _apiClient.Token = new TokenStore().Load(); } catch { }
            }

            _selectedPayment = PaymentOptions.First();

            RefreshSeatsCommand = new AsyncRelayCommand(ManualRefreshSeatsAsync);
            AutoRefreshSeatsCommand = new AsyncRelayCommand(AutoRefreshSeatsAsync);
            LoadVouchersCommand = new AsyncRelayCommand(LoadVouchersAsync);
            ApplyVoucherCommand = new AsyncRelayCommand(FindAndApplyVoucherAsync);

            OpenPaymentLinkCommand = new RelayCommand(_ =>
            {
                if (!string.IsNullOrWhiteSpace(PaymentUrl))
                    OpenUrlInBrowser(PaymentUrl);
            });

            ConfirmPaymentCommand = new AsyncRelayCommand(ConfirmPaymentAsync);
            CheckPaymentStatusCommand = new AsyncRelayCommand(CheckPaymentStatusAsync);

            ClearVoucherCommand = new RelayCommand(_ =>
            {
                SelectedVoucher = null;
                VoucherCode = "";
                RecalcTotals();
            });

            BookCommand = new AsyncRelayCommand(BookAsync);
            BackCommand = new RelayCommand(async _ =>
            {
                try
                {
                    await ReleaseAllHeldSeatsAsync();
                }
                catch { }
                goBack?.Invoke();
            });

            // auto-load (best-effort)
            LoadMyBookedSeats();
            _ = ManualRefreshSeatsAsync();
            _ = LoadVouchersAsync();
        }

        private Task ManualRefreshSeatsAsync() => LoadSeatsAsync();

        private async Task AutoRefreshSeatsAsync()
        {
            if (DateTime.UtcNow - _lastSeatInteractionUtc < AutoRefreshSeatInteractionCooldown)
                return;

            await LoadSeatsAsync();
        }

        // =========================================================
        // Seat events (BookingView.xaml.cs sẽ gọi)
        // =========================================================
        public async Task OnSeatCheckedAsync(SeatVm seat)
        {
            if (seat == null) return;

            if (!seat.IsAvailable)
            {
                seat.IsSelected = false;
                return;
            }

            try
            {
                Error = "";
                Info = "";
                IsBusy = true;
                _lastSeatInteractionUtc = DateTime.UtcNow;

                var payload = new
                {
                    idLichBay = SelectedSchedule.Id,
                    idGheNgoi = new[] { seat.IdGheNgoi }
                };

                var json = await SendAndReadAsync(HttpMethod.Post, EP_HOLD_SEATS, payload, throwOnBusinessError: false);

                // nếu BE trả list ghế fail (vừa bị người khác giữ)
                if (TryParseFailedSeatIds(json, out var failed) && failed.Contains(seat.IdGheNgoi))
                {
                    seat.IsSelected = false;
                    await LoadSeatsAsync();
                    Info = "Ghế vừa được người khác giữ. Vui lòng chọn ghế khác.";
                    return;
                }

                var holdBusinessError = TryExtractBusinessError(json);
                if (!string.IsNullOrWhiteSpace(holdBusinessError))
                {
                    seat.IsSelected = false;
                    await LoadSeatsAsync();
                    Info = holdBusinessError;
                    return;
                }

                _heldSeatIds.Add(seat.IdGheNgoi);
                seat.IsSelected = true;
                seat.IdTrangThai = 2; // Tạm thời đánh dấu là đang giữ để tránh flicker

                RecalcTotals();
            }
            catch (Exception ex)
            {
                seat.IsSelected = false;
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task OnSeatUncheckedAsync(SeatVm seat)
        {
            if (seat == null) return;

            try
            {
                Error = "";
                Info = "";
                IsBusy = true;
                _lastSeatInteractionUtc = DateTime.UtcNow;

                if (_heldSeatIds.Contains(seat.IdGheNgoi))
                {
                    var payload = new
                    {
                        idLichBay = SelectedSchedule.Id,
                        idGheNgoi = new[] { seat.IdGheNgoi }
                    };

                    await SendAndReadAsync(HttpMethod.Post, EP_RELEASE_SEATS, payload);
                    _heldSeatIds.Remove(seat.IdGheNgoi);
                }

                seat.IsSelected = false;
                seat.IdTrangThai = 0; // Optimistic logic: mark as free locally

                RecalcTotals();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                // ✅ FIX: Restore seat state khi lỗi
                seat.IsSelected = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ReleaseAllHeldSeatsAsync()
        {
            try
            {
                if (_heldSeatIds.Count == 0) return;

                var payload = new
                {
                    idLichBay = SelectedSchedule.Id,
                    idGheNgoi = _heldSeatIds.ToArray()
                };

                await SendAndReadAsync(HttpMethod.Post, EP_RELEASE_SEATS, payload);
                _heldSeatIds.Clear();
                RecalcTotals();
            }
            catch
            {
                // best-effort
            }
        }

        // =========================================================
        // Load Seats
        // =========================================================
        private async Task LoadSeatsAsync()
        {
            if (!await _loadSeatsGate.WaitAsync(0))
                return;

            try
            {
                Error = "";
                Info = "";
                IsBusy = true;

                LoadMyBookedSeats();
                LoaiVe1Seats.Clear();
                LoaiVe3Seats.Clear();
                LoaiVe4Seats.Clear();

                var groups = await TryLoadSeatGroupsWithFallbackAsync();

                foreach (var s in groups.LoaiVe1) LoaiVe1Seats.Add(ToSeatVm(s));
                foreach (var s in groups.LoaiVe3) LoaiVe3Seats.Add(ToSeatVm(s));
                foreach (var s in groups.LoaiVe4) LoaiVe4Seats.Add(ToSeatVm(s));

                MarkSelectedFromHeldIds();
                RebuildAllSeatRows();
                RecalcTotals();

                OnPropertyChanged(nameof(SelectedSeatCount));
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
                _loadSeatsGate.Release();
            }
        }

        private async Task<SeatGroups> TryLoadSeatGroupsWithFallbackAsync()
        {
            var urlQuery = $"{EP_GET_SEATS}?idLichBay={SelectedSchedule.Id}&idTuyenBay={SelectedSchedule.IdTuyenBay}";
            string? lastErr = null;

            async Task<SeatGroups?> TryCallAsync(string url, object? body)
            {
                try
                {
                    var json = await SendAndReadAsync(HttpMethod.Post, url, body);
                    var g = ParseSeatGroupsTolerant(json);

                    if (g.LoaiVe1.Count == 0 && g.LoaiVe3.Count == 0 && g.LoaiVe4.Count == 0)
                        return null;

                    return g;
                }
                catch (Exception ex)
                {
                    lastErr = ex.Message;
                    return null;
                }
            }

            // 1) query + null
            var g1 = await TryCallAsync(urlQuery, null);
            if (g1 != null) return g1;

            // 2) query + body {}
            var g2 = await TryCallAsync(urlQuery, new { });
            if (g2 != null) return g2;

            // 3) body contains ids (no query)
            var g3 = await TryCallAsync(EP_GET_SEATS, new
            {
                idLichBay = SelectedSchedule.Id,
                idTuyenBay = SelectedSchedule.IdTuyenBay
            });
            if (g3 != null) return g3;

            throw new Exception(
                "Không tải được danh sách ghế (BE trả lỗi). " +
                $"idLichBay={SelectedSchedule.Id}, idTuyenBay={SelectedSchedule.IdTuyenBay}. " +
                (string.IsNullOrWhiteSpace(lastErr) ? "" : $"Chi tiết: {lastErr}")
            );
        }

        private SeatVm ToSeatVm(SeatDtoLite dto)
        {
            var isHeld = _heldSeatIds.Contains(dto.IdGheNgoi);
            var isBooked = _myBookedSeatIds.Contains(dto.IdGheNgoi);

            return new SeatVm
            {
                IdGheNgoi = dto.IdGheNgoi,
                SoGhe = dto.SoGhe ?? "",
                IdTrangThai = dto.IdTrangThai,
                IsBookedByMe = isBooked,
                IsSelected = isHeld || isBooked
            };
        }

        private void MarkSelectedFromHeldIds()
        {
            foreach (var s in LoaiVe1Seats.Concat(LoaiVe3Seats).Concat(LoaiVe4Seats))
            {
                var isHeld = _heldSeatIds.Contains(s.IdGheNgoi);
                var isBooked = _myBookedSeatIds.Contains(s.IdGheNgoi);
                s.IsBookedByMe = isBooked;
                s.IsSelected = isHeld || isBooked;
            }
        }

        // =========================================================
        // Vouchers
        // =========================================================
        private async Task LoadVouchersAsync()
        {
            if (!await _loadVouchersGate.WaitAsync(0))
                return;

            try
            {
                Error = "";
                IsBusy = true;

                Vouchers.Clear();

                var json = await SendAndReadAsync(HttpMethod.Post, EP_VOUCHER_LIST, new { });

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement arr = default;
                var hasArray = false;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    arr = root;
                    hasArray = true;
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("data2", out var d2) && d2.ValueKind == JsonValueKind.Array)
                    {
                        arr = d2; hasArray = true;
                    }
                    else if (root.TryGetProperty("Data2", out var d2c) && d2c.ValueKind == JsonValueKind.Array)
                    {
                        arr = d2c; hasArray = true;
                    }
                    else if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                    {
                        arr = d; hasArray = true;
                    }
                    else if (root.TryGetProperty("Data", out var dc) && dc.ValueKind == JsonValueKind.Array)
                    {
                        arr = dc; hasArray = true;
                    }
                }

                if (hasArray)
                {
                    foreach (var it in arr.EnumerateArray())
                    {
                        Vouchers.Add(new VoucherItemVm
                        {
                            IdChiTiet = GetLong(it, "id", "Id", "idChiTiet", "IdChiTiet"),
                            IdMaGiamGia = GetLong(it, "idMaGiamGia", "IdMaGiamGia"),
                            MaGiamGia = GetString(it, "maGiamGia", "MaGiamGia") ?? "",
                            GiaTriGiam = GetDecimal(it, "giaTriGiam", "GiaTriGiam"),
                            IdLoaiGiamGia = GetInt(it, "idLoaiGiamGia", "IdLoaiGiamGia"),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
                _loadVouchersGate.Release();
            }
        }

        private async Task FindAndApplyVoucherAsync()
        {
            try
            {
                Error = "";
                Info = "";
                IsBusy = true;

                var code = (VoucherCode ?? string.Empty).Trim();

                // ưu tiên voucher đang chọn
                if (SelectedVoucher != null)
                {
                    var selectedCode = (SelectedVoucher.MaGiamGia ?? string.Empty).Trim();
                    var textboxMatches = string.IsNullOrWhiteSpace(code) ||
                                         string.Equals(code, selectedCode, StringComparison.OrdinalIgnoreCase);

                    if (textboxMatches)
                    {
                        if ((SelectedVoucher.IdMaGiamGia ?? 0) <= 0)
                        {
                            Info = "Voucher đang chọn không hợp lệ.";
                            return;
                        }

                        await SendAndReadAsync(HttpMethod.Post,
                            $"{EP_VOUCHER_APPLY}?idMaGiamGia={SelectedVoucher.IdMaGiamGia}",
                            new { });

                        var appliedId = SelectedVoucher.IdMaGiamGia ?? 0;
                        var appliedCode = (SelectedVoucher.MaGiamGia ?? "").Trim();

                        await LoadVouchersAsync();

                        SelectedVoucher =
                            (appliedId > 0 ? MyVouchers.FirstOrDefault(v => (v.IdMaGiamGia ?? 0) == appliedId) : null)
                            ?? (!string.IsNullOrWhiteSpace(appliedCode)
                                ? MyVouchers.FirstOrDefault(v => string.Equals(v.MaGiamGia, appliedCode, StringComparison.OrdinalIgnoreCase))
                                : null)
                            ?? SelectedVoucher;

                        RecalcTotals();
                        Info = "Đã áp dụng voucher.";
                        return;
                    }
                }

                // apply theo mã nhập tay
                if (string.IsNullOrWhiteSpace(code))
                {
                    Info = "Chọn voucher hoặc nhập mã giảm giá trước.";
                    return;
                }

                var json = await SendAndReadAsync(
                    HttpMethod.Post,
                    $"{EP_VOUCHER_FIND}?maGiamGia={Uri.EscapeDataString(code)}",
                    new { }
                );

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement dataEl = default;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d))
                    dataEl = d;
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("Data", out var dc))
                    dataEl = dc;
                else
                    dataEl = root;

                JsonElement? picked = null;
                if (dataEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in dataEl.EnumerateArray())
                    {
                        var c = (GetString(it, "maGiamGia", "MaGiamGia") ?? string.Empty).Trim();
                        if (string.Equals(c, code, StringComparison.OrdinalIgnoreCase))
                        {
                            picked = it;
                            break;
                        }
                    }
                    picked ??= dataEl.EnumerateArray().FirstOrDefault();
                }
                else if (dataEl.ValueKind == JsonValueKind.Object)
                {
                    picked = dataEl;
                }

                if (picked == null)
                {
                    Info = "Không tìm thấy voucher.";
                    return;
                }

                var idMa = GetLong(picked.Value, "id", "Id", "idMaGiamGia", "IdMaGiamGia");
                if (idMa <= 0)
                {
                    Info = "Không tìm thấy voucher.";
                    return;
                }

                await SendAndReadAsync(HttpMethod.Post, $"{EP_VOUCHER_APPLY}?idMaGiamGia={idMa}", new { });

                await LoadVouchersAsync();
                SelectedVoucher = MyVouchers.FirstOrDefault(v => (v.IdMaGiamGia ?? 0) == idMa)
                                 ?? MyVouchers.FirstOrDefault(v => string.Equals(v.MaGiamGia, code, StringComparison.OrdinalIgnoreCase));

                if (SelectedVoucher == null)
                {
                    SelectedVoucher = new VoucherItemVm
                    {
                        IdChiTiet = 0,
                        IdMaGiamGia = idMa,
                        MaGiamGia = GetString(picked.Value, "maGiamGia", "MaGiamGia") ?? code,
                        GiaTriGiam = GetDecimal(picked.Value, "giaTriGiam", "GiaTriGiam"),
                        IdLoaiGiamGia = 0
                    };
                }

                RecalcTotals();
                Info = "Đã áp dụng voucher.";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =========================================================
        // Totals
        // =========================================================
        private void RecalcTotals()
        {
            var count = _heldSeatIds.Count;
            Subtotal = (decimal)count * SelectedSchedule.Gia;

            if (SelectedVoucher == null || count == 0)
            {
                Discount = 0;
                Total = Subtotal;
                OnPropertyChanged(nameof(SelectedSeatCount));
                return;
            }

            var loai = SelectedVoucher.IdLoaiGiamGia;
            var giaTri = SelectedVoucher.GiaTriGiam;

            decimal discount = 0;
            if (loai == 1) // %
            {
                discount = Subtotal * (giaTri / 100m);
            }
            else
            {
                discount = giaTri;
            }

            if (discount < 0) discount = 0;
            if (discount > Subtotal) discount = Subtotal;

            Discount = discount;
            Total = Subtotal - Discount;

            OnPropertyChanged(nameof(SelectedSeatCount));
        }

        // =========================================================
        // Payment confirm (best-effort)
        // =========================================================
        private async Task ConfirmPaymentAsync()
        {
            try
            {
                Error = "";
                Info = "";
                IsBusy = true;

                await CheckPaymentStatusAsync();
                await LoadSeatsAsync();
                await LoadVouchersAsync();

                if (IsPaymentSuccessText(LatestPaymentStatus))
                {
                    IsWaitingPayment = false;
                    PaymentUrl = "";
                    Info = "Thanh toán đã được xác nhận.";
                }
                else
                {
                    Info = string.IsNullOrWhiteSpace(LatestPaymentStatus)
                        ? "Đã làm mới trạng thái sau thanh toán."
                        : $"Đã làm mới. {LatestPaymentStatus}";
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CheckPaymentStatusAsync()
        {
            try
            {
                Error = "";
                if (!IsBusy) IsBusy = true;

                PaymentHistory.Clear();
                LatestPaymentStatus = "";

                var raw = await SendAndReadAsync(HttpMethod.Get, EP_GET_THANH_TOAN, null);
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                JsonElement arr = default;
                bool ok = false;

                if (root.ValueKind == JsonValueKind.Array) { arr = root; ok = true; }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array) { arr = d; ok = true; }
                    else if (root.TryGetProperty("Data", out var D) && D.ValueKind == JsonValueKind.Array) { arr = D; ok = true; }
                    else if (root.TryGetProperty("data2", out var d2) && d2.ValueKind == JsonValueKind.Array) { arr = d2; ok = true; }
                }

                if (!ok)
                {
                    LatestPaymentStatus = "Không đọc được lịch sử thanh toán từ BE.";
                    return;
                }

                var list = new List<PaymentHistoryItemVm>();
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    string loai = GetString(item, "LoaiDichVu") ?? GetString(item, "loaiDichVu") ?? "";
                    string ma = GetString(item, "MaThanhToan") ?? GetString(item, "maThanhToan") ?? "";
                    string trangThai = GetString(item, "TrangThai") ?? GetString(item, "trangThai") ?? GetString(item, "TenTrangThai") ?? "";
                    string soTienRaw = GetNumberText(item, "SoTien") ?? GetNumberText(item, "soTien") ?? "";
                    string ngay = GetString(item, "NgayThanhToan") ?? GetString(item, "ngayThanhToan") ?? GetString(item, "CreatedAt") ?? "";
                    string provider = GetString(item, "CongThanhToan") ?? GetString(item, "congThanhToan") ?? "";
                    int? stCode = GetInt(item, "StatusCode") ?? GetInt(item, "statusCode") ?? GetInt(item, "Vnp_ResponseCode");

                    var line = new PaymentHistoryItemVm
                    {
                        LoaiDichVu = loai,
                        TrangThai = trangThai,
                        MaThanhToan = ma,
                        Provider = provider,
                        SoTien = soTienRaw,
                        Ngay = ngay,
                        StatusCode = stCode,
                    };
                    line.DisplayLine = BuildPaymentDisplay(line);
                    list.Add(line);
                }

                // Ưu tiên giao dịch flight + gần tổng tiền hiện tại
                var ordered = list
                    .OrderByDescending(x => ScorePaymentRecord(x))
                    .ThenByDescending(x => ParseDateSafe(x.Ngay))
                    .ToList();

                foreach (var r in ordered.Take(8)) PaymentHistory.Add(r);

                var latest = ordered.FirstOrDefault();
                LatestPaymentStatus = latest == null ? "Chưa có lịch sử thanh toán." : $"Thanh toán gần nhất: {latest.DisplayLine}";

                if (latest != null && IsPaymentSuccessText(latest.TrangThai + " " + latest.StatusCode))
                {
                    IsWaitingPayment = false;
                    PaymentUrl = "";
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =========================================================
        // Book
        // =========================================================
        private async Task BookAsync()
        {
            try
            {
                Error = "";
                Info = "";
                IsBusy = true;

                if (_heldSeatIds.Count == 0)
                {
                    Info = "Bạn chưa chọn ghế.";
                    return;
                }

                // Nếu có voucher nhưng chưa có IdChiTiet -> ensure Apply để BE tạo chi tiết theo user
                if (SelectedVoucher != null && SelectedVoucher.IdChiTiet <= 0 && (SelectedVoucher.IdMaGiamGia ?? 0) > 0)
                {
                    await SendAndReadAsync(HttpMethod.Post, $"{EP_VOUCHER_APPLY}?idMaGiamGia={SelectedVoucher.IdMaGiamGia}", new { });
                    await LoadVouchersAsync();
                    var idMa = SelectedVoucher.IdMaGiamGia ?? 0;
                    SelectedVoucher = MyVouchers.FirstOrDefault(v => (v.IdMaGiamGia ?? 0) == idMa) ?? SelectedVoucher;
                    RecalcTotals();
                }

                var payload = new
                {
                    IdChuyenBay = SelectedSchedule.IdTuyenBay,
                    IdTuyenBay = SelectedSchedule.IdTuyenBay,
                    IdLichBay = SelectedSchedule.Id,
                    IdGheNgois = _heldSeatIds.ToArray(),
                    SelectedPayment = SelectedPayment?.Id ?? 0,
                    Gia = Total,
                    IdChiTietPhieuGiamGia = (SelectedVoucher != null && SelectedVoucher.IdChiTiet > 0) ? SelectedVoucher.IdChiTiet : (long?)null
                };

                var raw = await SendAndReadAsync(HttpMethod.Post, EP_DAT_VE, payload);

                int bizCode = 0;
                string? bizMsg = null;
                string? payUrl = null;

                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("statusCode", out var sc) && sc.ValueKind == JsonValueKind.Number)
                            bizCode = sc.GetInt32();
                        if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                            bizMsg = msg.GetString();
                        if (root.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
                            payUrl = url.GetString();
                    }
                }
                catch
                {
                    // ignore
                }

                if (bizCode >= 400)
                    throw new Exception(string.IsNullOrWhiteSpace(bizMsg) ? "Đặt vé thất bại." : bizMsg);

                // VNPAY
                if (!string.IsNullOrWhiteSpace(payUrl))
                {
                    PaymentUrl = payUrl;
                    IsWaitingPayment = true;
                    // Mở WebView2 bên trong app (BookingView.xaml.cs sẽ navigate theo PaymentUrl)

                    // clear local selection to avoid stale UI
                    _heldSeatIds.Clear();
                    RecalcTotals();

                    Info = "Đã sẵn sàng thanh toán trong cửa sổ web ngay trong app. Sau khi thanh toán xong, bấm Kiểm tra/Tôi đã thanh toán.";
                    return;
                }

                var finalMsg = string.IsNullOrWhiteSpace(bizMsg) ? "Đặt vé thành công." : bizMsg;
                Info = finalMsg;

                // Save booked seats locally
                foreach(var id in _heldSeatIds) _myBookedSeatIds.Add(id);
                SaveMyBookedSeats();

                _heldSeatIds.Clear();
                await LoadSeatsAsync();
                await LoadVouchersAsync();
                
                // ✅ FIX: Hiển thị dialog ở cuối sau khi tất cả xử lý thành công
                DialogService.ShowSuccess(finalMsg, "Thông báo");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                // ✅ FIX: Clear held seats khi lỗi để reset state
                _heldSeatIds.Clear();
                RecalcTotals();
                DialogService.ShowError($"Đặt vé thất bại: {ex.Message}", "Lỗi");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =========================================================
        // Seat row map (cabin 2 bên)
        // =========================================================
        private void RebuildAllSeatRows()
        {
            RebuildSeatRows(LoaiVe1Seats, SeatRowsLoai1);
            RebuildSeatRows(LoaiVe3Seats, SeatRowsLoai3);
            RebuildSeatRows(LoaiVe4Seats, SeatRowsLoai4);
        }

        private void RebuildSeatRows(IEnumerable<SeatVm> source, ObservableCollection<SeatRowVm> target)
        {
            target.Clear();
            var seats = source?.ToList() ?? new List<SeatVm>();
            if (seats.Count == 0) return;

            var parsed = seats.Select((seat, idx) => new ParsedSeat
            {
                Seat = seat,
                Index = idx,
                Row = ParseSeatRowNumber(seat.SoGhe),
                Col = ParseSeatColumnLetter(seat.SoGhe)
            }).ToList();

            var grouped = parsed.Any(x => x.Row.HasValue)
                ? parsed.GroupBy(x => x.Row ?? 999999).OrderBy(g => g.Key)
                : parsed.GroupBy(x => x.Index / 6 + 1).OrderBy(g => g.Key);

            foreach (var g in grouped)
            {
                var rowVm = new SeatRowVm { RowLabel = (g.Key == 999999 ? "?" : g.Key.ToString()) };
                var items = g.OrderBy(x => x.Index).ToList();
                bool hasLetters = items.Any(x => x.Col.HasValue);

                if (hasLetters)
                {
                    foreach (var x in items.Where(x => x.Col is 'A' or 'B' or 'C').OrderBy(x => x.Col)) rowVm.LeftSeats.Add(x.Seat);
                    foreach (var x in items.Where(x => x.Col is 'D' or 'E' or 'F').OrderBy(x => x.Col)) rowVm.RightSeats.Add(x.Seat);

                    var leftovers = items.Where(x => !rowVm.LeftSeats.Contains(x.Seat) && !rowVm.RightSeats.Contains(x.Seat))
                                         .OrderBy(x => x.Index)
                                         .Select(x => x.Seat)
                                         .ToList();
                    if (leftovers.Count > 0)
                    {
                        int half = (leftovers.Count + 1) / 2;
                        foreach (var seat in leftovers.Take(half)) rowVm.LeftSeats.Add(seat);
                        foreach (var seat in leftovers.Skip(half)) rowVm.RightSeats.Add(seat);
                    }
                }
                else
                {
                    int half = (items.Count + 1) / 2;
                    foreach (var x in items.Take(half)) rowVm.LeftSeats.Add(x.Seat);
                    foreach (var x in items.Skip(half)) rowVm.RightSeats.Add(x.Seat);
                }

                target.Add(rowVm);
            }

            OnPropertyChanged(nameof(SeatRowsLoai1));
            OnPropertyChanged(nameof(SeatRowsLoai3));
            OnPropertyChanged(nameof(SeatRowsLoai4));
        }

        private static int? ParseSeatRowNumber(string? soGhe)
        {
            if (string.IsNullOrWhiteSpace(soGhe)) return null;
            var m = Regex.Match(soGhe.Trim(), @"(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : null;
        }

        private static char? ParseSeatColumnLetter(string? soGhe)
        {
            if (string.IsNullOrWhiteSpace(soGhe)) return null;
            var m = Regex.Match(soGhe.Trim(), @"([A-Za-z])$");
            return m.Success ? char.ToUpperInvariant(m.Groups[1].Value[0]) : null;
        }

        // =========================================================
        // Payment parsing helpers
        // =========================================================
        private static string? GetString(JsonElement obj, string name)
            => obj.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

        private static int? GetInt(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var p)) return null;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n)) return n;
            if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out n)) return n;
            return null;
        }

        private static string? GetNumberText(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var p)) return null;
            if (p.ValueKind == JsonValueKind.Number) return p.GetRawText();
            if (p.ValueKind == JsonValueKind.String) return p.GetString();
            return null;
        }

        private string BuildPaymentDisplay(PaymentHistoryItemVm x)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(x.Provider)) parts.Add(x.Provider);
            if (!string.IsNullOrWhiteSpace(x.TrangThai)) parts.Add(x.TrangThai);
            if (x.StatusCode.HasValue) parts.Add($"code:{x.StatusCode}");
            if (!string.IsNullOrWhiteSpace(x.SoTien)) parts.Add($"{x.SoTien} đ");
            if (!string.IsNullOrWhiteSpace(x.MaThanhToan)) parts.Add($"#{x.MaThanhToan}");
            if (!string.IsNullOrWhiteSpace(x.LoaiDichVu)) parts.Add($"[{x.LoaiDichVu}]");
            return string.Join(" • ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private int ScorePaymentRecord(PaymentHistoryItemVm x)
        {
            int score = 0;
            if (!string.IsNullOrWhiteSpace(x.LoaiDichVu) && x.LoaiDichVu.Contains("flight", StringComparison.OrdinalIgnoreCase)) score += 100;
            if (!string.IsNullOrWhiteSpace(x.LoaiDichVu) && x.LoaiDichVu.Contains("vé", StringComparison.OrdinalIgnoreCase)) score += 80;
            if (IsPaymentSuccessText(x.TrangThai + " " + x.StatusCode)) score += 30;
            if (decimal.TryParse(x.SoTien, out var money))
            {
                var diff = Math.Abs(money - Total);
                if (diff < 1) score += 40;
                else if (diff < 1000) score += 25;
                else if (diff < 100000) score += 10;
            }
            return score;
        }

        private static DateTime ParseDateSafe(string? s)
        {
            if (DateTime.TryParse(s, out var d)) return d;
            return DateTime.MinValue;
        }

        private static bool IsPaymentSuccessText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.ToLowerInvariant();
            return t.Contains("thành công") || t.Contains("success") || t.Contains("paid") || t.Contains("00");
        }

        // =========================================================
        // HTTP helper
        // =========================================================
        private async Task<string> SendAndReadAsync(HttpMethod method, string url, object? body, bool throwOnBusinessError = true)
        {
            var req = _apiClient.CreateRequest(method, url, true);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var resp = await _apiClient.Http.SendAsync(req);
            var content = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var msg = TryExtractMessage(content);
                if (string.IsNullOrWhiteSpace(msg))
                {
                    var snippet = (content ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (snippet.Length > 220) snippet = snippet.Substring(0, 220) + "...";
                    msg = $"{(int)resp.StatusCode} {resp.ReasonPhrase}".Trim();
                    if (!string.IsNullOrWhiteSpace(snippet))
                        msg += $" | {snippet}";
                }
                throw new Exception(msg);
            }

            if (throwOnBusinessError)
            {
                var businessError = TryExtractBusinessError(content);
                if (!string.IsNullOrWhiteSpace(businessError))
                    throw new Exception(businessError);
            }

            return string.IsNullOrWhiteSpace(content) ? "{}" : content;
        }

        private static string? TryExtractMessage(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    TryGetPropertyIgnoreCase(doc.RootElement, "message", out var m) &&
                    m.ValueKind == JsonValueKind.String)
                    return m.GetString();
                return null;
            }
            catch { return null; }
        }

        private static string? TryExtractBusinessError(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                if (!TryGetPropertyIgnoreCase(root, "statusCode", out var statusCodeEl))
                    return null;

                var isError = statusCodeEl.ValueKind switch
                {
                    JsonValueKind.Number => statusCodeEl.TryGetInt32(out var n) && n >= 400,
                    JsonValueKind.False => true,
                    JsonValueKind.True => false,
                    JsonValueKind.String => ParseStatusCodeString(statusCodeEl.GetString()),
                    _ => false
                };

                if (!isError) return null;

                if (TryGetPropertyIgnoreCase(root, "message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                {
                    var message = msgEl.GetString();
                    if (!string.IsNullOrWhiteSpace(message)) return message;
                }

                return "Backend trả về lỗi nghiệp vụ.";
            }
            catch
            {
                return null;
            }
        }

        private static bool ParseStatusCodeString(string? value)
        {
            var v = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(v)) return false;

            if (int.TryParse(v, out var number))
                return number >= 400;

            if (bool.TryParse(v, out var boolVal))
                return !boolVal;

            return false;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in obj.EnumerateObject())
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = p.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        // =========================================================
        // JSON tolerant seat parsing
        // =========================================================
        private static SeatGroups ParseSeatGroupsTolerant(string json)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = doc.RootElement;

            JsonElement dataEl;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d))
                dataEl = d;
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("Data", out var dc))
                dataEl = dc;
            else
                dataEl = root;

            var groups = new SeatGroups();

            // A: { loaiVe1:[], loaiVe3:[], loaiVe4:[] }
            if (dataEl.ValueKind == JsonValueKind.Object)
            {
                if (TryGetArray(dataEl, "loaiVe1", out var a1) || TryGetArray(dataEl, "LoaiVe1", out a1))
                    groups.LoaiVe1 = ParseSeatArray(a1);

                if (TryGetArray(dataEl, "loaiVe3", out var a3) || TryGetArray(dataEl, "LoaiVe3", out a3))
                    groups.LoaiVe3 = ParseSeatArray(a3);

                if (TryGetArray(dataEl, "loaiVe4", out var a4) || TryGetArray(dataEl, "LoaiVe4", out a4))
                    groups.LoaiVe4 = ParseSeatArray(a4);
            }

            // B: data là mảng ghế, mỗi ghế có idLoaiVe
            if (groups.LoaiVe1.Count == 0 && groups.LoaiVe3.Count == 0 && groups.LoaiVe4.Count == 0)
            {
                if (dataEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in dataEl.EnumerateArray())
                    {
                        var idLoai = GetInt(it, "idLoaiVe", "IdLoaiVe");
                        var dto = ParseSeat(it);
                        if (idLoai == 1) groups.LoaiVe1.Add(dto);
                        else if (idLoai == 3) groups.LoaiVe3.Add(dto);
                        else if (idLoai == 4) groups.LoaiVe4.Add(dto);
                        else groups.LoaiVe1.Add(dto);
                    }
                }
            }

            return groups;
        }

        private static List<SeatDtoLite> ParseSeatArray(JsonElement arr)
        {
            var list = new List<SeatDtoLite>();
            if (arr.ValueKind != JsonValueKind.Array) return list;

            foreach (var it in arr.EnumerateArray())
                list.Add(ParseSeat(it));

            return list;
        }

        private static SeatDtoLite ParseSeat(JsonElement it)
        {
            return new SeatDtoLite
            {
                IdGheNgoi = GetLong(it, "idGheNgoi", "IdGheNgoi", "id", "Id"),
                SoGhe = GetString(it, "soGhe", "SoGhe") ?? "",
                IdTrangThai = GetInt(it, "idTrangThai", "IdTrangThai")
            };
        }

        private static bool TryGetArray(JsonElement obj, string prop, out JsonElement arr)
        {
            arr = default;
            return obj.ValueKind == JsonValueKind.Object
                && obj.TryGetProperty(prop, out arr)
                && arr.ValueKind == JsonValueKind.Array;
        }

        private static bool TryParseFailedSeatIds(string json, out HashSet<long> failed)
        {
            failed = new HashSet<long>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    var propNames = new[] { "failedSeatIds", "failedSeats", "FailedSeatIds", "FailedSeats" };
                    foreach (var prop in propNames)
                    {
                        if (!TryGetPropertyIgnoreCase(root, prop, out var fs) || fs.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var it in fs.EnumerateArray())
                        {
                            if (it.ValueKind == JsonValueKind.Number && it.TryGetInt64(out var n))
                                failed.Add(n);
                            else if (it.ValueKind == JsonValueKind.String && long.TryParse(it.GetString(), out var s))
                                failed.Add(s);
                        }

                        if (failed.Count > 0)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static long GetLong(JsonElement it, params string[] names)
        {
            foreach (var n in names)
            {
                if (it.ValueKind == JsonValueKind.Object && it.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var num)) return num;
                    if (v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), out var s)) return s;
                }
            }
            return 0;
        }

        private static int GetInt(JsonElement it, params string[] names)
        {
            foreach (var n in names)
            {
                if (it.ValueKind == JsonValueKind.Object && it.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var num)) return num;
                    if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var s)) return s;
                }
            }
            return 0;
        }

        private static decimal GetDecimal(JsonElement it, params string[] names)
        {
            foreach (var n in names)
            {
                if (it.ValueKind == JsonValueKind.Object && it.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var num)) return num;
                    if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var s)) return s;
                }
            }
            return 0;
        }

        private static string? GetString(JsonElement it, params string[] names)
        {
            foreach (var n in names)
            {
                if (it.ValueKind == JsonValueKind.Object && it.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.String) return v.GetString();
                    return v.ToString();
                }
            }
            return null;
        }

        private static void OpenUrlInBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        // ===== nested types =====
        private class SeatGroups
        {
            public List<SeatDtoLite> LoaiVe1 { get; set; } = new();
            public List<SeatDtoLite> LoaiVe3 { get; set; } = new();
            public List<SeatDtoLite> LoaiVe4 { get; set; } = new();
        }

        private class SeatDtoLite
        {
            public long IdGheNgoi { get; set; }
            public string? SoGhe { get; set; }
            public int IdTrangThai { get; set; }
        }

        public class SeatVm : ObservableObject
        {
            public long IdGheNgoi { get; set; }
            public string SoGhe { get; set; } = "";

            private int _idTrangThai;
            public int IdTrangThai 
            { 
                get => _idTrangThai; 
                set { if (SetProperty(ref _idTrangThai, value)) OnPropertyChanged(nameof(IsAvailable)); }
            }

            private bool _isBookedByMe;
            public bool IsBookedByMe 
            { 
                get => _isBookedByMe; 
                set { if (SetProperty(ref _isBookedByMe, value)) OnPropertyChanged(nameof(IsAvailable)); }
            }

            // ✅ Enabled nếu là ghế trống (0) HOẶC là ghế đang giữ để đặt (IsSelected)
            // NHƯNG nếu đã thanh toán thành công (IsBookedByMe) thì nên khóa lại.
            public bool IsAvailable => (IdTrangThai == 0 || IsSelected) && !IsBookedByMe;

            private bool _isSelected;
            public bool IsSelected 
            { 
                get => _isSelected; 
                set { if (SetProperty(ref _isSelected, value)) OnPropertyChanged(nameof(IsAvailable)); }
            }
        }

        private class ParsedSeat
        {
            public SeatVm Seat { get; set; } = null!;
            public int Index { get; set; }
            public int? Row { get; set; }
            public char? Col { get; set; }
        }

        public class SeatRowVm : ObservableObject
        {
            public string RowLabel { get; set; } = "";
            public ObservableCollection<SeatVm> LeftSeats { get; } = new();
            public ObservableCollection<SeatVm> RightSeats { get; } = new();
        }

        public class PaymentHistoryItemVm : ObservableObject
        {
            public string LoaiDichVu { get; set; } = "";
            public string TrangThai { get; set; } = "";
            public string MaThanhToan { get; set; } = "";
            public string Provider { get; set; } = "";
            public string SoTien { get; set; } = "";
            public string Ngay { get; set; } = "";
            public int? StatusCode { get; set; }
            public string DisplayLine { get; set; } = "";
        }

        public class VoucherItemVm : ObservableObject
        {
            public long IdChiTiet { get; set; }
            public long? IdMaGiamGia { get; set; }
            public string MaGiamGia { get; set; } = "";
            public decimal GiaTriGiam { get; set; }
            public int IdLoaiGiamGia { get; set; }

            public string DisplayText => $"{MaGiamGia} (-{GiaTriGiam:n0})";
        }

        public record PaymentOption(int Id, string Name)
        {
            public override string ToString() => Name;
        }
    }
}
