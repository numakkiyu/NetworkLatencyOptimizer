using System;
using System.Linq;
using System.Windows;
using NetworkLatencyOptimizer.Core;

namespace NetworkLatencyOptimizer.UI
{
    public partial class SettingsWindow : Window
    {
        public Config UpdatedConfig { get; private set; }
        private Config originalConfig;

        public string DNSServersText
        {
            get => string.Join(",", originalConfig.DNSServers);
            set => originalConfig.DNSServers = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(s => s.Trim())
                                                   .ToArray();
        }

        public SettingsWindow(Config config)
        {
            InitializeComponent();
            originalConfig = config;
            UpdatedConfig = new Config
            {
                HypixelIP = config.HypixelIP,
                DNSServers = config.DNSServers.ToArray(),
                LatencyThreshold = config.LatencyThreshold,
                EnableTcpOptimization = config.EnableTcpOptimization,
                EnableRouteOptimization = config.EnableRouteOptimization,
                EnableDnsOptimization = config.EnableDnsOptimization
            };

            DataContext = UpdatedConfig;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(UpdatedConfig.HypixelIP))
            {
                MessageBox.Show("请输入有效的服务器IP地址", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UpdatedConfig.LatencyThreshold <= 0)
            {
                MessageBox.Show("延迟阈值必须大于0", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UpdatedConfig.DNSServers == null || UpdatedConfig.DNSServers.Length == 0)
            {
                MessageBox.Show("请至少输入一个DNS服务器地址", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 