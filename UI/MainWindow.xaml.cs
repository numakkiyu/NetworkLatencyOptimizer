using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using NetworkLatencyOptimizer.Core;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using NetworkLatencyOptimizer.Core.Models;
using LiveCharts.Wpf;
using LiveCharts.WinForms;
using System.Windows.Forms;

namespace NetworkLatencyOptimizer.UI
{
    public partial class MainWindow : Window
    {
        private Process monitorProcess;
        private DispatcherTimer updateTimer;
        private Config config;
        private bool isRunning;
        private FileSystemWatcher logWatcher;
        private ObservableCollection<MinecraftServer> servers;
        private MinecraftServer selectedServer;
        private LiveCharts.WinForms.CartesianChart latencyChart;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            SetupTimer();
            SetupLogWatcher();
            UpdateUI();
            SetupServerList();
            SetupLatencyChart();
            AddServerButton.Click += AddServerButton_Click;
        }

        private void LoadConfig()
        {
            config = ConfigManager.LoadConfig();
        }

        private void SetupTimer()
        {
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void SetupLogWatcher()
        {
            string logDir = Path.GetDirectoryName("Logs/optimizer.log");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            logWatcher = new FileSystemWatcher(logDir);
            logWatcher.Filter = "*.log";
            logWatcher.Changed += LogWatcher_Changed;
            logWatcher.EnableRaisingEvents = true;
        }

        private void LogWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith("optimizer.log"))
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        string[] lastLines = File.ReadAllLines(e.FullPath);
                        if (lastLines.Length > 0)
                        {
                            LogTextBox.AppendText(lastLines[lastLines.Length - 1] + Environment.NewLine);
                            LogTextBox.ScrollToEnd();
                        }
                    }
                    catch (IOException)
                    {
                        // 文件可能正在被写入，忽略此错误
                    }
                });
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsUserAdministrator())
                {
                    MessageBox.Show("请以管理员身份运行此程序。", "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StartButton.IsEnabled = false;
                StatusText.Text = "状态: 正在检查网络环境...";

                // 检查网络环境
                var checker = new NetworkEnvironmentChecker();
                var envStatus = await checker.CheckEnvironment();

                if (envStatus.HasIssues)
                {
                    var result = MessageBox.Show(
                        $"检测到以下网络问题:\n{string.Join("\n", envStatus.Issues)}\n\n是否仍要继续优化？",
                        "网络环境检查",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        StartButton.IsEnabled = true;
                        StatusText.Text = "状态: 已取消";
                        return;
                    }
                }

                if (envStatus.HasWarnings)
                {
                    Logger.Log("网络环境警告:\n" + string.Join("\n", envStatus.Warnings), LogLevel.Warning);
                }

                StatusText.Text = "状态: 正在启动优化...";

                await Task.Run(() =>
                {
                    // 启动优化
                    if (config.EnableTcpOptimization)
                    {
                        var tcpOptimizer = new TcpOptimizer();
                        tcpOptimizer.ApplyTcpOptimizations().Wait();
                    }

                    if (config.EnableRouteOptimization)
                    {
                        var routeOptimizer = new RouteOptimizer(config.HypixelIP);
                        routeOptimizer.OptimizeRoute().Wait();
                    }

                    if (config.EnableDnsOptimization)
                    {
                        var dnsOptimizer = new DnsOptimizer();
                        dnsOptimizer.OptimizeDns(config.DNSServers).Wait();
                    }
                });

                // 启动监控进程
                StartMonitorProcess();

                isRunning = true;
                StopButton.IsEnabled = true;
                StatusText.Text = "状态: 运行中";
                Logger.Log("优化器已启动");

                // 更新UI显示当前延迟
                UpdateLatencyChart(envStatus.Latency);
                LatencyText.Text = $"当前延迟: {envStatus.Latency:F1}ms";
                PacketLossText.Text = $"丢包率: {envStatus.PacketLoss:F1}%";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StartButton.IsEnabled = true;
                StatusText.Text = "状态: 启动失败";
                Logger.Log($"启动失败: {ex.Message}", LogLevel.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopMonitorProcess();
                
                // 还原网络设置
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-File Scripts/optimize.ps1 -Revert",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }).WaitForExit();

                isRunning = false;
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                StatusText.Text = "状态: 已停止";
                Logger.Log("优化器已停止");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Log($"停止失败: {ex.Message}", LogLevel.Error);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(config);
            if (settingsWindow.ShowDialog() == true)
            {
                config = settingsWindow.UpdatedConfig;
                ConfigManager.SaveConfig(config);
                Logger.Log("配置已更新");
            }
        }

        private void StartMonitorProcess()
        {
            if (monitorProcess != null && !monitorProcess.HasExited)
            {
                return;
            }

            monitorProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "Monitor/monitor.py",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            monitorProcess.Start();
        }

        private void StopMonitorProcess()
        {
            if (monitorProcess != null && !monitorProcess.HasExited)
            {
                monitorProcess.Kill();
                monitorProcess = null;
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (isRunning)
            {
                try
                {
                    // 更新状态信息
                    string logFile = "Logs/optimizer.log";
                    if (File.Exists(logFile))
                    {
                        var lastLines = File.ReadAllLines(logFile);
                        foreach (var line in lastLines.Reverse())
                        {
                            if (line.Contains("当前延迟:"))
                            {
                                LatencyText.Text = line.Split(new[] { "当前延迟:" }, StringSplitOptions.None)[1].Trim();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"更新UI失败: {ex.Message}", LogLevel.Error);
                }
            }
        }

        private bool IsUserAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (isRunning)
            {
                var result = MessageBox.Show(
                    "优化器正在运行中，是否停止优化器并退出？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StopButton_Click(null, null);
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }

            updateTimer.Stop();
            logWatcher.Dispose();
            base.OnClosing(e);
        }

        private void SetupServerList()
        {
            servers = new ObservableCollection<MinecraftServer>();
            ServerListBox.ItemsSource = servers;
        }

        private void SetupLatencyChart()
        {
            latencyChart = new LiveCharts.WinForms.CartesianChart
            {
                Series = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "延迟",
                        Values = new ChartValues<double>()
                    }
                },
                AxisX = new AxesCollection
                {
                    new Axis
                    {
                        Title = "时间",
                        LabelFormatter = value => DateTime.Now.AddSeconds(-30 + value).ToString("HH:mm:ss")
                    }
                },
                AxisY = new AxesCollection
                {
                    new Axis
                    {
                        Title = "延迟(ms)",
                        MinValue = 0
                    }
                }
            };

            var host = new WindowsFormsHost();
            host.Child = latencyChart;
            LatencyChart.Children.Add(host);
        }

        private void AddServerButton_Click(object sender, RoutedEventArgs e)
        {
            var addServerWindow = new AddServerWindow();
            if (addServerWindow.ShowDialog() == true)
            {
                servers.Add(addServerWindow.NewServer);
                Logger.Log($"已添加服务器: {addServerWindow.NewServer.Name}");
            }
        }

        private void ServerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedServer = ServerListBox.SelectedItem as MinecraftServer;
            if (selectedServer != null)
            {
                // 更新UI显示选中服务器的信息
                UpdateServerInfo();
            }
        }

        private void UpdateServerInfo()
        {
            if (selectedServer != null)
            {
                StatusText.Text = selectedServer.IsOptimized ? "已优化" : "未优化";
                LatencyText.Text = $"{selectedServer.CurrentLatency}ms";
                // 更新其他信息...
            }
        }

        private void UpdateLatencyChart(double latency)
        {
            var series = latencyChart.Series[0] as LiveCharts.Wpf.LineSeries;
            if (series != null)
            {
                if (series.Values.Count > 30)
                {
                    series.Values.RemoveAt(0);
                }
                series.Values.Add(latency);
            }
        }
    }
} 