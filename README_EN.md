# Network Latency Optimizer

English | [简体中文](README.md)

A network latency optimization tool designed for Minecraft players. This project is currently under development, aiming to reduce latency to international servers, especially Hypixel, through local optimization algorithms.

**Developer**: BHCN 

## Project Overview

This is an open-source network optimization tool currently under development.

### Current Features

#### 1. TCP Protocol Optimization
TCP optimization is the core component for reducing latency, implemented through:

1. **Dynamic TCP Parameter Adjustment**
```csharp
// TCP parameter optimization example
private readonly Dictionary<string, int> TcpParams = new Dictionary<string, int>
{
    { "TcpAckFrequency", 1 },     // Immediate ACK sending
    { "TCPNoDelay", 1 },          // Disable Nagle's algorithm
    { "TcpWindowSize", 65535 }    // Increase TCP window size
};
```

Parameter functions:
- `TcpAckFrequency`: Controls TCP acknowledgment packet frequency
- `TCPNoDelay`: Disables Nagle's algorithm to reduce small packet delay
- `TcpWindowSize`: Adjusts TCP window size for throughput optimization

2. **Adaptive Optimization Algorithm**
```csharp
private void AdjustTcpParams(NetworkStatus status)
{
    // Dynamically adjust parameters based on network status
    if (status.Latency > 200)
    {
        // Optimization for high latency
        TcpParams["TcpAckFrequency"] = 0;      // Delay ACK to reduce packets
        TcpParams["GlobalMaxTcpWindowSize"] *= 2; // Increase window size
    }

    if (status.PacketLoss > 5)
    {
        // Optimization for high packet loss
        TcpParams["TcpMaxDupAcks"] = 1;        // Faster retransmission
        TcpParams["DefaultTTL"] = 128;         // Increase TTL
    }
}
```

#### 2. Network Status Monitoring System

The monitoring system is implemented in Python with the following features:

1. **Real-time Latency Monitoring**
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
        logging.error(f"Ping failed: {e.output.decode('utf-8')}")
        return None
```

2. **Network Performance Analysis**
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
        logging.error(f"Failed to get network stats: {e}")
        return None
```

#### 3. Network Settings Protection Mechanism

To prevent issues during optimization, a complete backup and recovery mechanism is implemented:

1. **Settings Backup**
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

2. **Automatic Recovery**
```csharp
public bool RestoreOriginalState()
{
    if (!_isOptimized)
        return true;

    try
    {
        // Restore TCP parameters
        RestoreTcpParameters(_originalState.TcpParameters);
        // Restore network adapter settings
        RestoreNetworkAdapterSettings(_originalState.NetworkAdapterSettings);
        // Restore QoS settings
        RestoreQosSettings(_originalState.QosSettings);
        return true;
    }
    catch (Exception ex)
    {
        Logger.Log($"Failed to restore network state: {ex.Message}", LogLevel.Error);
        return false;
    }
}
```

### Features Under Development

1. **Route Optimization**
- Implementation: Automatically select optimal routes by analyzing latency and packet loss
- Current Status: Basic framework completed, route selection algorithm needs improvement

2. **DNS Optimization**
- Implementation: Smart DNS server selection and local DNS caching
- Current Status: Basic DNS switching implemented, smart selection logic needed

3. **User Interface**
- Implementation: Real-time data visualization using WPF
- Current Status: Basic interface completed, more interactive features needed

## Developer Guide

### How to Use

1. **Environment Setup**
- Visual Studio 2022 or JetBrains Rider
- .NET 6.0 SDK
- Python 3.8+
- PowerShell 7.0+

2. **Project Structure**
```
NetworkLatencyOptimizer/
├── Core/                 # Core optimization logic
│   ├── Models/          # Data models
│   ├── TcpOptimizer.cs  # TCP optimization
│   └── NetworkState.cs  # Network state management
├── UI/                  # WPF user interface
└── Monitor/            # Python monitoring module
```

3. **Standards**
- Using C# 10.0 features
- Following async programming best practices
- Detailed code comments
- Error handling and logging

### Issues to Resolve

1. **Performance Optimization**
- TCP parameter auto-tuning algorithm needs improvement
- Network status monitoring performance overhead needs optimization

2. **Compatibility**
- Compatibility testing for different Windows versions
- Compatibility handling for different network card drivers

3. **Feature Enhancement**
- Smart route selection algorithm
- DNS optimization strategy
- User interface enhancement

## Contact

- Developer: BHCN
- Blog: [https://me.tianbeigm.cn](https://me.tianbeigm.cn)
- Email: [2129373966@qq.com](mailto:2129373966@qq.com)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Special Notes**: 
1. This project is currently under development. Please use with caution as there may be unknown issues. Please provide feedback promptly. The author is not responsible for any issues that may arise.
2. Administrator privileges are required to use this software. Please be cautious with network settings.
3. This software is for learning and research purposes only. Please comply with relevant laws and regulations. 