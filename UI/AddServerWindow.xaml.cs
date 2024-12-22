using System;
using System.Windows;
using NetworkLatencyOptimizer.Core.Models;

namespace NetworkLatencyOptimizer.UI
{
    public partial class AddServerWindow : Window
    {
        public MinecraftServer NewServer { get; private set; }

        public AddServerWindow()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ServerNameTextBox.Text))
            {
                MessageBox.Show("请输入服务器名称", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ServerAddressTextBox.Text))
            {
                MessageBox.Show("请输入服务器地址", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ServerPortTextBox.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号(1-65535)", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewServer = new MinecraftServer
            {
                Name = ServerNameTextBox.Text,
                Address = ServerAddressTextBox.Text,
                Port = port,
                IsOptimized = AutoOptimizeCheckBox.IsChecked ?? false
            };

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