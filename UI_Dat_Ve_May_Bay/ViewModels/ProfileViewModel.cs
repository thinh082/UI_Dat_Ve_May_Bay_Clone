using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Text.RegularExpressions;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Services;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class ProfileViewModel : ObservableObject
    {
        private readonly ApiClient _apiClient;
        private readonly TokenStore _tokenStore = new TokenStore();

        public class ConfigOption
        {
            public string Id { get; set; } = "";
            public string Ten { get; set; } = "";
            public string? ParentId { get; set; }
            public override string ToString() => $"{Ten} ({Id})";
        }


        public ProfileViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
            EnsureToken();

            LoadAllCommand = new AsyncRelayCommand(LoadAllAsync);
            SaveBasicCommand = new AsyncRelayCommand(SaveBasicAsync);
            SaveCccdCommand = new AsyncRelayCommand(SaveCccdAsync);
            SavePassportCommand = new AsyncRelayCommand(SavePassportAsync);
            LoadConfigCommand = new AsyncRelayCommand(LoadConfigListsAsync);

            _ = LoadAllAsync();
        }

        #region State
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private string _status = "Sẵn sàng";
        public string Status 
        { 
            get => _status; 
            set 
            { 
                if (SetProperty(ref _status, value))
                    OnPropertyChanged(nameof(HasStatus));
            } 
        }

        private string _error = "";
        public string Error 
        { 
            get => _error; 
            set 
            { 
                if (SetProperty(ref _error, value))
                    OnPropertyChanged(nameof(HasError));
            } 
        }

        public bool HasStatus => !string.IsNullOrWhiteSpace(Status) && Status != "Sẵn sàng";
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        private string _avatarUrl = "";
        public string AvatarUrl { get => _avatarUrl; set => SetProperty(ref _avatarUrl, value); }
        #endregion

        #region Basic profile
        private string _tenKh = "";
        public string TenKh { get => _tenKh; set => SetProperty(ref _tenKh, value); }

        private string _email = "";
        public string Email { get => _email; set => SetProperty(ref _email, value); }

        private string _soDienThoai = "";
        public string SoDienThoai { get => _soDienThoai; set => SetProperty(ref _soDienThoai, value); }

        private string _soNha = "";
        public string SoNha { get => _soNha; set => SetProperty(ref _soNha, value); }

        private string _gioiTinh = "";
        public string GioiTinh { get => _gioiTinh; set { if (SetProperty(ref _gioiTinh, value)) OnPropertyChanged(nameof(GioiTinhText)); } }

        private string _idPhuong = "";
        public string IdPhuong { get => _idPhuong; set { if (SetProperty(ref _idPhuong, value)) OnPropertyChanged(nameof(PhuongText)); } }

        private string _idQuan = "";
        public string IdQuan { get => _idQuan; set { if (SetProperty(ref _idQuan, value)) { RefreshPhuongOptions(); OnPropertyChanged(nameof(QuanText)); } } }

        private string _idTinh = "";
        public string IdTinh { get => _idTinh; set { if (SetProperty(ref _idTinh, value)) { RefreshQuanOptions(); OnPropertyChanged(nameof(TinhText)); } } }

        private string _idQuocTich = "";
        public string IdQuocTich { get => _idQuocTich; set { if (SetProperty(ref _idQuocTich, value)) OnPropertyChanged(nameof(QuocTichText)); } }

        public string GioiTinhText => GioiTinhOptions.FirstOrDefault(x => x.Id == GioiTinh)?.Ten ?? "";
        public string QuocTichText => QuocTichOptions.FirstOrDefault(x => x.Id == IdQuocTich)?.Ten ?? "";
        public string TinhText => TinhOptions.FirstOrDefault(x => x.Id == IdTinh)?.Ten ?? "";
        public string QuanText => QuanOptionsAll.FirstOrDefault(x => x.Id == IdQuan)?.Ten ?? "";
        public string PhuongText => PhuongOptionsAll.FirstOrDefault(x => x.Id == IdPhuong)?.Ten ?? "";
        #endregion

        #region Config dropdown data
        public ObservableCollection<ConfigOption> TinhOptions { get; } = new();
        public ObservableCollection<ConfigOption> QuanOptionsAll { get; } = new();
        public ObservableCollection<ConfigOption> PhuongOptionsAll { get; } = new();
        public ObservableCollection<ConfigOption> QuocTichOptions { get; } = new();
        public ObservableCollection<ConfigOption> GioiTinhOptions { get; } = new();

        public ObservableCollection<ConfigOption> QuanOptions { get; } = new();
        public ObservableCollection<ConfigOption> PhuongOptions { get; } = new();

        public bool HasTinhOptions => TinhOptions.Count > 0;
        public bool HasQuanOptions => QuanOptions.Count > 0;
        public bool HasPhuongOptions => PhuongOptions.Count > 0;
        public bool HasQuocTichOptions => QuocTichOptions.Count > 0;
        public bool NoTinhOptions => !HasTinhOptions;
        public bool NoQuanOptions => !HasQuanOptions;
        public bool NoPhuongOptions => !HasPhuongOptions;
        public bool NoQuocTichOptions => !HasQuocTichOptions;
        #endregion

        #region CCCD
        private string _soCccd = "";
        public string SoCccd { get => _soCccd; set => SetProperty(ref _soCccd, value); }

        private string _tenTrenCccd = "";
        public string TenTrenCccd { get => _tenTrenCccd; set => SetProperty(ref _tenTrenCccd, value); }

        private string _noiThuongTru = "";
        public string NoiThuongTru { get => _noiThuongTru; set => SetProperty(ref _noiThuongTru, value); }

        private string _queQuan = "";
        public string QueQuan { get => _queQuan; set => SetProperty(ref _queQuan, value); }
        #endregion

        #region Passport
        private string _soPassport = "";
        public string SoPassport { get => _soPassport; set => SetProperty(ref _soPassport, value); }

        private string _tenTrenPassport = "";
        public string TenTrenPassport { get => _tenTrenPassport; set => SetProperty(ref _tenTrenPassport, value); }

        private string _noiCap = "";
        public string NoiCap { get => _noiCap; set => SetProperty(ref _noiCap, value); }

        private DateTime? _ngayCap;
        public DateTime? NgayCap { get => _ngayCap; set => SetProperty(ref _ngayCap, value); }

        private DateTime? _ngayHetHan;
        public DateTime? NgayHetHan { get => _ngayHetHan; set => SetProperty(ref _ngayHetHan, value); }

        private string _passportQuocTich = "";
        public string PassportQuocTich { get => _passportQuocTich; set => SetProperty(ref _passportQuocTich, value); }

        private string _loaiPassport = "";
        public string LoaiPassport { get => _loaiPassport; set => SetProperty(ref _loaiPassport, value); }

        private string _ghiChuPassport = "";
        public string GhiChuPassport { get => _ghiChuPassport; set => SetProperty(ref _ghiChuPassport, value); }
        #endregion

        public AsyncRelayCommand LoadAllCommand { get; }
        public AsyncRelayCommand SaveBasicCommand { get; }
        public AsyncRelayCommand SaveCccdCommand { get; }
        public AsyncRelayCommand SavePassportCommand { get; }
        public AsyncRelayCommand LoadConfigCommand { get; }

        private void EnsureToken()
        {
            if (!string.IsNullOrWhiteSpace(_apiClient.Token))
            {
                _apiClient.Token = _apiClient.Token!.Trim().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                return;
            }

            var tk = _tokenStore.Load();
            if (!string.IsNullOrWhiteSpace(tk))
                _apiClient.Token = tk.Trim();
        }

        private async Task LoadAllAsync()
        {
            await RunSafe(async () =>
            {
                Status = "Đang tải hồ sơ...";
                Error = "";
                EnsureToken();

                // Tải danh mục dropdown (best-effort, có thì dùng dropdown, không có vẫn nhập tay)
                await TryLoadConfigListsAsync();

                // Ưu tiên load thông tin cơ bản. Nếu 401 thì dừng luôn để tránh spam lỗi.
                var basicOk = await TryLoadBasicAsync();
                if (!basicOk) return;

                await TryLoadCccdAsync();
                await TryLoadPassportAsync();

                Status = "Đã tải hồ sơ khách hàng";
            });
        }

        private async Task TryLoadConfigListsAsync()
        {
            try { await LoadConfigListsAsync(); }
            catch { /* best effort */ }
        }

        private async Task<bool> TryLoadBasicAsync()
        {
            try
            {
                await LoadBasicAsync();
                return true;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Status = ex.Message.Contains("đăng nhập", StringComparison.OrdinalIgnoreCase)
                    ? "Phiên đăng nhập không hợp lệ"
                    : "Không tải được thông tin cơ bản";
                return false;
            }
        }

        private async Task TryLoadCccdAsync()
        {
            try { await LoadCccdAsync(); }
            catch { /* best effort */ }
        }

        private async Task TryLoadPassportAsync()
        {
            try { await LoadPassportAsync(); }
            catch { /* best effort */ }
        }

        private async Task LoadBasicAsync()
        {
            // Một số tài khoản chưa có dữ liệu chi tiết -> endpoint này có thể 500.
            // Vì vậy load ThongTinCoBan trước cho chắc, rồi thử ThongTinCapNhat sau.
            var docCoBan = await SendJsonAsync(HttpMethod.Post, "/api/KhachHang/ThongTinCoBan", body: null);
            var dataCoBan = GetData(docCoBan.RootElement);
            if (dataCoBan.HasValue)
            {
                TenKh = GetString(dataCoBan.Value, "TenKh", "HoTen");
                Email = GetString(dataCoBan.Value, "Email");
                SoDienThoai = GetString(dataCoBan.Value, "SoDienThoai", "SDT");
                AvatarUrl = GetString(dataCoBan.Value, "HinhAnh", "Avatar", "UrlAnh");
            }

            // load chi tiết (best effort)
            try
            {
                var doc = await SendJsonAsync(HttpMethod.Post, "/api/KhachHang/ThongTinCapNhat", body: null);
                var data = GetData(doc.RootElement);
                if (data.HasValue)
                {
                    TenKh = FirstNonEmpty(TenKh, GetString(data.Value, "TenKh"));
                    Email = FirstNonEmpty(Email, GetString(data.Value, "Email"));
                    SoDienThoai = FirstNonEmpty(SoDienThoai, GetString(data.Value, "SoDienThoai"));
                    SoNha = GetString(data.Value, "SoNha");
                    IdTinh = ToIdText(GetInt(data.Value, "IdTinh"));
                    IdQuan = ToIdText(GetInt(data.Value, "IdQuan"));
                    IdPhuong = ToIdText(GetInt(data.Value, "IdPhuong"));
                    GioiTinh = ToIdText(GetInt(data.Value, "GioiTinh"));
                    IdQuocTich = ToIdText(GetInt(data.Value, "IdQuocTich"));
                    AvatarUrl = FirstNonEmpty(AvatarUrl, GetString(data.Value, "HinhAnh"));
                }
            }
            catch (Exception ex)
            {
                // ✅ FIX: Luôn set Error, không chỉ khi nó trống
                Status = "Đã tải thông tin cơ bản (thiếu dữ liệu chi tiết)";
                Error = ex.Message;
            }
        }

        private async Task LoadCccdAsync()
        {
            var doc = await SendJsonAsync(HttpMethod.Post, "/api/KhachHang/ThongTinCCCD", body: null);
            var data = GetData(doc.RootElement);
            if (!data.HasValue) return;

            SoCccd = GetString(data.Value, "SoCccd", "SoCCCD");
            TenTrenCccd = GetString(data.Value, "TenTrenCccd", "TenTrenCCCD", "TenKh");
            NoiThuongTru = GetString(data.Value, "NoiThuongTru");
            QueQuan = GetString(data.Value, "QueQuan");
        }

        private async Task LoadPassportAsync()
        {
            var doc = await SendJsonAsync(HttpMethod.Get, "/api/KhachHang/ThongTinPassport", body: null);
            var data = GetData(doc.RootElement);
            if (!data.HasValue) return;

            SoPassport = GetString(data.Value, "SoPassport");
            TenTrenPassport = GetString(data.Value, "TenTrenPassport", "TenKh");
            NoiCap = GetString(data.Value, "NoiCap");
            NgayCap = GetDateTime(data.Value, "NgayCap");
            NgayHetHan = GetDateTime(data.Value, "NgayHetHan");
            PassportQuocTich = GetString(data.Value, "QuocTich");
            LoaiPassport = GetString(data.Value, "LoaiPassport");
            GhiChuPassport = GetString(data.Value, "GhiChu");
        }

        private async Task LoadConfigListsAsync()
        {
            // Bây giờ lấy dữ liệu từ folder local 'data' thay vì gọi BE
            var tinh = await LoadConfigJsonArrayAsync("Tinh.json");
            var quan = await LoadConfigJsonArrayAsync("Quan.json");
            var phuong = await LoadConfigJsonArrayAsync("Phuong.json");
            var quocTich = await LoadConfigJsonArrayAsync("QuocTich.json");
            var gioiTinh = await LoadConfigJsonArrayAsync("GioiTinh.json");

            FillOptions(TinhOptions, tinh, "id", "ten", null);
            FillOptions(QuanOptionsAll, quan, "id", "ten", "idTinh");
            FillOptions(PhuongOptionsAll, phuong, "id", "ten", "idQuan");
            FillOptions(QuocTichOptions, quocTich, "id", "ten", null);
            FillOptions(GioiTinhOptions, gioiTinh, "id", "ten", null);

            RefreshQuanOptions();
            RefreshPhuongOptions();

            OnPropertyChanged(nameof(HasTinhOptions));
            OnPropertyChanged(nameof(NoTinhOptions));
            OnPropertyChanged(nameof(HasQuanOptions));
            OnPropertyChanged(nameof(NoQuanOptions));
            OnPropertyChanged(nameof(HasPhuongOptions));
            OnPropertyChanged(nameof(NoPhuongOptions));
            OnPropertyChanged(nameof(HasQuocTichOptions));
            OnPropertyChanged(nameof(NoQuocTichOptions));

            if (TinhOptions.Count > 0 || QuocTichOptions.Count > 0)
                Status = "Đã tải danh mục từ folder local 'data'";
            else
                Status = "CẢNH BÁO: Không tìm thấy dữ liệu trong folder 'data'. Hãy kiểm tra đường dẫn file.";
        }

        private void RefreshQuanOptions()
        {
            QuanOptions.Clear();
            var pid = (IdTinh ?? "").Trim();
            foreach (var q in QuanOptionsAll)
            {
                if (string.IsNullOrWhiteSpace(pid) || string.Equals(q.ParentId, pid, StringComparison.OrdinalIgnoreCase))
                    QuanOptions.Add(q);
            }

            if (!string.IsNullOrWhiteSpace(IdQuan) && QuanOptions.All(x => x.Id != IdQuan))
            {
                _idQuan = ""; 
                OnPropertyChanged(nameof(IdQuan));
                OnPropertyChanged(nameof(QuanText));
            }
            RefreshPhuongOptions();
            OnPropertyChanged(nameof(HasQuanOptions));
            OnPropertyChanged(nameof(NoQuanOptions));
        }

        private void RefreshPhuongOptions()
        {
            PhuongOptions.Clear();
            var pid = (IdQuan ?? "").Trim();
            foreach (var p in PhuongOptionsAll)
            {
                if (string.IsNullOrWhiteSpace(pid) || string.Equals(p.ParentId, pid, StringComparison.OrdinalIgnoreCase))
                    PhuongOptions.Add(p);
            }

            if (!string.IsNullOrWhiteSpace(IdPhuong) && PhuongOptions.All(x => x.Id != IdPhuong))
            {
                _idPhuong = ""; 
                OnPropertyChanged(nameof(IdPhuong));
                OnPropertyChanged(nameof(PhuongText));
            }
            OnPropertyChanged(nameof(HasPhuongOptions));
            OnPropertyChanged(nameof(NoPhuongOptions));
        }

        private async Task TryCallNoThrow(HttpMethod method, string path)
        {
            try
            {
                using var req = _apiClient.CreateRequest(method, path, attachAuth: false);
                using var res = await _apiClient.Http.SendAsync(req);
            }
            catch { }
        }

        private async Task<JsonElement[]> LoadConfigJsonArrayAsync(string fileName)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // Các đường dẫn có thể chứa folder data
                var candidates = new List<string>
                {
                    Path.Combine(baseDir, "data", fileName), // Trong bin (nếu có Copy to Output)
                    Path.Combine(baseDir, "..", "..", "..", "data", fileName), // bin/Debug/netX.0 -> ProjectRoot
                    Path.Combine(baseDir, "..", "..", "data", fileName), // bin/Debug -> ProjectRoot
                    Path.Combine(Directory.GetCurrentDirectory(), "data", fileName), // Cwd
                    Path.Combine(baseDir, fileName) // Trực tiếp trong bin
                };

                string? filePath = null;
                foreach (var c in candidates)
                {
                    if (File.Exists(c))
                    {
                        filePath = c;
                        break;
                    }
                }

                if (filePath == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy file config: {fileName}");
                    return Array.Empty<JsonElement>();
                }

                var text = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(text)) return Array.Empty<JsonElement>();

                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    return doc.RootElement.EnumerateArray().Select(x => x.Clone()).ToArray();
            }
            catch (Exception ex)
            {
                Status = $"Lỗi load {fileName}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(Status);
            }
            return Array.Empty<JsonElement>();
        }
    
        private void FillOptions(ObservableCollection<ConfigOption> target, JsonElement[] source, string idKey, string tenKey, string? parentKey)
        {
            target.Clear();
            foreach (var el in source)
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var id = GetString(el, idKey);
                var ten = GetString(el, tenKey);
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ten)) continue;
                target.Add(new ConfigOption
                {
                    Id = id,
                    Ten = ten,
                    ParentId = string.IsNullOrWhiteSpace(parentKey) ? null : GetString(el, parentKey)
                });
            }
            // sort by name for nice UX
            var sorted = target.OrderBy(x => x.Ten).ToList();
            target.Clear();
            foreach (var item in sorted) target.Add(item);
        }

        private async Task SaveBasicAsync()
        {
            await RunSafe(async () =>
            {
                ValidateRequire(TenKh, "Họ và tên");
                ValidateRequire(Email, "Email liên hệ");
                ValidateRequire(SoDienThoai, "Số điện thoại");
                ValidatePhone(SoDienThoai);
                ValidateRequire(GioiTinh, "Giới tính");
                ValidateRequire(SoNha, "Số nhà / Đường");
                ValidateRequire(IdTinh, "Tỉnh/TP");
                ValidateRequire(IdQuan, "Quận/Huyện");
                ValidateRequire(IdPhuong, "Phường/Xã");
                ValidateRequire(IdQuocTich, "Quốc tịch");

                var body = new
                {
                    Email = Email.Trim(),
                    SoDienThoai = SoDienThoai.Trim(),
                    TenKh = TenKh.Trim(),
                    SoNha = SoNha.Trim(),
                    GioiTinh = ParseNullableInt(GioiTinh),
                    IdPhuong = ParseNullableInt(IdPhuong),
                    IdQuan = ParseNullableInt(IdQuan),
                    IdTinh = ParseNullableInt(IdTinh),
                    IdQuocTich = ParseNullableInt(IdQuocTich)
                };

                var doc = await SendJsonAsync(HttpMethod.Post, "/api/KhachHang/CapNhatThongTin", body);
                Status = GetMessage(doc.RootElement, "Cập nhật thông tin cơ bản thành công");
                if (string.IsNullOrWhiteSpace(Status)) Status = "Cập nhật thông tin cơ bản thành công";
                // ✅ FIX: Chỉ dùng Status message, không hiển thị dialog
                // Để tránh dialog che phủ form và code tiếp tục chạy
                await LoadBasicAsync();
            });
        }

        private async Task SaveCccdAsync()
        {
            await RunSafe(async () =>
            {
                ValidateRequire(SoCccd, "Số CCCD / ID Card");
                ValidateCccd(SoCccd);
                ValidateRequire(TenTrenCccd, "Họ và tên trên CCCD");
                ValidateRequire(NoiThuongTru, "Địa chỉ thường trú");
                ValidateRequire(QueQuan, "Quê quán");

                var body = new
                {
                    SoCCCD = SoCccd.Trim(),
                    TenTrenCCCD = TenTrenCccd.Trim(),
                    NoiThuongTru = NoiThuongTru.Trim(),
                    QueQuan = QueQuan.Trim()
                };

                var doc = await SendJsonAsync(HttpMethod.Post, "/api/KhachHang/CapNhatCCCD", body);
                Status = GetMessage(doc.RootElement, "Cập nhật CCCD thành công");
                if (string.IsNullOrWhiteSpace(Status)) Status = "Cập nhật CCCD thành công";
                // ✅ FIX: Chỉ dùng Status message
                await TryLoadCccdAsync();
            });
        }

        private async Task SavePassportAsync()
        {
            await RunSafe(async () =>
            {
                ValidateRequire(SoPassport, "Số Hộ chiếu");
                ValidatePassport(SoPassport);
                ValidateRequire(TenTrenPassport, "Tên trên Hộ chiếu");
                ValidateRequire(NoiCap, "Nơi cấp");
                ValidateDate(NgayCap, "Ngày cấp");
                ValidateDate(NgayHetHan, "Ngày hết hạn");
                ValidateRequire(PassportQuocTich, "Quốc tịch (Passport)");
                ValidateRequire(LoaiPassport, "Loại Passport");

                var body = new
                {
                    SoPassport = SoPassport.Trim(),
                    TenTrenPassport = TenTrenPassport.Trim(),
                    NoiCap = NoiCap.Trim(),
                    NgayCap = NgayCap,
                    NgayHetHan = NgayHetHan,
                    QuocTich = PassportQuocTich.Trim(),
                    LoaiPassport = LoaiPassport.Trim(),
                    GhiChu = NullIfEmpty(GhiChuPassport)
                };

                var doc = await SendJsonAsync(HttpMethod.Post, "/api/KhachHang/CapNhatPassport", body);
                Status = GetMessage(doc.RootElement, "Cập nhật Passport thành công");
                if (string.IsNullOrWhiteSpace(Status)) Status = "Cập nhật Passport thành công";
                // ✅ FIX: Chỉ dùng Status message
                await TryLoadPassportAsync();
            });
        }

        private async Task RunSafe(Func<Task> action)
        {
            try
            {
                IsBusy = true;
                Error = "";
                EnsureToken();
                if (string.IsNullOrWhiteSpace(_apiClient.Token))
                    throw new Exception("Bạn chưa đăng nhập hoặc token không hợp lệ.");
                await action();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Status = "Có lỗi xảy ra";
                UI_Dat_Ve_May_Bay.Services.DialogService.ShowError(ex.Message, "Thông báo lỗi");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<JsonDocument> SendJsonAsync(HttpMethod method, string path, object? body)
        {
            EnsureToken();

            using var req = body == null
                ? _apiClient.CreateRequest(method, path, attachAuth: true)
                : _apiClient.CreateJsonRequest(method, path, body, attachAuth: true);

            using var res = await _apiClient.Http.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                var msg = ReadMessageFromJson(text);

                if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception(msg ?? "Phiên đăng nhập hết hạn hoặc token không hợp lệ. Vui lòng đăng nhập lại.");
                }

                if (res.StatusCode == HttpStatusCode.InternalServerError)
                {
                    throw new Exception(msg ?? (string.IsNullOrWhiteSpace(text)
                        ? "BE trả lỗi 500 (Internal Server Error)."
                        : $"BE trả lỗi 500: {text}"));
                }

                throw new Exception(msg ?? $"HTTP {(int)res.StatusCode}: {res.ReasonPhrase}");
            }

            // BE có endpoint có thể trả rỗng / plain text -> bọc lại để tránh JsonReaderException
            if (string.IsNullOrWhiteSpace(text))
                return JsonDocument.Parse("{}");

            try
            {
                var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    int? statusCode = GetInt(root, "statusCode", "StatusCode");
                    if (statusCode.HasValue && statusCode.Value >= 400)
                    {
                        var msg = GetMessage(root, $"BE trả lỗi {statusCode.Value}.");
                        if (msg.Contains("Không tìm thấy thông tin khách", StringComparison.OrdinalIgnoreCase))
                        {
                            msg = "Vui lòng cập nhật và lưu 'Thông tin cơ bản' trước khi thực hiện thao tác này.";
                        }
                        throw new Exception(msg);
                    }
                }
                return doc;
            }
            catch (JsonException)
            {
                // convert plain text thành json giả để UI vẫn lấy message được
                var escaped = JsonSerializer.Serialize(text);
                return JsonDocument.Parse($"{{\"message\":{escaped}}}");
            }
        }

        private static string? ReadMessageFromJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try
            {
                using var doc = JsonDocument.Parse(text);
                return GetMessage(doc.RootElement, null);
            }
            catch
            {
                return text.Length > 300 ? text[..300] + "..." : text;
            }
        }

        private static JsonElement? GetData(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetProperty(root, "data", out var d) || TryGetProperty(root, "Data", out d)) return d;
                return root; // một số endpoint có thể trả object trực tiếp
            }
            return null;
        }

        private static string GetMessage(JsonElement root, string? fallback)
        {
            if (root.ValueKind == JsonValueKind.Object &&
                (TryGetProperty(root, "message", out var m) || TryGetProperty(root, "Message", out m)))
            {
                if (m.ValueKind == JsonValueKind.String) return m.GetString() ?? (fallback ?? "");
                return m.ToString();
            }
            return fallback ?? "";
        }

        private static string GetString(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetProperty(el, n, out var p))
                {
                    if (p.ValueKind == JsonValueKind.String) return p.GetString() ?? "";
                    if (p.ValueKind == JsonValueKind.Number || p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)
                        return p.ToString();
                }
            }
            return "";
        }

        private static int? GetInt(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetProperty(el, n, out var p))
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var v)) return v;
                    if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out v)) return v;
                }
            }
            return null;
        }

        private static DateTime? GetDateTime(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetProperty(el, n, out var p) && p.ValueKind == JsonValueKind.String)
                {
                    var s = p.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return null;
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                        return dt;
                }
            }
            return null;
        }

        private static bool TryGetProperty(JsonElement el, string name, out JsonElement value)
        {
            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in el.EnumerateObject())
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

        private static int? ParseNullableInt(string? text)
        {
            if (int.TryParse(text?.Trim(), out var v)) return v;
            return null;
        }

        private static string? NullIfEmpty(string? text)
            => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static string ToIdText(int? v) => v.HasValue && v.Value > 0 ? v.Value.ToString() : "";
        private static string FirstNonEmpty(string current, string incoming)
            => string.IsNullOrWhiteSpace(current) ? incoming : current;

        private static void ValidateRequire(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception($"{fieldName} không được để trống.");
        }

        private static void ValidateDate(DateTime? value, string fieldName)
        {
            if (!value.HasValue)
                throw new Exception($"{fieldName} không được để trống.");
        }

        private static void ValidatePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!Regex.IsMatch(value, @"^\d{10}$"))
                throw new Exception("Số điện thoại phải đủ 10 số.");
        }

        private static void ValidateCccd(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!Regex.IsMatch(value, @"^\d{12}$"))
                throw new Exception("Số CCCD phải đủ 12 số.");
        }

        private static void ValidatePassport(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            // Gồm 1 chữ cái đứng đầu và 7 số
            if (!Regex.IsMatch(value, @"^[A-Z]\d{7}$", RegexOptions.IgnoreCase))
                throw new Exception("Số hộ chiếu phải gồm 1 chữ cái và 7 số (VD: A1234567).");
        }
    }
}
