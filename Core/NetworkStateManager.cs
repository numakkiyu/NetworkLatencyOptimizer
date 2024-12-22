using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace NetworkLatencyOptimizer.Core
{
    public class NetworkStateManager
    {
        private static readonly string BackupFile = "Config/network_backup.json";
        private static NetworkStateManager _instance;
        private NetworkState _originalState;
        private bool _isOptimized;

        public static NetworkStateManager Instance => _instance ??= new NetworkStateManager();

        private NetworkStateManager()
        {
            _originalState = new NetworkState();
            _isOptimized = false;
        }

        public void BackupCurrentState()
        {
            try
            {
                _originalState = new NetworkState
                {
                    TcpParameters = GetCurrentTcpParameters(),
                    NetworkAdapterSettings = GetCurrentNetworkAdapterSettings(),
                    QosSettings = GetCurrentQosSettings()
                };

                // 保存备份到文件
                string directory = Path.GetDirectoryName(BackupFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(BackupFile, JsonConvert.SerializeObject(_originalState));
                Logger.Log("网络状态已备份");
            }
            catch (Exception ex)
            {
                Logger.Log($"备份网络状态失败: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        public bool RestoreOriginalState()
        {
            if (!_isOptimized)
            {
                return true; // 如果没有优化过，不需要恢复
            }

            try
            {
                // 如果有备份文件，从文件加载
                if (File.Exists(BackupFile))
                {
                    _originalState = JsonConvert.DeserializeObject<NetworkState>(File.ReadAllText(BackupFile));
                }

                // 恢复TCP参数
                RestoreTcpParameters(_originalState.TcpParameters);

                // 恢复网络适配器设置
                RestoreNetworkAdapterSettings(_originalState.NetworkAdapterSettings);

                // 恢复QoS设置
                RestoreQosSettings(_originalState.QosSettings);

                _isOptimized = false;
                Logger.Log("网络状态已恢复");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"恢复网络状态失败: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        public void SetOptimized()
        {
            _isOptimized = true;
        }

        private Dictionary<string, int> GetCurrentTcpParameters()
        {
            var parameters = new Dictionary<string, int>();
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
            {
                if (key != null)
                {
                    foreach (var name in key.GetValueNames())
                    {
                        var value = key.GetValue(name);
                        if (value is int intValue)
                        {
                            parameters[name] = intValue;
                        }
                    }
                }
            }
            return parameters;
        }

        private Dictionary<string, string> GetCurrentNetworkAdapterSettings()
        {
            var settings = new Dictionary<string, string>();
            // 使用PowerShell获取当前网络适配器设置
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "Get-NetAdapter | Get-NetAdapterAdvancedProperty | ConvertTo-Json",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var properties = JsonConvert.DeserializeObject<dynamic[]>(output);
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    string key = $"{prop.Name}_{prop.DisplayName}";
                    settings[key] = prop.DisplayValue.ToString();
                }
            }
            return settings;
        }

        private Dictionary<string, string> GetCurrentQosSettings()
        {
            var settings = new Dictionary<string, string>();
            // 获取当前QoS策略设置
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "Get-NetQosPolicy | ConvertTo-Json",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var policies = JsonConvert.DeserializeObject<dynamic[]>(output);
            if (policies != null)
            {
                foreach (var policy in policies)
                {
                    settings[policy.Name.ToString()] = policy.ThrottleRateActionBitsPerSecond.ToString();
                }
            }
            return settings;
        }

        private void RestoreTcpParameters(Dictionary<string, int> parameters)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
            {
                if (key != null)
                {
                    foreach (var param in parameters)
                    {
                        key.SetValue(param.Key, param.Value, RegistryValueKind.DWord);
                    }
                }
            }
        }

        private void RestoreNetworkAdapterSettings(Dictionary<string, string> settings)
        {
            foreach (var setting in settings)
            {
                var parts = setting.Key.Split('_');
                if (parts.Length == 2)
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"Set-NetAdapterAdvancedProperty -Name \"{parts[0]}\" -DisplayName \"{parts[1]}\" -DisplayValue \"{setting.Value}\" -ErrorAction SilentlyContinue",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
            }
        }

        private void RestoreQosSettings(Dictionary<string, string> settings)
        {
            foreach (var setting in settings)
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"Set-NetQosPolicy -Name \"{setting.Key}\" -ThrottleRateActionBitsPerSecond {setting.Value}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }
    }

    public class NetworkState
    {
        public Dictionary<string, int> TcpParameters { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> NetworkAdapterSettings { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QosSettings { get; set; } = new Dictionary<string, string>();
    }
} 