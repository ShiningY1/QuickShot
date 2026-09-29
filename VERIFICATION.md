# 验证记录与 Windows 验收

## 本次已执行

- 在现有 QuickShot-WPF-v1-source.zip 基础上修改；保留原工程、目标框架、截图框选和四个主要动作。
- 还原实际 NuGet 依赖，固定 LocalV5 3.3.1；`packages.lock.json` 记录解析版本及内容哈希。
- .NET 8 Release / win-x64 跨平台编译，包含全部 XAML 编译。最终结果见 `build-validation.txt`。
- 静态检查所有 Window/Form：MainWindow、SettingsWindow、OcrWindow、PinWindow、MessageWindow、CaptureOverlayForm。
- 检查所有图标引用和 ICO 七种尺寸；保留原有 Logo 资源。
- 检查保存目录只有一个默认配置来源、保存与打开统一，无静默路径回退。
- 检查最终 OCR 文本不经过 ASCII/正则/Trim 清洗；原截图无损输入。
- 检查局部贴图关闭、标准化快捷键冲突检测、失败回滚、退出注销和模型释放顺序。

## 尚未执行：Windows 实机运行

当前执行环境为 Linux，没有 Windows 桌面或原生 Paddle Windows DLL 执行环境。
因此下面是待实机验收项目，不是已通过的结果。

| 项目 | 实机方法 / 预期 |
| --- | --- |
| 主窗口 | 每个缩放比例打开，缩窄/缩高窗口，按钮和说明可见或可滚动 |
| 设置窗口 | 每个比例打开，滚到底部；说明完整，保存/取消始终可操作 |
| OCR 结果窗口 | 长文本、长错误信息、调整宽高，结果区可滚动，复制/关闭可操作 |
| 消息窗口 | 长路径/错误信息，滚动查看全文，确定按钮可操作 |
| DPI | 分别测试 100%、125%、150%、175%、200%，低分辨率及双屏不同缩放 |
| 目录 | 将目录改为含中文及空格的路径，保存；打开和另存为初始位置相同；重启后仍一致 |
| 目录错误 | 不存在目录可创建；无权限/不存在盘符提示错误，不跳转其他位置 |
| Logo | EXE、任务栏、每个窗口、托盘、操作完成通知均检查 |
| 默认 Delete | 打开 3 个贴图，激活其中一个按 Delete，只关闭这个窗口 |
| 修改关闭键 | 改 Ctrl+Shift+D，现有与新贴图均生效；重启仍有效 |
| 输入安全 | 在设置或 OCR 文本框按 Delete，只删除文字，不关闭贴图 |
| 快捷键冲突 | Ctrl+A 与 Control+A、重复动作被拒绝；外部程序占用时清晰报错，旧设置保持 |
| 并发 | OCR 过程中继续截图/复制/贴图；重复 OCR 提示等待，不并发使用模型 |
| 连续操作 | 连续 20 次截图/贴图/识别；观察内存和句柄是否持续异常增长 |
| 退出 | OCR 过程中退出，等本机调用结束后进程退出；热键可被其他程序注册 |
| 配置损坏 | 备份设置后写入无效 JSON；启动有提示，截图界面仍可用 |
| 依赖损坏 | 在测试发布副本移走原生 OCR DLL，首次 OCR 显示错误，截图功能可继续 |
| 超大/多屏贴图 | 宽图、长图跨屏拖动、滚轮、恢复大小；窗口保持在工作区内且原图复制不变 |

## 可重复的 OCR 实机测试

已提供 `Diagnostics/OcrVerification.cs`，使用同一个生产 OcrService。
在 Windows 构建完成后，在项目目录的 PowerShell 中运行：

```powershell
$exe = Join-Path $PWD 'bin\Release\net8.0-windows10.0.19041.0\win-x64\QuickShot.exe'
$report = Join-Path $PWD 'ocr-verification'
Start-Process -FilePath $exe -ArgumentList @('--verify-ocr', ('"' + $report + '"')) -Wait
Get-Content (Join-Path $report 'ocr-results.json') -Encoding UTF8
```

输出 9 张实际输入 PNG 和 JSON：中文、英文、混排、数字、符号、日文、小字、黑底、IDE 风格代码。
记录期望文字、实际文字、实际字体、模型分数、字符编辑距离和字符错误率。没有过滤标点或空格来美化结果。
退出码 0 仅表示报告生成完成；须检查各条 Status、Actual、ExactMatch 和 CharacterErrorRate。
缺少指定字体时 Windows 会回退，报告中的 ActualFont 用于解释差异。
再使用真实网页、IDE 与截图样本验证；合成文字不等同于所有实际屏幕场景。

## 已知限制

- 尚无实测结果证明 OCR 准确率提升幅度。预处理和依赖修复已实现，效果必须以真实图片衡量。
- 未提供任意语言包下载；日文尽力支持，其他语言不能保证。
- 不能识别模型字典以外的全部符号；后处理保留 Unicode 不等于模型一定能预测这些字符。
- OCR 分数是模型内部置信度，不是实测准确率。
- 原生推理不能安全强制取消，因此取消/退出可能等待；没有虚构下载百分比。
- 原图不缩小意味着超大框选可能使用较多内存；建议只选文字区域。
- 精确空格、表格布局、公式与代码缩进不保证复原。
- 混合 DPI 多屏坐标和系统通知需要 Windows 验证；勿将跨平台编译成功当作 GUI 验收通过。

## 实现依据

- https://github.com/sdcb/PaddleSharp/blob/master/docs/ocr.md
- https://www.nuget.org/packages/Sdcb.PaddleOCR.Models.Local/3.3.1
- https://www.nuget.org/packages/Sdcb.PaddleOCR.Models.LocalV5/3.3.1
- https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataw
