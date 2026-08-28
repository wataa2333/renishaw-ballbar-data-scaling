# 雷尼绍QC20球杆仪位移偏差数据倍率切换工具

版本：1.1.0  
制作人：wataa  
邮箱：wataa2333@gmail.com

## 项目目标

这是一个免安装的 WPF 单 EXE 工具，面向指定版本的 Renishaw Ballbar 20 / QC20。主要功能：

- Google Authenticator 兼容的离线 TOTP 登录；
- 手动输入 1%–500%；
- 预设倍率按升序排列；
- 同时处理实时读取与正式采集两条数据路径；
- 修改前核验原版 SHA-256；
- 自动备份两份原版 DLL；
- 双文件替换失败自动回滚；
- 检测 Renishaw Ballbar 20 进程并征得用户同意后结束；
- 恢复原版；
- 生成独立手动替换包；
- 日志和备份优先写入 EXE 同目录的 `Data`；
- 目录不可写时回退到 `C:\ProgramData\QC20倍率切换工具`；
- 只有目标目录不可写时才请求 UAC。
- Windows 10/11 高 DPI（PerMonitorV2）适配；
- 主窗口固定为 `640 × 650`，登录窗口固定为 `480 × 540`；
- 禁止调整窗口大小和最大化，页面无滚动条；
- 手动输入与预设列表使用固定内容区域，切换时主页面控件不会位移；
- 全局统一使用 `Microsoft YaHei UI`，标题、卡片标题和操作按钮采用一致的字重层级；
- 用户头像同时作为窗口、任务栏和 EXE 图标。

## 源码结构

- `Program.cs`：程序入口。
- `LoginWindow.cs`：TOTP 登录页。
- `MainWindow.cs`：主界面和操作编排。
- `WpfTheme.cs`：统一字体、颜色、圆角和控件视觉规范。
- `WpfUi.cs`：WPF 圆角控件、头像和通用布局辅助。
- `TotpService.cs`：RFC 6238 动态口令。
- `SecretVault.cs`：拆分保存的解码后密钥。
- `EncryptedResourceLoader.cs`：AES-256-CBC + HMAC-SHA256 资源解密和完整性校验。
- `Qc20PatchService.cs`：两条倍率注入路径。
- `BackupService.cs`：原版备份、双文件替换与失败回滚。
- `ProcessService.cs`：Renishaw 进程检测与停止。
- `PermissionService.cs`：写权限检测和按需 UAC。
- `ManualPackageService.cs`：手动替换包生成。
- `CurrentStateService.cs`：当前倍率与哈希状态。
- `DwmHelper.cs`：Windows 11 Mica、圆角和深色标题栏。
- `Assets/avatar.jpg`：头像构建输入。
- `Assets/app.ico`：应用程序图标构建输入。
- `Lib/dnlib.dll`：补丁引擎构建依赖。
- `build.ps1`：加密资源并调用 Windows 自带 C# 编译器。

## 构建

Windows 10/11 无需安装 .NET SDK。使用系统自带的 .NET Framework 编译器：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

输出：

```text
dist\雷尼绍QC20球杆仪位移偏差数据倍率切换工具.exe
```

## 指定的目标 DLL

```text
Renishaw.Calibration.Ballbar.dll
版本 1.4.8382.25726
原版 SHA-256 12B679343DD871B0FF4F39A7E5A404266EB3AA7AA55BB3D2F86B2F4884B8A255

Renishaw.Calibration.Ballbar.BallbarDeviceService.dll
版本 1.2.8382.25728
原版 SHA-256 7494DF3816A27D4EC90449B0B836D66C667490357D6B2784EA47080CB4C7B86B
```

原版哈希不同或首次运行时已是未知修改版，工具会拒绝建立备份和修改。

## 安全说明

- Base32 明文密钥没有写入源码或 EXE。
- 解码后的密钥使用两个字节片段异或重组。
- `dnlib` 和头像经过 AES-256-CBC 加密，并使用 HMAC-SHA256 验证密文完整性。
- TOTP 采用 HMAC-SHA1、6 位、30 秒周期，接受前后各一个时间窗口。
- 连续失败 5 次临时锁定 60 秒。
- 日志不记录动态口令或密钥。

离线 EXE 中的固定密钥无法做到不可提取；专业逆向人员仍可能恢复密钥或修改验证分支。本项目没有使用可能显著触发杀毒软件的激进加壳或反调试器，也没有 Authenticode 证书，因此最终 EXE 当前为未签名。

本次提供的密钥已出现在对话记录中，而且只有 80 bit。长期正式分发前，建议生成新的 160 bit / 32 位 Base32 密钥后重新构建。

## 计量限制

工具及其输出仅限学习研究测试，不得用于正式校准、计量溯源、合格判定、设备验收或对外合规报告。它只缩放位移偏差数据源；角度、温度、半径、日期等字段不按倍率改变。
