using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Services;
using UI_Dat_Ve_May_Bay.Utils;
using UI_Dat_Ve_May_Bay.ViewModels;

namespace UI_Dat_Ve_May_Bay
{
    public partial class App : Application
    {
        private static readonly string LogDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "UI_Dat_Ve_May_Bay", "Logs");

        private static readonly string FirstChanceLogPath =
            Path.Combine(LogDir, "UI_FirstChance.log");

        private static readonly string FatalLogPath =
            Path.Combine(LogDir, "UI_DatVe_FATAL.log");

        public App()
        {
            // đảm bảo có folder log
            TryEnsureLogDir();

            // log first-chance (nơi THROW thật sự, dù bị catch)
            InstallFirstChanceLogger();

            // bắt lỗi sớm nhất khi load App.xaml resources
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                WriteFatal(ex, "AppCtor.InitializeComponent");
                MessageBox.Show(
                    $"App ctor crash:\n{ex}\n\nLog folder:\n{LogDir}",
                    "App crash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Environment.Exit(1);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // global handlers (nếu class này của bạn)
            try { ExceptionLogger.InstallGlobalExceptionHandlers(); }
            catch (Exception ex) { WriteFatal(ex, "InstallGlobalExceptionHandlers"); }

            try
            {
                base.OnStartup(e);

                var apiClient = new ApiClient
                {
                    BaseUrl = "https://audrina-subultimate-ghostily.ngrok-free.dev"
                };

                // nếu nghi thằng này gây throw -> comment tạm để test
                apiClient.ApplyBaseUrl();

                var tokenStore = new TokenStore();
                var mainVm = new MainViewModel(apiClient, tokenStore);

                var mainWindow = new MainWindow { DataContext = mainVm };
                MainWindow = mainWindow;
                mainWindow.Show();

                // hiện chỗ lưu log để bạn khỏi hỏi nữa :))
                // MessageBox.Show($"Log folder:\n{LogDir}", "Logs", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WriteFatal(ex, "OnStartup");
                MessageBox.Show(
                    $"Startup exception:\n{ex}\n\nLog folder:\n{LogDir}",
                    "Startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }

        private static void InstallFirstChanceLogger()
        {
            try
            {
                TryEnsureLogDir();
                File.WriteAllText(FirstChanceLogPath,
                    $"--- FirstChance log started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\n");

                AppDomain.CurrentDomain.FirstChanceException += (s, ev) =>
                {
                    try
                    {
                        // chỉ log System.Exception do code bạn throw (đỡ spam)
                        if (ev.Exception.GetType() != typeof(Exception)) return;

                        var st = new StackTrace(ev.Exception, true).ToString();
                        if (!st.Contains("UI_Dat_Ve_May_Bay")) return;

                        File.AppendAllText(
                            FirstChanceLogPath,
                            $"[{DateTime.Now:HH:mm:ss}] {ev.Exception.GetType().FullName}: {ev.Exception.Message}\n{st}\n-----------------\n"
                        );

                        // Bonus: đẩy ra Output window để bạn thấy ngay trong VS
                        Debug.WriteLine($"[FirstChance] {ev.Exception.Message}\n{st}");
                    }
                    catch { }
                };
            }
            catch { }
        }

        private static void WriteFatal(Exception ex, string stage)
        {
            try
            {
                TryEnsureLogDir();
                File.AppendAllText(
                    FatalLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Stage={stage}\n{ex}\n----------------------\n");
            }
            catch { }
        }

        private static void TryEnsureLogDir()
        {
            try { Directory.CreateDirectory(LogDir); }
            catch { /* ignore */ }
        }
    }
}