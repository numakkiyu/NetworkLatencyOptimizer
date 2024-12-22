using System;
using System.Windows;
using System.Threading.Tasks;

namespace NetworkLatencyOptimizer.UI
{
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // 显示启动画面
                var splashWindow = new SplashWindow();
                splashWindow.Show();

                // 执行初始化
                await splashWindow.LoadAsync();

                // 创建并显示主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // 关闭启动画面
                splashWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 恢复网络状态
                if (!NetworkStateManager.Instance.RestoreOriginalState())
                {
                    MessageBox.Show("网络状态恢复失败，请检查网络设置", "警告", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"程序退出时发生错误: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
} 