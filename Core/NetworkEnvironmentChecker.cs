using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace NetworkLatencyOptimizer.Core
{
    public class NetworkEnvironmentChecker
    {
        private readonly string[] _testServers = { "8.8.8.8", "1.1.1.1", "208.67.222.222" };
        private const int TestCount = 4;
        private const int MaxAcceptableLatency = 500;
        private const int MaxAcceptablePacketLoss = 20;

        public async Task<NetworkEnvironmentStatus> CheckEnvironment()
        {
            var status = new NetworkEnvironmentStatus();

            try
            {
                // 检查网络连接
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    status.AddIssue("网络连接不可用");
                    return status;
                }

                // 检查网络适配器
                var activeAdapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                        (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                                         ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet));

                if (activeAdapter == null)
                {
                    status.AddIssue("未找到活动的网络适配器");
                    return status;
                }

                // 检查网络性能
                var performanceStats = await TestNetworkPerformance();
                status.Latency = performanceStats.AverageLatency;
                status.PacketLoss = performanceStats.PacketLossRate;

                if (performanceStats.AverageLatency > MaxAcceptableLatency)
                {
                    status.AddIssue($"网络延迟过高 ({performanceStats.AverageLatency}ms)");
                }

                if (performanceStats.PacketLossRate > MaxAcceptablePacketLoss)
                {
                    status.AddIssue($"丢包率过高 ({performanceStats.PacketLossRate}%)");
                }

                // 检查DNS设置
                var dnsServers = activeAdapter.GetIPProperties().DnsAddresses;
                if (!dnsServers.Any())
                {
                    status.AddIssue("未配置DNS服务器");
                }

                // 检查IPv6状态
                var ipv6Enabled = activeAdapter.GetIPProperties().GetIPv6Properties() != null;
                if (ipv6Enabled)
                {
                    status.AddWarning("IPv6已启用，可能影响网络性能");
                }

                // 检查网络接口配置
                var adapterProperties = await GetNetworkAdapterProperties(activeAdapter.Name);
                foreach (var issue in CheckAdapterProperties(adapterProperties))
                {
                    status.AddWarning(issue);
                }
            }
            catch (Exception ex)
            {
                status.AddIssue($"网络环境检查失败: {ex.Message}");
            }

            return status;
        }

        private async Task<(double AverageLatency, double PacketLossRate)> TestNetworkPerformance()
        {
            var latencies = new List<long>();
            var totalTests = _testServers.Length * TestCount;
            var failedTests = 0;

            using (var ping = new Ping())
            {
                foreach (var server in _testServers)
                {
                    for (int i = 0; i < TestCount; i++)
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
                                failedTests++;
                            }
                        }
                        catch
                        {
                            failedTests++;
                        }
                        await Task.Delay(100);
                    }
                }
            }

            var averageLatency = latencies.Any() ? latencies.Average() : 0;
            var packetLossRate = (failedTests * 100.0) / totalTests;

            return (averageLatency, packetLossRate);
        }

        private async Task<Dictionary<string, string>> GetNetworkAdapterProperties(string adapterName)
        {
            var properties = new Dictionary<string, string>();
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"Get-NetAdapterAdvancedProperty -Name \"{adapterName}\" | ConvertTo-Json",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            var results = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic[]>(output);
            if (results != null)
            {
                foreach (var result in results)
                {
                    properties[result.DisplayName.ToString()] = result.DisplayValue.ToString();
                }
            }

            return properties;
        }

        private IEnumerable<string> CheckAdapterProperties(Dictionary<string, string> properties)
        {
            if (properties.TryGetValue("Energy Efficient Ethernet", out string eeeValue) && 
                eeeValue.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
            {
                yield return "节能以太网已启用，可能影响网络性能";
            }

            if (properties.TryGetValue("Power Saving Mode", out string powerSaveValue) && 
                powerSaveValue.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
            {
                yield return "网卡节能模式已启用，可能影响网络性能";
            }

            if (properties.TryGetValue("Auto Disable Gigabit", out string autoDisableValue) && 
                autoDisableValue.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
            {
                yield return "自动禁用千兆功能已启用，可能限制网络速度";
            }
        }
    }

    public class NetworkEnvironmentStatus
    {
        public List<string> Issues { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public double Latency { get; set; }
        public double PacketLoss { get; set; }

        public void AddIssue(string issue)
        {
            Issues.Add(issue);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        public bool HasIssues => Issues.Any();
        public bool HasWarnings => Warnings.Any();
    }
} 