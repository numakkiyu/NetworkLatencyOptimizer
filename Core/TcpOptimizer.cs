using System;
using System.Diagnostics;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NetworkLatencyOptimizer.Core
{
    public class TcpOptimizer
    {
        // TCP优化参数
        private readonly Dictionary<string, int> TcpParams = new Dictionary<string, int>
        {
            { "TcpAckFrequency", 1 },           // 立即发送ACK
            { "TCPNoDelay", 1 },                // 禁用Nagle算法
            { "GlobalMaxTcpWindowSize", 65535 }, // 最大TCP窗口大小
            { "TcpWindowSize", 65535 },         // TCP窗口大小
            { "DefaultTTL", 64 },               // TTL值
            { "SackOpts", 1 },                  // 启用选择性确认
            { "Tcp1323Opts", 3 },               // 启用时间戳和窗口扩大
            { "TcpMaxDupAcks", 2 },             // 快速重传阈值
            { "MaxUserPort", 65534 },           // 最大用户端口数
            { "TcpTimedWaitDelay", 30 }         // TIME_WAIT状态超时时间
        };

        public async Task<bool> ApplyTcpOptimizations()
        {
            try
            {
                // 备份当前网络状态
                NetworkStateManager.Instance.BackupCurrentState();

                // 获取当前网络状态
                var networkStatus = await GetNetworkStatus();
                Logger.Log($"当前网络状态: 延迟={networkStatus.Latency}ms, 丢包率={networkStatus.PacketLoss}%");

                // 根据网络状态调整参数
                AdjustTcpParams(networkStatus);

                // 应用TCP参数
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (key != null)
                    {
                        foreach (var param in TcpParams)
                        {
                            key.SetValue(param.Key, param.Value, RegistryValueKind.DWord);
                        }
                    }
                }

                // 调整TCP自动调优级别
                await SetTcpAutoTuning();

                // 优化网络接口
                await OptimizeNetworkInterfaces();

                // 标记为已优化
                NetworkStateManager.Instance.SetOptimized();

                Logger.Log("TCP优化已应用");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"TCP优化失败: {ex.Message}", LogLevel.Error);
                // 发生错误时尝试恢复原始状态
                NetworkStateManager.Instance.RestoreOriginalState();
                return false;
            }
        }

        private async Task<NetworkStatus> GetNetworkStatus()
        {
            var status = new NetworkStatus();
            var ping = new Ping();

            try
            {
                // 测试多个目标服务器以获取平均延迟
                string[] testServers = { "8.8.8.8", "1.1.1.1", "208.67.222.222" };
                List<long> latencies = new List<long>();
                int packetLoss = 0;

                foreach (string server in testServers)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            var reply = await ping.SendPingAsync(server, 1000);
                            if (reply.Status == IPStatus.Success)
                            {
                                latencies.Add(reply.RoundtripTime);
                            }
                            else
                            {
                                packetLoss++;
                            }
                        }
                        catch
                        {
                            packetLoss++;
                        }
                        await Task.Delay(100);
                    }
                }

                status.Latency = latencies.Count > 0 ? (int)latencies.Average() : 0;
                status.PacketLoss = (packetLoss * 100) / (testServers.Length * 4);
            }
            catch (Exception ex)
            {
                Logger.Log($"网络状态检测失败: {ex.Message}", LogLevel.Error);
            }

            return status;
        }

        private void AdjustTcpParams(NetworkStatus status)
        {
            // 根据网络状态动态调整参数
            if (status.Latency > 200)
            {
                // 高延迟情况下的优化
                TcpParams["TcpAckFrequency"] = 0;      // 延迟ACK以减少包数
                TcpParams["GlobalMaxTcpWindowSize"] *= 2; // 增大窗口大小
                TcpParams["TcpWindowSize"] *= 2;
            }

            if (status.PacketLoss > 5)
            {
                // 高丢包率情况下的优化
                TcpParams["TcpMaxDupAcks"] = 1;        // 更快的重传
                TcpParams["DefaultTTL"] = 128;         // 增加TTL
            }
        }

        private async Task SetTcpAutoTuning()
        {
            try
            {
                // 设置TCP自动调优级别
                await ExecutePowerShellScript("Set-NetTCPSetting -SettingName InternetCustom -AutoTuningLevel Restricted");
                
                // 设置QoS策略
                await ExecutePowerShellScript("Set-NetQosPolicy -Name 'Game Traffic' -IPProtocol Both -NetworkProfile All -ThrottleRateActionBitsPerSecond 0");
            }
            catch (Exception ex)
            {
                Logger.Log($"TCP自动调优设置失败: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task OptimizeNetworkInterfaces()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                           (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                            ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet));

                foreach (var netInterface in interfaces)
                {
                    // 禁用IPv6
                    await ExecutePowerShellScript($"Disable-NetAdapterBinding -Name \"{netInterface.Name}\" -ComponentID ms_tcpip6");

                    // 优化网卡设置
                    await ExecutePowerShellScript($@"
                        Set-NetAdapterAdvancedProperty -Name ""{netInterface.Name}"" -DisplayName ""Jumbo Packet"" -DisplayValue ""Disabled"" -ErrorAction SilentlyContinue
                        Set-NetAdapterAdvancedProperty -Name ""{netInterface.Name}"" -DisplayName ""Energy Efficient Ethernet"" -DisplayValue ""Disabled"" -ErrorAction SilentlyContinue
                        Set-NetAdapterAdvancedProperty -Name ""{netInterface.Name}"" -DisplayName ""Power Saving Mode"" -DisplayValue ""Disabled"" -ErrorAction SilentlyContinue
                    ");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"网络接口优化失败: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task ExecutePowerShellScript(string script)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask);
                await process.WaitForExitAsync();

                string error = await errorTask;
                if (!string.IsNullOrEmpty(error))
                {
                    Logger.Log($"PowerShell执行错误: {error}", LogLevel.Error);
                }
            }
        }

        private class NetworkStatus
        {
            public int Latency { get; set; }
            public int PacketLoss { get; set; }
        }
    }
} 