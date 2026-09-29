# QuickShot

使用 Windows x64、.NET 8、WPF + WinForms 框选和本地 PaddleOCR，没有迁移 UI 框架。

## 构建

在本目录（包含 QuickShot.csproj）运行：

```powershell
dotnet restore --locked-mode
dotnet build .\QuickShot.csproj -c Release
dotnet run --project .\QuickShot.csproj -c Release
```

发布：

```powershell
dotnet publish .\QuickShot.csproj -c Release -r win-x64 --self-contained true
```

发布目录：`bin\Release\net8.0-windows10.0.19041.0\win-x64\publish`。
请分发整个目录，模型和原生 DLL 不能省略。源码 ZIP 不包含 NuGet 缓存、bin/obj 或数百 MB 的发布依赖。
首次 restore 下载依赖；应用首次 OCR 只加载本地模型，无运行时下载。不要只复制 EXE。

## 保留的操作

| 动作 | 默认快捷键 |
| --- | --- |
| 框选并另存 PNG | Alt+A |
| 框选并复制截图 | Alt+C |
| 框选并识别、复制文字 | Alt+T |
| 框选并创建贴图 | Alt+P |
| 关闭当前激活的贴图 | Delete（局部快捷键） |

框选 Esc 取消。主窗口关闭后驻留托盘，菜单“退出”结束程序。贴图支持拖动、滚轮缩放、右键复制/恢复/关闭。
OCR 结果自动复制，也保留可编辑结果窗口，剪贴板暂时被占用时可以重试。
OCR 运行期间仍可保存、复制、贴图；同一时刻只允许一个识别任务。

## 保存目录

唯一默认值位于 `AppSettings.DefaultSaveDirectory`，新配置为 `D:\secreenshot`。
原有配置继续沿用，不强行覆盖用户已经保存的目录。没有 D 盘时请在设置中选择有效目录。
设置文件：`%LOCALAPPDATA%\QuickShot\settings.json`。

主界面“打开截图目录”与保存对话框使用同一个当前配置。
设置里的“打开此目录”打开正在编辑的路径；按“保存设置”后成为当前配置。
路径不存在会创建；失败明确报错，不再自动跳到“图片”目录。
保留原来的“另存为”交互，用户可以为单张截图另选位置，这不改变默认目录。
配置采用临时文件后替换的写法。无法读取/解析配置时提示并使用默认值，原文件不自动覆盖。

## OCR

- 使用现有 `LocalFullModels.ChineseV5` + `PaddleOcrAll`（检测 + 识别），CPU MKL。
- 固定 `Sdcb.PaddleOCR.Models.LocalV5` 3.3.1，避免 Local 3.3.1 的最低依赖自动解析成 LocalV5 3.0.0；新增的是现有传递依赖的显式版本约束，不是新 OCR 引擎。
- 原始截图无损 PNG → OpenCV Mat；检测 `MaxSize=null`，不自动缩小。
- 默认原图；可选小字 2 倍放大、反色、反色并放大。超过 400 万原始像素时跳过额外放大，以约束额外内存，仍保留原图分辨率。
- 不强制灰度/二值化，不做 ASCII/正则过滤，也不删除模型输出的标点、数字或空白。
- 可选倾斜文字检测；默认关闭。当前不提供自动 180°倒置分类。
- 同一个模型实例复用；串行识别。初始化失败显示具体原因，可以再次尝试。
- 取消会停止后续处理并丢弃结果，不能强行中断正在执行的原生推理；退出时等本次调用安全结束。

重点是中文、英文、数字与常用标点。日文使用同一 V5 模型尽力识别。没有增加韩文、阿拉伯文等语言模型下载器；不承诺所有语言/特殊符号均可识别，也不承诺数学公式或代码排版无误。

## 窗口与图标

主窗口双列按钮；内容自动高度。设置、消息和 OCR 窗口提供滚动区域，底部操作使用独立 Auto 行。
共享 `WindowLayout` 按当前显示器工作区和 DPI 限制窗口，初始宽度按字体比例设置，允许用户缩放。
贴图按原始像素与所在显示器 DPI 换算，限制到工作区，保存/复制始终使用原图。
框选覆盖层是特殊全屏窗口，需要使用虚拟桌面物理像素边界，不适合 SizeToContent。

沿用项目已有的取景框/镜头/纸角 Logo；ICO 已有 16/24/32/48/64/128/256 尺寸。
EXE、全部 WPF 窗口、托盘、原生通知均指向这套资源。
通知使用 Shell_NotifyIcon 的 NIIF_USER 与 hBalloonIcon；系统最终呈现仍受 Windows 通知设置影响。
替换版本前应从托盘退出旧程序，重新发布到干净目录，避免运行旧 EXE；固定到任务栏的旧快捷方式可能需要重新固定。

## 实际验证范围

本次在 Linux 上以 .NET SDK 8.0.425 和 Windows targeting 完成 Release 编译。
Windows GUI、100/125/150/175/200% 缩放、混合 DPI 多屏、Windows 原生 OCR 推理、通知最终显示尚未实机验证。
详细记录见 `VERIFICATION.md`；不将静态检查等同于 Windows 运行通过。
