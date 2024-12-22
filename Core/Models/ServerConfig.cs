using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NetworkLatencyOptimizer.Core.Models
{
    public class ServerConfig
    {
        private static readonly string ConfigFile = "Config/servers.json";
        private static readonly object LockObj = new object();

        public static List<MinecraftServer> LoadServers()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    string json = File.ReadAllText(ConfigFile);
                    return JsonConvert.DeserializeObject<List<MinecraftServer>>(json) ?? new List<MinecraftServer>();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"加载服务器配置失败: {ex.Message}", LogLevel.Error);
            }
            return new List<MinecraftServer>();
        }

        public static void SaveServers(IEnumerable<MinecraftServer> servers)
        {
            try
            {
                string directory = Path.GetDirectoryName(ConfigFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                lock (LockObj)
                {
                    string json = JsonConvert.SerializeObject(servers, Formatting.Indented);
                    File.WriteAllText(ConfigFile, json);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"保存服务器配置失败: {ex.Message}", LogLevel.Error);
            }
        }
    }
} 