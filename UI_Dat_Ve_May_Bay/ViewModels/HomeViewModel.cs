using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Core;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class HomeViewModel : ObservableObject
    {
        private readonly ApiClient _apiClient;

        // Vé (vòng đời)
        private const string EP_HUY_DAT_VE = "/api/ChuyenBay/HuyDatVe"; // POST (query: idDatVe, lyDoHuy)
        private const string EP_CHECKIN = "/api/ChuyenBay/CheckIn";    // POST (query: id)

        // ===== User Header =====
        private string _greeting = "Hi there!";
        public string Greeting { get => _greeting; set => SetProperty(ref _greeting, value); }
        
        private string _question = "Chào bạn, hôm nay bạn muốn đi đâu?";
        public string Question { get => _question; set => SetProperty(ref _question, value); }

        public ObservableCollection<HomeServiceVm> Services { get; } = new();
        public ObservableCollection<HomeHotelVm> TopHotels { get; } = new();
        public ObservableCollection<HomeFlightVm> FeaturedFlights { get; } = new();

        // ===== Vé của tôi (Tra cứu / Huỷ / Check-in) =====
        private string _bookingIdText = "";
        public string BookingIdText { get => _bookingIdText; set => SetProperty(ref _bookingIdText, value); }

        private string _cancelReason = "";
        public string CancelReason { get => _cancelReason; set => SetProperty(ref _cancelReason, value); }

        private string _checkInIdText = "";
        public string CheckInIdText { get => _checkInIdText; set => SetProperty(ref _checkInIdText, value); }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private string _ticketInfo = "";
        public string TicketInfo { get => _ticketInfo; set => SetProperty(ref _ticketInfo, value); }

        private string _ticketError = "";
        public string TicketError { get => _ticketError; set => SetProperty(ref _ticketError, value); }

        public AsyncRelayCommand CancelBookingCommand { get; }
        public AsyncRelayCommand CheckInCommand { get; }
        
        public RelayCommand SelectServiceCommand { get; }
        public RelayCommand SelectHotelCommand { get; }
        public RelayCommand ViewAllHotelsCommand { get; }
        public RelayCommand InfoBoxCommand { get; } // Helper for generic messages

        public HomeViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
            CancelBookingCommand = new AsyncRelayCommand(CancelBookingAsync);
            CheckInCommand = new AsyncRelayCommand(CheckInAsync);

            SelectServiceCommand = new RelayCommand(obj => SelectService(obj as HomeServiceVm));
            SelectHotelCommand = new RelayCommand(obj => SelectHotel(obj as HomeHotelVm));
            ViewAllHotelsCommand = new RelayCommand(() => TicketInfo = "Tính năng xem tất cả khách sạn đang được phát triển.");
            InfoBoxCommand = new RelayCommand(msg => TicketInfo = msg?.ToString() ?? "");

            // Tải dữ liệu trang chủ
            _ = LoadHomeDataAsync();
        }

        private async Task LoadHomeDataAsync()
        {
            try
            {
                IsBusy = true;

                // 1. Lấy thông tin người dùng (Greeting)
                await LoadUserInfoAsync();

                // 2. Lấy danh sách dịch vụ (Categories)
                await LoadServicesAsync();

                // 3. Lấy danh sách khách sạn tiêu biểu
                await LoadHotelsAsync();

                // 4. Lấy danh sách chuyến bay tiêu biểu (mock từ search)
                await LoadFeaturedFlightsAsync();
            }
            catch (Exception ex)
            {
                TicketError = $"Lỗi tải dữ liệu trang chủ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadUserInfoAsync()
        {
            try
            {
                var doc = await SendJsonAsync(HttpMethod.Post, "/api/KhachHang/ThongTinCoBan", null);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var d) || root.TryGetProperty("Data", out d))
                {
                    var name = d.TryGetProperty("TenKh", out var n) ? n.GetString() : "người dùng";
                    Greeting = $"Hi, {name?.ToUpperInvariant()}";
                    Question = "Dự định hôm nay của bạn là gì?";
                }
            }
            catch { Greeting = "Hi, Khách hàng"; }
        }

        private async Task LoadServicesAsync()
        {
            try
            {
                var doc = await SendJsonAsync(HttpMethod.Get, "/api/DichVu/LayDanhSachDichVu", null);
                var root = doc.RootElement;
                // Giả định trả về mảng trực tiếp hoặc data: []
                var arr = root.ValueKind == JsonValueKind.Array ? root :
                          ((root.TryGetProperty("data", out var d) || root.TryGetProperty("Data", out d)) ? d : default);

                Services.Clear();
                if (arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        Services.Add(new HomeServiceVm
                        {
                            Id = item.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                            Name = item.TryGetProperty("tenDichVu", out var t) ? t.GetString() ?? "" : ""
                        });
                    }
                }
                
                // Fallback nếu rỗng
                if (Services.Count == 0)
                {
                    Services.Add(new HomeServiceVm { Name = "Vé máy bay", Icon = "✈" });
                    Services.Add(new HomeServiceVm { Name = "Khách sạn", Icon = "🏨" });
                    Services.Add(new HomeServiceVm { Name = "Xe", Icon = "🚗" });
                }
            }
            catch 
            {
                 Services.Add(new HomeServiceVm { Name = "Vé máy bay", Icon = "✈" });
                 Services.Add(new HomeServiceVm { Name = "Khách sạn", Icon = "🏨" });
                 Services.Add(new HomeServiceVm { Name = "Xe", Icon = "🚗" });
            }
        }

        private async Task LoadHotelsAsync()
        {
            try
            {
                var doc = await SendJsonAsync(HttpMethod.Get, "/api/Hotel/GetAllHotel", null);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var d) || root.TryGetProperty("Data", out d))
                {
                    TopHotels.Clear();
                    foreach (var item in d.EnumerateArray())
                    {
                        TopHotels.Add(new HomeHotelVm
                        {
                            Name = item.TryGetProperty("tenPhong", out var t) ? t.GetString() ?? "" : "",
                            Location = "Đang cập nhật", // BE chưa trả location chi tiết
                            PriceText = item.TryGetProperty("gia", out var g) ? $"{g.GetDecimal():N0}đ/đêm" : "Liên hệ",
                            ImageUrl = item.TryGetProperty("hinh", out var h) ? h.GetString() ?? "" : ""
                        });
                        if (TopHotels.Count >= 4) break; 
                    }
                }
            }
            catch { /* fallback logic in XAML if empty */ }
        }

        private void SelectService(HomeServiceVm? service)
        {
            if (service == null) return;
            
            if (service.Name.Contains("Vé máy bay") || service.Name.Contains("Flight"))
            {
                TicketInfo = "Đang chuyển trang Tìm vé máy bay...";
                Question = "Tìm kiếm chuyến bay tốt nhất cho bạn!";
                
                // Real navigation hack for WPF
                try
                {
                    var mainVm = System.Windows.Application.Current.MainWindow.DataContext as MainViewModel;
                    if (mainVm?.GoFlightCommand != null && mainVm.GoFlightCommand.CanExecute(null))
                    {
                        mainVm.GoFlightCommand.Execute(null);
                    }
                }
                catch { /* fallback if UI hierarchy differs */ }
            }
            else
            {
                TicketInfo = $"Dịch vụ '{service.Name}' đang được kết nối với Backend. Vui lòng quay lại sau!";
                Question = $"Dịch vụ {service.Name} sẽ sớm ra mắt.";
            }
        }

        private void SelectHotel(HomeHotelVm? hotel)
        {
            if (hotel == null) return;
            
            // Show more details in the UI
            TicketInfo = $"Khách sạn: {hotel.Name} | Giá: {hotel.PriceText}. Backend đã sẵn sàng (Endpoint: /api/Hotel/DatHotel).";
            Question = $"Bạn có muốn đặt {hotel.Name}?";
            
            // We could navigate to a detail page if it existed, 
            // but for now, we provide the specific info that backend is integrated.
        }

        private async Task LoadFeaturedFlightsAsync()
        {
            // Tạm thời hardcode vài chuyến hoặc fetch từ search chung
            FeaturedFlights.Clear();
            FeaturedFlights.Add(new HomeFlightVm { Airline = "Vietnam Airlines", Route = "HAN → SGN", Time = "08:30 - 10:40", Price = "1.500.000đ" });
            FeaturedFlights.Add(new HomeFlightVm { Airline = "Bamboo Airways", Route = "DAD → SGN", Time = "13:00 - 14:25", Price = "1.200.000đ" });
            FeaturedFlights.Add(new HomeFlightVm { Airline = "Vietjet Air", Route = "SGN → PQC", Time = "09:15 - 10:15", Price = "980.000đ" });
            await Task.CompletedTask;
        }

        private async Task<JsonDocument> SendJsonAsync(HttpMethod method, string path, object? body)
        {
            var req = body == null
                ? _apiClient.CreateRequest(method, path, true)
                : _apiClient.CreateJsonRequest(method, path, body, true);

            var resp = await _apiClient.Http.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(text)) return JsonDocument.Parse("{}");
            return JsonDocument.Parse(text);
        }

        // Models
        public class HomeServiceVm
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Icon { get; set; } = "⭐";
        }

        public class HomeHotelVm
        {
            public string Name { get; set; } = "";
            public string Location { get; set; } = "";
            public string PriceText { get; set; } = "";
            public string ImageUrl { get; set; } = "";
        }

        public class HomeFlightVm
        {
            public string Airline { get; set; } = "";
            public string Route { get; set; } = "";
            public string Time { get; set; } = "";
            public string Price { get; set; } = "";
        }

        private async Task CancelBookingAsync()
        {
            try
            {
                TicketError = "";
                TicketInfo = "";
                IsBusy = true;

                if (!long.TryParse((BookingIdText ?? string.Empty).Trim(), out var idDatVe) || idDatVe <= 0)
                {
                    TicketInfo = "Nhập Mã đặt vé (idDatVe) hợp lệ trước.";
                    return;
                }

                var reason = (CancelReason ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(reason))
                    reason = "Huỷ theo yêu cầu";

                var url = $"{EP_HUY_DAT_VE}?idDatVe={idDatVe}&lyDoHuy={Uri.EscapeDataString(reason)}";
                var raw = await SendAndReadAsync(HttpMethod.Post, url, new { });

                var msg = TryExtractMessage(raw);
                TicketInfo = string.IsNullOrWhiteSpace(msg) ? "Đã gửi yêu cầu huỷ vé." : msg;
            }
            catch (Exception ex)
            {
                TicketError = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CheckInAsync()
        {
            try
            {
                TicketError = "";
                TicketInfo = "";
                IsBusy = true;

                var id = (CheckInIdText ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id))
                {
                    TicketInfo = "Nhập id để check-in trước.";
                    return;
                }

                var url = $"{EP_CHECKIN}?id={Uri.EscapeDataString(id)}";
                var raw = await SendAndReadAsync(HttpMethod.Post, url, new { });

                var msg = TryExtractMessage(raw);
                TicketInfo = string.IsNullOrWhiteSpace(msg) ? "Đã gửi yêu cầu check-in." : msg;
            }
            catch (Exception ex)
            {
                TicketError = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<string> SendAndReadAsync(HttpMethod method, string url, object? body)
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
                    msg = $"{(int)resp.StatusCode} {resp.ReasonPhrase}";
                throw new Exception(msg);
            }

            return string.IsNullOrWhiteSpace(content) ? "{}" : content;
        }

        private static string? TryExtractMessage(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("message", out var m) &&
                    m.ValueKind == JsonValueKind.String)
                    return m.GetString();
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}