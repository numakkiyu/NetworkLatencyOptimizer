using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NetworkLatencyOptimizer.Core
{
    public class RouteOptimizer
    {
        private readonly string _serverIp;
        
        public RouteOptimizer(string serverIp)
        {
            _serverIp = serverIp;
        }

        public async Task<bool> OptimizeRoute()
        {
            try
            {
                // 获取最佳网络接口
                NetworkInterface bestInterface = await GetBestInterface();
                if (bestInterface == null)
                {
                    Logger.Log("未找到合适的网络接口", LogLevel.Error);
                    return false;
                }

                // 获取网关IP
                string gatewayIp = GetGatewayIp(bestInterface);
                if (string.IsNullOrEmpty(gatewayIp))
                {
                    Logger.Log("未找到网关IP", LogLevel.Error);
                    return false;
                }

                // 添加静态路由
                return AddStaticRoute(_serverIp, gatewayIp, bestInterface.Name);
            }
            catch (Exception ex)
            {
                Logger.Log($"路由优化失败: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<NetworkInterface> GetBestInterface()
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
                    long latency = await MeasureLatency(_serverIp);
                    if (latency < bestLatency)
                    {
                        bestLatency = latency;
                        bestInterface = ni;
                    }
                }
            }

            return bestInterface;
        }

        private string GetGatewayIp(NetworkInterface ni)
        {
            IPInterfaceProperties ipProps = ni.GetIPProperties();
            foreach (GatewayIPAddressInformation gateway in ipProps.GatewayAddresses)
            {
                return gateway.Address.ToString();
            }
            return null;
        }

        private bool AddStaticRoute(string serverIp, string gatewayIp, string interfaceName)
        {
            string command = $"route add {serverIp} mask 255.255.255.255 {gatewayIp} if {GetInterfaceIndex(interfaceName)}";
            return ExecuteCommand(command);
        }

        private int GetInterfaceIndex(string interfaceName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/c netsh interface ipv4 show interfaces",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (var line in output.Split('\n'))
                {
                    if (line.Contains(interfaceName))
                    {
                        var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return int.Parse(parts[0]);
                    }
                }
            }

            throw new Exception("Interface not found.");
        }

        private async Task<long> MeasureLatency(string ip)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(ip, 1000);
                    return reply.Status == IPStatus.Success ? reply.RoundtripTime : long.MaxValue;
                }
            }
            catch
            {
                return long.MaxValue;
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