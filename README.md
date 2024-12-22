# Network Latency Optimizer

[English](README_EN.md) | 简体中文

一个为Minecraft玩家设计的网络延迟优化工具。本项目目前处于开发阶段，通过本地算法优化网络连接，旨在降低国际服务器的延迟，特别是Hypixel服务器。

**开发者**: BHCN (北海的佰川)

## 项目说明

本项目是一个开源的网络优化工具，目前正在开发中。

### 当前实现的功能

#### 1. TCP协议优化
TCP优化是降低延迟的核心部分，主要通过以下方式实现：

1. **TCP参数动态调整**
```csharp
// TCP参数优化示例
private readonly Dictionary<string, int> TcpParams = new Dictionary<string, int>
{
    { "TcpAckFrequency", 1 },     // 立即发送ACK确认包
    { "TCPNoDelay", 1 },          // 禁用Nagle算法，减少数据包合并
    { "TcpWindowSize", 65535 }    // 增大TCP窗口，提高吞吐量
};
```

这些参数的作用：
- `TcpAckFrequency`: 控制TCP确认包的发送频率
- `TCPNoDelay`: 禁用Nagle算法，避免小数据包的延迟
- `TcpWindowSize`: 调整TCP窗口大小，优化数据传输

2. **自适应优化算法**
```csharp
private void AdjustTcpParams(NetworkStatus status)
{
    // 根据网络状态动态调整参数
    if (status.Latency > 200)
    {
        // 高延迟情况下的优化
        TcpParams["TcpAckFrequency"] = 0;      // 延迟ACK以减少包数
        TcpParams["GlobalMaxTcpWindowSize"] *= 2; // 增大窗口大小
    }

    if (status.PacketLoss > 5)
    {
        // 高丢包率情况下的优化
        TcpParams["TcpMaxDupAcks"] = 1;        // 更快的重传
        TcpParams["DefaultTTL"] = 128;         // 增加TTL
    }
}
```

#### 2. 网络状态监控系统

监控系统使用Python实现，主要功能：

1. **实时延迟监测**
```python
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
```

2. **网络性能分析**
```python
def get_network_stats(self):
    try:
        net_io = psutil.net_io_counters()
        return {
            "bytes_sent": net_io.bytes_sent,
            "bytes_recv": net_io.bytes_recv,
            "packets_sent": net_io.packets_sent,
            "packets_recv": net_io.packets_recv,
            "errin": net_io.errin,
            "errout": net_io.errout
        }
    except Exception as e:
        logging.error(f"获取网络统计失败: {e}")
        return None
```

#### 3. 网络设置保护机制

为了防止优化过程中出现问题，实现了完整的网络设置备份和恢复机制：

1. **设置备份**
```csharp
public void BackupCurrentState()
{
    _originalState = new NetworkState
    {
        TcpParameters = GetCurrentTcpParameters(),
        NetworkAdapterSettings = GetCurrentNetworkAdapterSettings(),
        QosSettings = GetCurrentQosSettings()
    };
}
```

2. **自动恢复**
```csharp
public bool RestoreOriginalState()
{
    if (!_isOptimized)
        return true;

    try
    {
        // 恢复TCP参数
        RestoreTcpParameters(_originalState.TcpParameters);
        // 恢复网络适配器设置
        RestoreNetworkAdapterSettings(_originalState.NetworkAdapterSettings);
        // 恢复QoS设置
        RestoreQosSettings(_originalState.QosSettings);
        return true;
    }
    catch (Exception ex)
    {
        Logger.Log($"恢复网络状态失败: {ex.Message}", LogLevel.Error);
        return false;
    }
}
```

### 正在开发的功能

1. **路由优化**
- 实现方案：通过分析多个路由路径的延迟和丢包率，自动选择最优路由
- 当前进度：基础框架已完成，需要完善路由选择算法

2. **DNS优化**
- 实现方案：智能DNS服务器选择和本地DNS缓存
- 当前进度：基础DNS切换功能已实现，需要添加智能选择逻辑

3. **用户界面**
- 实现方案：使用WPF实现实时数据可视化
- 当前进度：基础界面已完成，需要添加更多交互功能

## 开发者指南

### 如何使用

1. **环境配置**
- Visual Studio 2022 或 JetBrains Rider
- .NET 6.0 SDK
- Python 3.8+
- PowerShell 7.0+

2. **项目结构说明**
```
NetworkLatencyOptimizer/
├── Core/                 # 核心优化逻辑
│   ├── Models/          # 数据模型
│   ├── TcpOptimizer.cs  # TCP优化实现
│   └── NetworkState.cs  # 网络状态管理
├── UI/                  # WPF用户界面
└── Monitor/            # Python监控模块
```

3. **规范**
- 使用C# 10.0特性
- 遵循异步编程最佳实践
- 详细的代码注释
- 错误处理和日志记录

### 待解决的问题

1. **性能优化**
- TCP参数自动调优算法需要改进
- 网络状态监控的性能开销需要优化

2. **兼容性**
- 不同Windows版本的兼容性测试
- 不同网卡驱动的兼容性处理

3. **功能完善**
- 智能路由选择算法
- DNS优化策略
- 用户界面美化

## 联系方式

- 开发者: BHCN (北海的佰川)
- 博客网站: [https://me.tianbeigm.cn](https://me.tianbeigm.cn)
- 邮箱: [2129373966@qq.com](mailto:2129373966@qq.com)


## 许可证

本项目采用 MIT 许可证，详见 [LICENSE](LICENSE) 文件。

---

**特别说明**: 
1. 本项目目前处于开发阶段，请谨慎使用，可能存在未知问题，请及时反馈，发生任何问题与作者无关。
2. 使用本软件需要管理员权限，请谨慎操作网络设置。
3. 本软件仅供学习和研究使用，请遵守相关法律法规。 