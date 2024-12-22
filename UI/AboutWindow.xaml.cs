using System;
using System.Windows;
using System.Management;
using System.Threading.Tasks;

namespace NetworkLatencyOptimizer.UI
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadSystemInfo();
            CheckForUpdates();
        }

        private void LoadSystemInfo()
        {
            try
            {
                // 获取操作系统信息
                var os = Environment.OSVersion;
                AddSystemInfoItem("操作系统", $"{os.VersionString} ({(Environment.Is64BitOperatingSystem ? "64位" : "32位")})");

                // 获取CPU信息
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        AddSystemInfoItem("处理器", obj["Name"].ToString());
                        break;
                    }
                }

                // 获取内存信息
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var totalMemory = Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                        AddSystemInfoItem("内存", $"{totalMemory:F1} GB");
                        break;
                    }
                }

                // 获取网络适配器信息
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        AddSystemInfoItem("网络适配器", obj["Name"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                AddSystemInfoItem("错误", $"获取系统信息失败: {ex.Message}");
            }
        }

        private void AddSystemInfoItem(string label, string value)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            panel.Children.Add(new TextBlock { Text = label + ": ", FontWeight = FontWeights.Bold, Width = 100 });
            panel.Children.Add(new TextBlock { Text = value });
            SystemInfoPanel.Children.Add(panel);
        }

        private async void CheckForUpdates()
        {
            try
            {
                UpdateStatusText.Text = "正在检查更新...";
                await CheckUpdateFromServer();
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"检查更新失败: {ex.Message}";
            }
        }

        private async Task CheckUpdateFromServer()
        {
            // TODO: 实现实际的更新检查逻辑
            await Task.Delay(2000); // 模拟网络请求
            UpdateStatusText.Text = "当前已是最新版本";
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                await CheckUpdateFromServer();
                button.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 