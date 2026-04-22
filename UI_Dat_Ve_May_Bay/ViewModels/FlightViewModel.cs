using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Core;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class FlightViewModel : ObservableObject
    {
        private readonly ApiClient _apiClient;
        private readonly FlightApi _flightApi;
        private readonly Action<FlightViewModel.LichBayItemVm?> _onContinue;

        private bool _suppressLookupSync;

        public FlightViewModel(ApiClient apiClient, Action<FlightViewModel.LichBayItemVm?> onContinue)
        {
            _apiClient = apiClient;
            _flightApi = new FlightApi(apiClient);
            _onContinue = onContinue;

            // default (fallback)
            FromCode = "SGN";
            ToCode = "HAN";
            DepartureDate = DateTime.Today;
            PriceMin = 0;
            PriceMax = 5000000;

            // BE require idTienNghi string
            IdTienNghi = "1";
            IdLoaiVe = 1;
            IdHangBay = 1;

            SortOptions = new ObservableCollection<string>
            {
                "Ghế còn nhiều nhất",
                "Giá tăng dần",
                "Giá giảm dần",
                "Giờ đi sớm nhất",
                "Giờ đi muộn nhất",
            };
            SelectedSort = SortOptions.FirstOrDefault();

            SearchCommand = new AsyncRelayCommand(SearchAsync);
            SwapCommand = new RelayCommand(_ => Swap());
            SelectScheduleCommand = new RelayCommand(p => SelectSchedule(p as LichBayItemVm));
            ContinueCommand = new RelayCommand(_ => Continue(), _ => SelectedSchedule != null);

            FlightGroups.CollectionChanged += FlightGroups_CollectionChanged;

            // Load lookups (best-effort, nếu BE không expose được thì fallback list nhỏ)
            _ = LoadLookupsAsync();
        }

        private void FlightGroups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(Results));
        }

        // ================================
        // Lookup sources (Config dropdown)
        // ================================
        public ObservableCollection<LookupItem> Airports { get; } = new();
        public ObservableCollection<LookupItem> Airlines { get; } = new();
        public ObservableCollection<LookupItem> Amenities { get; } = new();

        private LookupItem? _selectedFromAirport;
        public LookupItem? SelectedFromAirport
        {
            get => _selectedFromAirport;
            set
            {
                if (SetProperty(ref _selectedFromAirport, value) && !_suppressLookupSync && value != null)
                {
                    _suppressLookupSync = true;
                    FromCode = value.Code ?? value.IdText ?? "";
                    _suppressLookupSync = false;
                }
            }
        }

        private LookupItem? _selectedToAirport;
        public LookupItem? SelectedToAirport
        {
            get => _selectedToAirport;
            set
            {
                if (SetProperty(ref _selectedToAirport, value) && !_suppressLookupSync && value != null)
                {
                    _suppressLookupSync = true;
                    ToCode = value.Code ?? value.IdText ?? "";
                    _suppressLookupSync = false;
                }
            }
        }

        private LookupItem? _selectedAirline;
        public LookupItem? SelectedAirline
        {
            get => _selectedAirline;
            set
            {
                if (SetProperty(ref _selectedAirline, value) && !_suppressLookupSync && value != null)
                {
                    if (value.IdInt > 0) IdHangBay = value.IdInt;
                }
            }
        }

        private LookupItem? _selectedAmenity;
        public LookupItem? SelectedAmenity
        {
            get => _selectedAmenity;
            set
            {
                if (SetProperty(ref _selectedAmenity, value) && !_suppressLookupSync && value != null)
                {
                    // BE nhận string (vd "1" hoặc "1,2,3"). Hiện UI hỗ trợ chọn 1.
                    IdTienNghi = value.IdText ?? (value.IdInt > 0 ? value.IdInt.ToString() : "1");
                }
            }
        }

        private bool _isLookupLoading;
        public bool IsLookupLoading
        {
            get => _isLookupLoading;
            set
            {
                if (SetProperty(ref _isLookupLoading, value))
                    OnPropertyChanged(nameof(IsLoadingLookups));
            }
        }
        public bool IsLoadingLookups => _isLookupLoading;

        private async Task LoadLookupsAsync()
        {
            // Best-effort: nếu BE không serve được json thì vẫn dùng fallback.
            try
            {
                IsLookupLoading = true;

                var airports = await TryLoadJsonArrayAsync(
                    staticJsonPath: "/Models/Config/data/SanBay.json",
                    convertEndpoint: "/api/Config/convert-sanbay-to-json",
                    parser: ParseAirports);

                var airlines = await TryLoadJsonArrayAsync(
                    staticJsonPath: "/Models/Config/data/HangBay.json",
                    convertEndpoint: "/api/Config/convert-hangBay-to-json",
                    parser: ParseIdTenList);

                var amenities = await TryLoadJsonArrayAsync(
                    staticJsonPath: "/Models/Config/data/TienNghi.json",
                    convertEndpoint: "/api/Config/convert-TienNghi-to-json",
                    parser: ParseIdTenList);

                // Apply
                Airports.Clear();
                foreach (var a in airports) Airports.Add(a);

                Airlines.Clear();
                foreach (var a in airlines) Airlines.Add(a);

                Amenities.Clear();
                foreach (var a in amenities) Amenities.Add(a);

                // Sync selections with current values
                _suppressLookupSync = true;

                SelectedFromAirport = Airports.FirstOrDefault(x => EqualsCode(x, FromCode));
                SelectedToAirport = Airports.FirstOrDefault(x => EqualsCode(x, ToCode));

                SelectedAirline = Airlines.FirstOrDefault(x => x.IdInt == IdHangBay) ?? Airlines.FirstOrDefault();
                if (SelectedAirline != null && SelectedAirline.IdInt > 0) IdHangBay = SelectedAirline.IdInt;

                SelectedAmenity = Amenities.FirstOrDefault(x => string.Equals(x.IdText, IdTienNghi, StringComparison.OrdinalIgnoreCase))
                    ?? Amenities.FirstOrDefault();
                if (SelectedAmenity != null)
                    IdTienNghi = SelectedAmenity.IdText ?? IdTienNghi;

                _suppressLookupSync = false;
            }
            catch
            {
                // ignore (fallback đã set trong parser)
            }
            finally
            {
                IsLookupLoading = false;
            }
        }

        private static bool EqualsCode(LookupItem item, string? code)
        {
            var c = (code ?? "").Trim().ToUpperInvariant();
            var v = (item.Code ?? item.IdText ?? "").Trim().ToUpperInvariant();
            return !string.IsNullOrWhiteSpace(c) && c == v;
        }

        private async Task<List<LookupItem>> TryLoadJsonArrayAsync(
            string staticJsonPath,
            string convertEndpoint,
            Func<JsonElement, List<LookupItem>> parser)
        {
            // 1) thử lấy thẳng JSON file (nếu BE có expose)
            var json = await TryGetStringAsync(staticJsonPath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var items = TryParse(json, parser);
                if (items.Count > 0) return items;
            }

            // 2) gọi convert endpoint (BE hiện trả message nhưng có thể generate file trên server)
            await TryCallConvertAsync(convertEndpoint);

            // 3) thử lại lần nữa
            json = await TryGetStringAsync(staticJsonPath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var items = TryParse(json, parser);
                if (items.Count > 0) return items;
            }

            // 4) fallback
            return parser(default);
        }

        private async Task TryCallConvertAsync(string endpoint)
        {
            try
            {
                using var req = _apiClient.CreateRequest(HttpMethod.Get, endpoint, false);
                _ = await _apiClient.Http.SendAsync(req);
            }
            catch
            {
                // ignore
            }
        }

        private async Task<string?> TryGetStringAsync(string path)
        {
            try
            {
                using var req = _apiClient.CreateRequest(HttpMethod.Get, path, false);
                var resp = await _apiClient.Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private static List<LookupItem> TryParse(string json, Func<JsonElement, List<LookupItem>> parser)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                    return parser(root);
            }
            catch { }
            return new List<LookupItem>();
        }

        private static List<LookupItem> ParseAirports(JsonElement arrOrDefault)
        {
            // Nếu parse thật
            if (arrOrDefault.ValueKind == JsonValueKind.Array)
            {
                var list = new List<LookupItem>();
                foreach (var it in arrOrDefault.EnumerateArray())
                {
                    // BE json: [{ "Ma": "SGN" }, ...]
                    var code = GetStringCI(it, "Ma", "ma", "code", "Code", "maSanBay", "MaSanBay");
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    list.Add(new LookupItem
                    {
                        Code = code.Trim().ToUpperInvariant(),
                        Name = code.Trim().ToUpperInvariant()
                    });
                }
                return list.OrderBy(x => x.Code).ToList();
            }

            // Fallback list (tối thiểu để UI dùng được ngay)
            return new List<LookupItem>
            {
                new LookupItem{ Code="SGN", Name="SGN" },
                new LookupItem{ Code="HAN", Name="HAN" },
                new LookupItem{ Code="DAD", Name="DAD" },
                new LookupItem{ Code="CXR", Name="CXR" },
                new LookupItem{ Code="PQC", Name="PQC" },
            };
        }

        private static List<LookupItem> ParseIdTenList(JsonElement arrOrDefault)
        {
            if (arrOrDefault.ValueKind == JsonValueKind.Array)
            {
                var list = new List<LookupItem>();
                foreach (var it in arrOrDefault.EnumerateArray())
                {
                    var idText = GetStringCI(it, "id", "Id");
                    var ten = GetStringCI(it, "ten", "Ten", "name", "Name");

                    if (string.IsNullOrWhiteSpace(idText)) continue;
                    var idInt = int.TryParse(idText, out var n) ? n : 0;

                    list.Add(new LookupItem
                    {
                        IdText = idText,
                        IdInt = idInt,
                        Name = ten ?? idText
                    });
                }
                return list.OrderBy(x => x.Name).ToList();
            }

            // fallback
            return new List<LookupItem>
            {
                new LookupItem{ IdText="1", IdInt=1, Name="(Mặc định)"},
            };
        }

        private static string? GetStringCI(JsonElement obj, params string[] keys)
        {
            if (obj.ValueKind != JsonValueKind.Object) return null;
            foreach (var p in obj.EnumerateObject())
            {
                foreach (var k in keys)
                {
                    if (string.Equals(p.Name, k, StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Value.ValueKind == JsonValueKind.String) return p.Value.GetString();
                        return p.Value.ToString();
                    }
                }
            }
            return null;
        }

        public class LookupItem
        {
            public string? IdText { get; set; }
            public int IdInt { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }

            public string Display
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(Code))
                        return Code!;
                    if (IdInt > 0 && !string.IsNullOrWhiteSpace(Name))
                        return $"{Name} (#{IdInt})";
                    return Name ?? IdText ?? "";
                }
            }
        }

        // ========== Inputs ==========
        private string _fromCode = "";
        public string FromCode
        {
            get => _fromCode;
            set
            {
                if (SetProperty(ref _fromCode, value))
                {
                    if (!_suppressLookupSync && Airports.Count > 0)
                    {
                        _suppressLookupSync = true;
                        SelectedFromAirport = Airports.FirstOrDefault(x => EqualsCode(x, _fromCode));
                        _suppressLookupSync = false;
                    }
                }
            }
        }

        private string _toCode = "";
        public string ToCode
        {
            get => _toCode;
            set
            {
                if (SetProperty(ref _toCode, value))
                {
                    if (!_suppressLookupSync && Airports.Count > 0)
                    {
                        _suppressLookupSync = true;
                        SelectedToAirport = Airports.FirstOrDefault(x => EqualsCode(x, _toCode));
                        _suppressLookupSync = false;
                    }
                }
            }
        }

        private DateTime _departureDate;
        public DateTime DepartureDate { get => _departureDate; set => SetProperty(ref _departureDate, value); }

        private decimal _priceMin;
        public decimal PriceMin { get => _priceMin; set => SetProperty(ref _priceMin, value); }

        private decimal _priceMax;
        public decimal PriceMax { get => _priceMax; set => SetProperty(ref _priceMax, value); }

        private string _idTienNghi = "1";
        public string IdTienNghi
        {
            get => _idTienNghi;
            set
            {
                if (SetProperty(ref _idTienNghi, value))
                {
                    if (!_suppressLookupSync && Amenities.Count > 0)
                    {
                        _suppressLookupSync = true;
                        SelectedAmenity = Amenities.FirstOrDefault(x => string.Equals(x.IdText, _idTienNghi, StringComparison.OrdinalIgnoreCase));
                        _suppressLookupSync = false;
                    }
                }
            }
        }

        private int _idLoaiVe = 1;
        public int IdLoaiVe { get => _idLoaiVe; set => SetProperty(ref _idLoaiVe, value); }

        private int _idHangBay = 1;
        public int IdHangBay
        {
            get => _idHangBay;
            set
            {
                if (SetProperty(ref _idHangBay, value))
                {
                    if (!_suppressLookupSync && Airlines.Count > 0)
                    {
                        _suppressLookupSync = true;
                        SelectedAirline = Airlines.FirstOrDefault(x => x.IdInt == _idHangBay);
                        _suppressLookupSync = false;
                    }
                }
            }
        }

        // --- Compatibility wrappers for existing XAML (Vietnamese names) ---
        public string MaSanBayDi { get => FromCode; set => FromCode = value; }
        public string MaSanBayDen { get => ToCode; set => ToCode = value; }
        public DateTime NgayDi { get => DepartureDate; set => DepartureDate = value; }
        public decimal GiaMin { get => PriceMin; set => PriceMin = value; }
        public decimal GiaMax { get => PriceMax; set => PriceMax = value; }

        public ObservableCollection<string> SortOptions { get; }

        private string? _selectedSort;
        public string? SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (SetProperty(ref _selectedSort, value))
                {
                    ApplySort();
                }
            }
        }

        // ========== Results ==========
        public ObservableCollection<FlightResultVm> FlightGroups { get; } = new();
        public IEnumerable<FlightResultVm> Results => FlightGroups;

        private LichBayItemVm? _selectedSchedule;
        public LichBayItemVm? SelectedSchedule
        {
            get => _selectedSchedule;
            set
            {
                if (SetProperty(ref _selectedSchedule, value))
                {
                    UpdateSelectedSummary();
                    ContinueCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _selectedSummary = "Chưa chọn lịch bay.";
        public string SelectedSummary { get => _selectedSummary; set => SetProperty(ref _selectedSummary, value); }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        public bool IsLoading { get => IsBusy; set => IsBusy = value; }

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

        public bool HasError => !string.IsNullOrWhiteSpace(Error);
        public bool HasStatus => !string.IsNullOrWhiteSpace(SelectedSummary);

        public bool ShowEmptyState => !IsBusy && (FlightGroups == null || FlightGroups.Count == 0);
        public bool NoResultsFound => ShowEmptyState && string.IsNullOrWhiteSpace(Error);

        // ========== Commands ==========
        public AsyncRelayCommand SearchCommand { get; }
        public RelayCommand SwapCommand { get; }
        public RelayCommand SelectScheduleCommand { get; }
        public RelayCommand ContinueCommand { get; }
        public RelayCommand SwapAirportsCommand => SwapCommand;

        private void Swap()
        {
            var tmp = FromCode;
            FromCode = ToCode;
            ToCode = tmp;
        }

        private async Task SearchAsync()
        {
            try
            {
                Error = "";
                IsBusy = true;

                FlightGroups.Clear();
                SelectedSchedule = null;

                var sort = SelectedSort switch
                {
                    "Ghế còn nhiều nhất" => "seats_desc",
                    "Giá tăng dần" => "price_asc",
                    "Giá giảm dần" => "price_desc",
                    "Giờ đi sớm nhất" => "time_asc",
                    "Giờ đi muộn nhất" => "time_desc",
                    _ => ""
                };

                var tienNghi = string.IsNullOrWhiteSpace(IdTienNghi) ? "1" : IdTienNghi.Trim();

                // ✅ request đúng format FlightApi đang đọc (camelCase)
                var req = new
                {
                    maSanBayDi = (FromCode ?? "").Trim(),
                    maSanBayDen = (ToCode ?? "").Trim(),
                    ngayDi = DepartureDate,
                    giaMin = PriceMin,
                    giaMax = PriceMax,
                    sort = sort,

                    idTienNghi = tienNghi,
                    idLoaiVe = IdLoaiVe,
                    idHangBay = IdHangBay
                };

                var data = await _flightApi.SearchFlightsAsync(req);

                if (data.HasValue && data.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var g in data.Value.EnumerateArray())
                    {
                        var airline = GetNestedStringAny(g,
                            new[] { "hangBay", "HangBay" },
                            new[] { "tenHang", "TenHang", "TenHangBay" }) ?? "";

                        var fromCode = GetNestedStringAny(g,
                            new[] { "sanBayDi", "SanBayDi" },
                            new[] { "maSanBayDi", "MaSanBayDi", "MaSanBay" }) ?? "";

                        var toCode = GetNestedStringAny(g,
                            new[] { "sanBayDen", "SanBayDen" },
                            new[] { "maSanBayDen", "MaSanBayDen", "MaSanBay" }) ?? "";

                        var fromName = GetNestedStringAny(g,
                            new[] { "sanBayDi", "SanBayDi" },
                            new[] { "ten", "Ten", "TenSanBay" }) ?? "";

                        var toName = GetNestedStringAny(g,
                            new[] { "sanBayDen", "SanBayDen" },
                            new[] { "ten", "Ten", "TenSanBay" }) ?? "";

                        var groupVm = new FlightResultVm
                        {
                            AirlineName = airline,
                            FromCode = fromCode,
                            ToCode = toCode,
                            FromName = fromName,
                            ToName = toName
                        };

                        if (!TryGetPropertyCI(g, "lichBay", out var lichBayArr) || lichBayArr.ValueKind != JsonValueKind.Array)
                        {
                            if (!TryGetPropertyCI(g, "LichBay", out lichBayArr) || lichBayArr.ValueKind != JsonValueKind.Array)
                                continue;
                        }

                        foreach (var lb in lichBayArr.EnumerateArray())
                        {
                            groupVm.Schedules.Add(new LichBayItemVm
                            {
                                Id = GetInt64Any(lb, new[] { "id", "Id" }),
                                IdTuyenBay = GetInt64Any(lb, new[] { "idTuyenBay", "IdTuyenBay" }),
                                HangBayName = airline,
                                MaSanBayDi = fromCode,
                                TenSanBayDi = fromName,
                                MaSanBayDen = toCode,
                                TenSanBayDen = toName,
                                GioDiLocal = GetDateTimeAny(lb, new[] { "thoiGianOsanBayDiUtc", "ThoiGianOsanBayDiUtc" }).ToLocalTime(),
                                GioDenLocal = GetDateTimeAny(lb, new[] { "thoiGianOsanBayDenUtc", "ThoiGianOsanBayDenUtc" }).ToLocalTime(),
                                ThoiGianBay = GetInt32Any(lb, new[] { "thoiGianBay", "ThoiGianBay" }),
                                Gia = GetDecimalAny(lb, new[] { "gia", "Gia" }),
                                SoLuongGhe = GetInt32Any(lb, new[] { "soLuongGhe", "SoLuongGhe" }),
                                TenTienIch = GetStringListAny(lb, new[] { "tenTienIch", "TenTienIch" })
                            });
                        }

                        if (groupVm.Schedules.Count > 0)
                            FlightGroups.Add(groupVm);
                    }
                }

                ApplySort();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(NoResultsFound));
            }
        }

        private void SelectSchedule(LichBayItemVm? item)
        {
            if (item == null) return;

            foreach (var g in FlightGroups)
                foreach (var s in g.Schedules)
                    s.IsSelected = false;

            item.IsSelected = true;
            SelectedSchedule = item;

            Continue();
        }

        private void Continue()
        {
            _onContinue?.Invoke(SelectedSchedule);
        }

        private void ApplySort()
        {
            foreach (var g in FlightGroups)
            {
                IEnumerable<LichBayItemVm> sorted = g.Schedules;

                sorted = SelectedSort switch
                {
                    "Ghế còn nhiều nhất" => g.Schedules.OrderByDescending(x => x.SoLuongGhe),
                    "Giá tăng dần" => g.Schedules.OrderBy(x => x.Gia),
                    "Giá giảm dần" => g.Schedules.OrderByDescending(x => x.Gia),
                    // ✅ FIX: Sắp xếp theo giờ hiển thị (HH:mm) thay vì DateTime object
                    "Giờ đi sớm nhất" => g.Schedules.OrderBy(x => x.GioDi),
                    "Giờ đi muộn nhất" => g.Schedules.OrderByDescending(x => x.GioDi),
                    _ => g.Schedules
                };

                g.Schedules = new ObservableCollection<LichBayItemVm>(sorted);
                OnPropertyChanged(nameof(FlightGroups));
            }

            OnPropertyChanged(nameof(Results));
            OnPropertyChanged(nameof(ShowEmptyState));
        }

        private void UpdateSelectedSummary()
        {
            if (SelectedSchedule == null)
            {
                SelectedSummary = "Chưa chọn lịch bay.";
                return;
            }

            SelectedSummary =
                $"{SelectedSchedule.HangBayName} • {SelectedSchedule.MaSanBayDi} → {SelectedSchedule.MaSanBayDen} • " +
                $"{SelectedSchedule.GioDiLocal:HH:mm}–{SelectedSchedule.GioDenLocal:HH:mm} • {SelectedSchedule.Gia:N0} đ";
        }

        // ===== JSON helpers (case-insensitive + multi-key) =====
        private static bool TryGetPropertyCI(JsonElement obj, string name, out JsonElement value)
        {
            value = default;
            if (obj.ValueKind != JsonValueKind.Object) return false;

            foreach (var p in obj.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            return false;
        }

        private static string? GetNestedStringAny(JsonElement obj, string[] parentKeys, string[] childKeys)
        {
            foreach (var pk in parentKeys)
            {
                if (!TryGetPropertyCI(obj, pk, out var parent) || parent.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var ck in childKeys)
                {
                    if (TryGetPropertyCI(parent, ck, out var child))
                    {
                        if (child.ValueKind == JsonValueKind.String) return child.GetString();
                        return child.ToString();
                    }
                }
            }
            return null;
        }

        private static long GetInt64Any(JsonElement obj, string[] keys)
        {
            foreach (var k in keys)
            {
                if (TryGetPropertyCI(obj, k, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n)) return n;
                    if (v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), out var s)) return s;
                }
            }
            return 0;
        }

        private static int GetInt32Any(JsonElement obj, string[] keys)
        {
            foreach (var k in keys)
            {
                if (TryGetPropertyCI(obj, k, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return n;
                    if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var s)) return s;
                }
            }
            return 0;
        }

        private static decimal GetDecimalAny(JsonElement obj, string[] keys)
        {
            foreach (var k in keys)
            {
                if (TryGetPropertyCI(obj, k, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var n)) return n;
                    if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var s)) return s;
                }
            }
            return 0;
        }

        private static DateTime GetDateTimeAny(JsonElement obj, string[] keys)
        {
            foreach (var k in keys)
            {
                if (TryGetPropertyCI(obj, k, out var v))
                {
                    if (v.ValueKind == JsonValueKind.String && DateTime.TryParse(v.GetString(), out var dt)) return dt;
                    try { return v.GetDateTime(); } catch { }
                }
            }
            return DateTime.Now;
        }

        private static List<string> GetStringListAny(JsonElement obj, string[] keys)
        {
            foreach (var k in keys)
            {
                if (TryGetPropertyCI(obj, k, out var v) && v.ValueKind == JsonValueKind.Array)
                {
                    return v.EnumerateArray()
                        .Select(x => x.GetString() ?? x.ToString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                }
            }
            return new List<string>();
        }

        // ===== VMs =====
        public class FlightResultVm : ObservableObject
        {
            public string AirlineName { get; set; } = "";
            public string FromCode { get; set; } = "";
            public string ToCode { get; set; } = "";
            public string FromName { get; set; } = "";
            public string ToName { get; set; } = "";

            public string Header => string.IsNullOrWhiteSpace(AirlineName)
                ? $"{FromCode} → {ToCode}"
                : $"{AirlineName} • {FromCode} → {ToCode}";

            private ObservableCollection<LichBayItemVm> _schedules = new();
            public ObservableCollection<LichBayItemVm> Schedules
            {
                get => _schedules;
                set => SetProperty(ref _schedules, value);
            }
        }

        public class LichBayItemVm : ObservableObject
        {
            public long Id { get; set; }
            public long IdTuyenBay { get; set; }

            public string HangBayName { get; set; } = "";
            public string MaSanBayDi { get; set; } = "";
            public string TenSanBayDi { get; set; } = "";
            public string MaSanBayDen { get; set; } = "";
            public string TenSanBayDen { get; set; } = "";

            public DateTime GioDiLocal { get; set; }
            public DateTime GioDenLocal { get; set; }

            public int ThoiGianBay { get; set; }
            public decimal Gia { get; set; }
            public int SoLuongGhe { get; set; }

            public List<string> TenTienIch { get; set; } = new();

            public string GioDi => GioDiLocal.ToString("HH:mm");
            public string GioDen => GioDenLocal.ToString("HH:mm");
            public string TienIchText => (TenTienIch == null || TenTienIch.Count == 0)
                ? ""
                : string.Join(" · ", TenTienIch.Where(x => !string.IsNullOrWhiteSpace(x)));

            public string DisplayTime => $"{GioDi} — {GioDen}";
            public string DisplayPrice => $"{Gia:N0} đ";

            private bool _isSelected;
            public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        }
    }
}