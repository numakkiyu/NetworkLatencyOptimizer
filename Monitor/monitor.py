import subprocess
import time
import re
import logging
import json
import psutil
import os
from datetime import datetime

class NetworkMonitor:
    def __init__(self):
        self.config = self.load_config()
        self.setup_logging()
        self.servers = {}
        self.load_servers()
        
    def load_config(self):
        try:
            with open("Config/config.json", "r") as f:
                return json.load(f)
        except Exception as e:
            print(f"加载配置失败: {e}")
            return {
                "LatencyThreshold": 210
            }
    
    def load_servers(self):
        try:
            with open("Config/servers.json", "r") as f:
                self.servers = json.load(f)
        except Exception as e:
            print(f"加载服务器列表失败: {e}")
            self.servers = {}
    
    def setup_logging(self):
        log_dir = "Logs"
        if not os.path.exists(log_dir):
            os.makedirs(log_dir)
            
        logging.basicConfig(
            filename=os.path.join(log_dir, "monitor.log"),
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s'
        )
    
    def ping_server(self, server_ip):
        try:
            output = subprocess.check_output(
                ["ping", "-n", "4", server_ip],
                stderr=subprocess.STDOUT
            ).decode('utf-8')
            
            match = re.search(r'Average = (\d+)ms', output)
            if match:
                return float(match.group(1))
            return None
        except subprocess.CalledProcessError as e:
            logging.error(f"Ping失败: {e.output.decode('utf-8')}")
            return None
    
    def get_network_stats(self):
        try:
            net_io = psutil.net_io_counters()
            return {
                "bytes_sent": net_io.bytes_sent,
                "bytes_recv": net_io.bytes_recv,
                "packets_sent": net_io.packets_sent,
                "packets_recv": net_io.packets_recv,
                "errin": net_io.errin,
                "errout": net_io.errout,
                "dropin": net_io.dropin,
                "dropout": net_io.dropout
            }
        except Exception as e:
            logging.error(f"获取网络统计失败: {e}")
            return None
    
    def optimize_if_needed(self, server_name, latency):
        if latency > self.config["LatencyThreshold"]:
            logging.info(f"服务器 {server_name} 延迟 ({latency}ms) 超过阈值 ({self.config['LatencyThreshold']}ms)，尝试优化...")
            try:
                subprocess.call(["powershell.exe", "-File", "Scripts/optimize.ps1"])
                logging.info(f"服务器 {server_name} 优化完成")
            except Exception as e:
                logging.error(f"执行优化脚本失败: {e}")
    
    def monitor(self):
        logging.info("网络监控启动")
        last_stats = self.get_network_stats()
        last_time = time.time()
        
        while True:
            try:
                # 监控所有服务器
                for server_name, server_info in self.servers.items():
                    if server_info.get("AutoOptimize", True):
                        latency = self.ping_server(server_info["Address"])
                        if latency:
                            logging.info(f"服务器 {server_name} 当前延迟: {latency}ms")
                            self.optimize_if_needed(server_name, latency)
                            
                            # 更新服务器状态
                            server_info["CurrentLatency"] = latency
                            server_info["LastCheck"] = datetime.now().isoformat()
                
                # 计算网络统计
                current_stats = self.get_network_stats()
                current_time = time.time()
                
                if last_stats and current_stats:
                    time_diff = current_time - last_time
                    bytes_sent_sec = (current_stats["bytes_sent"] - last_stats["bytes_sent"]) / time_diff
                    bytes_recv_sec = (current_stats["bytes_recv"] - last_stats["bytes_recv"]) / time_diff
                    
                    logging.info(f"上传速度: {bytes_sent_sec/1024:.2f} KB/s")
                    logging.info(f"下载速度: {bytes_recv_sec/1024:.2f} KB/s")
                
                # 保存服务器状态
                with open("Config/servers.json", "w") as f:
                    json.dump(self.servers, f, indent=2)
                
                last_stats = current_stats
                last_time = current_time
                
                time.sleep(60)  # 每分钟检查一次
                
            except Exception as e:
                logging.error(f"监控过程发生错误: {e}")
                time.sleep(60)  # 发生错误时等待1分钟后继续

if __name__ == "__main__":
    monitor = NetworkMonitor()
    monitor.monitor() 