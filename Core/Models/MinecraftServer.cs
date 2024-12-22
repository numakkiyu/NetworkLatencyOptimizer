using System;

namespace NetworkLatencyOptimizer.Core.Models
{
    public class MinecraftServer
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string IconPath { get; set; }
        public int CurrentLatency { get; set; }
        public bool IsOptimized { get; set; }
        public DateTime LastOptimized { get; set; }

        public MinecraftServer()
        {
            Port = 25565; // 默认MC端口
            IconPath = "/Resources/server.png"; // 默认图标
        }
    }
} 