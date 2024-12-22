# 优化脚本
param (
    [switch]$Revert
)

function Set-TcpOptimizations {
    param (
        [bool]$Enable = $true
    )
    
    try {
        # 设置TCP参数
        $params = @{
            "TcpAckFrequency" = if ($Enable) { 1 } else { 2 }
            "TCPNoDelay" = if ($Enable) { 1 } else { 0 }
            "GlobalMaxTcpWindowSize" = if ($Enable) { 65535 } else { 16384 }
            "TcpWindowSize" = if ($Enable) { 65535 } else { 16384 }
            "DefaultTTL" = if ($Enable) { 64 } else { 128 }
        }
        
        $path = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"
        foreach ($param in $params.GetEnumerator()) {
            Set-ItemProperty -Path $path -Name $param.Key -Value $param.Value -Type DWord
        }
        
        # 设置网络自动调优级别
        $tuningLevel = if ($Enable) { "highlyrestricted" } else { "normal" }
        Set-NetTCPSetting -SettingName InternetCustom -AutoTuningLevel $tuningLevel
        
        Write-Host "TCP优化已" + $(if ($Enable) { "启用" } else { "还原" })
    }
    catch {
        Write-Error "TCP优化失��: $_"
        exit 1
    }
}

function Set-NetworkOptimizations {
    param (
        [bool]$Enable = $true
    )
    
    try {
        # 禁用IPv6
        $interfaces = Get-NetAdapter | Where-Object { $_.Status -eq "Up" }
        foreach ($interface in $interfaces) {
            if ($Enable) {
                Disable-NetAdapterBinding -Name $interface.Name -ComponentID ms_tcpip6
            }
            else {
                Enable-NetAdapterBinding -Name $interface.Name -ComponentID ms_tcpip6
            }
        }
        
        Write-Host "网络优化已" + $(if ($Enable) { "启用" } else { "还原" })
    }
    catch {
        Write-Error "网络优化失败: $_"
        exit 1
    }
}

# 主执行逻辑
if ($Revert) {
    Write-Host "正在还原网络设置..."
    Set-TcpOptimizations -Enable $false
    Set-NetworkOptimizations -Enable $false
}
else {
    Write-Host "正在应用网络优化..."
    Set-TcpOptimizations -Enable $true
    Set-NetworkOptimizations -Enable $true
}

Write-Host "优化脚本执行完成" 