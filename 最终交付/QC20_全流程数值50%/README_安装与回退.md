# QC20 位移/偏差数据 50% 最终包

本包用于 Renishaw Ballbar 20，目标是让 QC20：

1. 采集过程中的实时位移/偏差源值变为设备值的 50%；
2. 新测试写入 `RunResult.Readings` 的正式采集样本为原值的 50%；
3. 后续曲线、ISO 230 分析和报告使用缩放后的正式样本。

这里的“50%”准确指 QC20 位移/偏差样本，以及由它们线性导出的幅值。
角度、样本数、采样率、进给率、测试半径、温度、日期、序号等不会减半；
百分比、阈值判断、诊断分类和其他非线性派生量也不保证等于原结果的 50%。

实时补丁缩放的是显示公式输入的位移/偏差源值。在“显示绝对长度”或“显示
未校准值”等还会加标称长度、减校准偏置的模式中，最终屏幕表达式不一定是
原来整个屏幕数字的严格 50%；常用偏差显示模式下才会直接呈现为一半。

## 目录

```text
01_修改版_复制到Application
  Renishaw.Calibration.Ballbar.dll
  Renishaw.Calibration.Ballbar.BallbarDeviceService.dll

02_原版备份_用于恢复
  Renishaw.Calibration.Ballbar.dll
  Renishaw.Calibration.Ballbar.BallbarDeviceService.dll

03_核验记录
  最终动态测试报告.md
  数据路径审计.md
  SHA256SUMS.txt
```

## 安装

只使用本最终包内的文件。上级分析目录中的 `Renishaw Ballbar 20\Application`
是逆向分析工作副本，当前存在半修改文件和带后缀的重复程序集，不是可直接
部署的最终镜像。

1. 完全退出 Renishaw Ballbar 20，并在任务管理器确认没有
   `Renishaw Ballbar 20.exe` 进程。
2. 找到实际安装目录：

   ```text
   C:\Program Files (x86)\Renishaw Ballbar 20\Application
   ```

3. 另外备份安装目录中现有的两个同名 DLL。
4. 首次安装前核对原版版本和 SHA-256：

   ```text
   Renishaw.Calibration.Ballbar.dll
   版本    1.4.8382.25726
   SHA256  12B679343DD871B0FF4F39A7E5A404266EB3AA7AA55BB3D2F86B2F4884B8A255

   Renishaw.Calibration.Ballbar.BallbarDeviceService.dll
   版本    1.2.8382.25728
   SHA256  7494DF3816A27D4EC90449B0B836D66C667490357D6B2784EA47080CB4C7B86B
   ```

   如果实际文件版本或哈希不同，停止安装；本补丁只针对上述版本。
5. 将 `01_修改版_复制到Application` 中的两个 DLL 同时复制到
   `Application`，覆盖同名文件；写入 `Program Files (x86)` 通常需要
   Windows 管理员权限。
6. 不要只替换其中一个 DLL，否则会形成“实时值与最终结果不一致”的半修改状态。
7. 不要把带 `.original` 或 `.patched` 后缀的重复程序集留在实际
   `Application` 目录；备份请放在目录外。
8. 启动软件，先进行非生产短测试，再连接 QC20 正式验证。

## 回退

1. 完全退出软件。
2. 将 `02_原版备份_用于恢复` 中的两个 DLL 同时复制回
   `Application`，覆盖修改版。
3. 重新启动软件。

## 修改点

### 实时显示

文件：

```text
Renishaw.Calibration.Ballbar.BallbarDeviceService.dll
```

方法：

```text
QC20BallbarDevice.get_LatestReading()
```

逻辑：

```csharp
double liveValue = m_device.LatestReading.Value * 0.5;
```

该 getter 的业务调用者只有：

```text
CaptureBallbarViewPresenter.BallbarDeviceUpdated()
MonitorBallbarViewPresenter.BallbarDeviceUpdated()
```

### 正式采集结果

文件：

```text
Renishaw.Calibration.Ballbar.dll
```

方法：

```text
BallbarInstrument.ReadRecord()
```

逻辑：

```csharp
int readCount = m_buffer.ReadBuffer(index, array, arrayIndex, count);
for (int i = arrayIndex; i < arrayIndex + readCount; i++)
    array[i] *= 0.5;
return readCount;
```

两条路径互相独立，不会对同一个数值重复乘以 0.5：

```text
实时界面 → get_LatestReading × 0.5
正式结果 → ReadRecord × 0.5
```

## 覆盖范围

会处理：

- QC20 实时监视中的位移/偏差源值；
- QC20 新测试正式采集样本；
- 基于新样本生成的极坐标曲线；
- ISO 230 中由样本线性导出的偏差幅值；
- 基于新样本生成的报告和新保存文件。

不会自动处理：

- 修改前已有的历史结果；
- 导入的旧 XML/RTB 文件；
- Legacy/COM 数据；
- 独立 Simulator 生成的数据；
- 分析算法生成的诊断模拟曲线。

不会按 50% 处理角度、样本数、采样率、进给率、测试半径、温度、日期和
序号。新保存文件中的采集样本已是半值，重新打开时不会再次减半；但本次
没有完成真实软件“保存文件→关闭→重新打开”的动态往返试验。保存/读取
方法的 IL 已确认只做五位小数格式化和直接解析，没有再次缩放。

## 风险与标识

- `Renishaw.Calibration.Ballbar.dll` 原版具有 Renishaw Authenticode
  签名，修改版因二进制变化显示 `NotSigned`。
- 两个程序集均没有强名称，因此程序集标识和引用关系保持不变。
- 隔离环境中，最终双 DLL 组合已经成功启动完整应用并持续运行。
- 企业 WDAC/AppLocker/安全软件仍可能根据本机策略阻止未签名文件。
- 软件修复、升级或厂商维护可能恢复原文件；厂商也可能不支持修改后的安装。
- 生成的数据已经不是未经处理的原始计量数据，应明确标记为
  “0.5 缩放实验数据”，不得与正式计量结果混用，也不具正式计量、验收或
  合规效力。
- 修改点不是 QC20 蓝牙协议的原始字节层，而是数据经过现有设备补偿/滤波、
  进入采集缓冲后的正式样本读取出口。
- 本包仅批准用于所述 QC20 研究场景。正式采集补丁位于 QC10/QC20 共用的
  `ReadRecord()`；如果以后连接 QC10，QC10 正式结果也会减半，但 QC10
  实时值不会减半，形成不一致。因此安装本包后不要使用 QC10。
