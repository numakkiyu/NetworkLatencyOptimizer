using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NetworkLatencyOptimizer.Core
{
    public class DnsOptimizer
    {
        private readonly string[] _defaultDnsServers = new[]
        {
            "1.1.1.1", // Cloudflare
            "8.8.8.8"  // Google
        };

        public async Task<bool> OptimizeDns(string[] customDnsServers = null)
        {
            try
            {
                string[] dnsServers = customDnsServers ?? _defaultDnsServers;
                
                // 获取最佳网络接口
                NetworkInterface bestInterface = await GetBestInterface(dnsServers);
                if (bestInterface == null)
                {
                    Logger.Log("未找到合适的网络接口", LogLevel.Error);
                    return false;
                }

                // 设置DNS服务器
                return SetDnsServers(dnsServers, bestInterface.Name);
            }
            catch (Exception ex)
            {
                Logger.Log($"DNS优化失败: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<NetworkInterface> GetBestInterface(string[] dnsServers)
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface bestInterface = null;
            long bestLatency = long.MaxValue;

            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    long avgLatency = await MeasureAverageDnsLatency(dnsServers);
                    if (avgLatency < bestLatency)
                    {
                        bestLatency = avgLatency;
                        bestInterface = ni;
                    }
                }
            }

            return bestInterface;
        }

        private async Task<long> MeasureAverageDnsLatency(string[] dnsServers)
        {
            long totalLatency = 0;
            int successCount = 0;

            foreach (string dns in dnsServers)
            {
                try
                {
                    using (Ping ping = new Ping())
                    {
                        PingReply reply = await ping.SendPingAsync(dns, 1000);
                        if (reply.Status == IPStatus.Success)
                        {
                            totalLatency += reply.RoundtripTime;
                            successCount++;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return successCount > 0 ? totalLatency / successCount : long.MaxValue;
        }

        private bool SetDnsServers(string[] dnsServers, string interfaceName)
        {
            try
            {
                // 设置主DNS服务器
                string primaryDnsCommand = $"netsh interface ip set dns name=\"{interfaceName}\" source=static addr={dnsServers[0]} register=primary";
                if (!ExecuteCommand(primaryDnsCommand))
                {
                    return false;
                }

                // 设置备用DNS服务器
                for (int i = 1; i < dnsServers.Length; i++)
                {
                    string secondaryDnsCommand = $"netsh interface ip add dns name=\"{interfaceName}\" addr={dnsServers[i]} index={i + 1}";
                    if (!ExecuteCommand(secondaryDnsCommand))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"设置DNS服务器失败: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private bool ExecuteCommand(string command)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        Logger.Log($"命令执行错误: {error}", LogLevel.Error);
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"命令执行异常: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
    }
} 