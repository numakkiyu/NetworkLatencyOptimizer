using System;
using System.IO;
using Newtonsoft.Json;

namespace NetworkLatencyOptimizer.Core
{
    public class Config
    {
        public string HypixelIP { get; set; }
        public string[] DNSServers { get; set; }
        public int LatencyThreshold { get; set; }
        public bool EnableTcpOptimization { get; set; }
        public bool EnableRouteOptimization { get; set; }
        public bool EnableDnsOptimization { get; set; }
    }

    public static class ConfigManager
    {
        private static readonly string ConfigFile = "Config/config.json";
        private static Config _currentConfig;

        static ConfigManager()
        {
            string directory = Path.GetDirectoryName(ConfigFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static Config LoadConfig()
        {
            try
            {
                if (_currentConfig != null)
                {
                    return _currentConfig;
                }

                if (!File.Exists(ConfigFile))
                {
                    _currentConfig = CreateDefaultConfig();
                    SaveConfig(_currentConfig);
                    return _currentConfig;
                }

                string json = File.ReadAllText(ConfigFile);
                _currentConfig = JsonConvert.DeserializeObject<Config>(json);
                return _currentConfig;
            }
            catch (Exception ex)
            {
                Logger.Log($"加载配置失败: {ex.Message}", LogLevel.Error);
                return CreateDefaultConfig();
            }
        }

        public static void SaveConfig(Config config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
                _currentConfig = config;
            }
            catch (Exception ex)
            {
                Logger.Log($"保存配置失败: {ex.Message}", LogLevel.Error);
            }
        }

        private static Config CreateDefaultConfig()
        {
            return new Config
            {
                HypixelIP = "hypixel.net",
                DNSServers = new[] { "1.1.1.1", "8.8.8.8" },
                LatencyThreshold = 210,
                EnableTcpOptimization = true,
                EnableRouteOptimization = true,
                EnableDnsOptimization = true
            };
        }
    }
} 