using System;
using System.Windows;
using System.Threading.Tasks;

namespace NetworkLatencyOptimizer.UI
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public async Task LoadAsync()
        {
            try
            {
                // 检查更新
                StatusText.Text = "正在检查更新...";
                await Task.Delay(1000); // 模拟检查更新

                // 初始化配置
                StatusText.Text = "正在加载配置...";
                await Task.Delay(500);

                // 检查系统要求
                StatusText.Text = "正在检查系统要求...";
                await Task.Delay(500);

                // 初始化网络组件
                StatusText.Text = "正在初始化网络组件...";
                await Task.Delay(1000);

                // 完成
                StatusText.Text = "启动完成";
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
    }
} 