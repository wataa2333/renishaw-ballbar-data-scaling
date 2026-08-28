# QC20 倍率 0.10：安装与回退

本档将 QC20 实时位移源值和正式采集样本乘以 `0.10`。

## 安装

1. 完全退出 Renishaw Ballbar 20，并确认后台没有相关进程。
2. 核对目标 `Application` 目录中的原版文件：
   - `Renishaw.Calibration.Ballbar.dll`：版本 `1.4.8382.25726`，SHA-256 `12B679343DD871B0FF4F39A7E5A404266EB3AA7AA55BB3D2F86B2F4884B8A255`
   - `Renishaw.Calibration.Ballbar.BallbarDeviceService.dll`：版本 `1.2.8382.25728`，SHA-256 `7494DF3816A27D4EC90449B0B836D66C667490357D6B2784EA47080CB4C7B86B`
3. 原版不一致时停止安装。
4. 将 `01_修改版_复制到Application` 内两份 DLL 同时复制到软件 `Application` 目录并覆盖同名文件。
5. 不要在实际 `Application` 目录中保留带 `.original`、`.bak` 或其他扩展名的同程序集副本。

## 回退

完全退出软件，将 `02_原版备份_用于恢复` 内两份 DLL 同时复制回 `Application` 目录。

## 限制

只用于 QC20 研究验证，不得用于正式计量、验收或合规报告。已保存的缩放后采集数据再次打开时不会再次乘以 0.10；旧数据和导入数据不会因为替换 DLL 自动重算。修改版 `Ballbar.dll` 的 Authenticode 签名已失效，安全策略严格的电脑可能阻止启动。

