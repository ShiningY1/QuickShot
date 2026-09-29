# 主要修改文件

| 文件 | 修改 |
| --- | --- |
| Windows/MainWindow.xaml / .cs | 双列布局、统一命名、打开目录、分离截图与 OCR 状态、设置事务与安全退出，修复 Icon 类型冲突 |
| Windows/SettingsWindow.xaml / .cs | 可滚动内容、独立底部按钮、局部关闭键和 OCR 输入选项、路径校验、标准化热键冲突 |
| Windows/PinWindow.xaml / .cs | Delete 局部关闭、修改即生效、原像素按 DPI 换算、工作区边界、清晰等比显示 |
| Windows/OcrWindow.xaml / .cs | 新增可编辑识别结果、进度状态、错误详情、取消与复制重试 |
| Windows/MessageWindow.cs | 可滚动并统一图标的提示窗口 |
| Windows/CaptureOverlayForm.cs | 设置无边框/不缩放后再应用屏幕边界，保持物理像素框选 |
| Services/WindowLayout.cs | 共享字体比例初始宽度、当前显示器工作区限制与 DPI 更新 |
| Services/SaveDirectory.cs | 统一完整路径规范化、创建、打开；无隐式备用目录 |
| Services/SettingsService.cs | 配置读取容错、明确提示、原子替换保存 |
| Services/HotkeyGesture.cs / GlobalHotkeyManager.cs | 允许局部单键、拒绝纯修饰键、全局热键仍要求修饰键 |
| Services/OcrService.cs | 原图无损输入、可选放大/反色、初始化进度、Unicode 保留、模型复用、异步安全释放 |
| Services/TrayService.cs | 原生托盘与自定义通知图标，资源释放、Explorer 重启恢复 |
| Models/AppSettings.cs | 唯一默认目录、新增关闭键和 OCR 选项 |
| App.xaml / App.xaml.cs | 按钮自适应样式、可选 OCR 验证入口 |
| QuickShot.csproj / packages.lock.json | 固定配套 LocalV5 模型包版本与依赖锁定 |
| app.manifest | WPF/WinForms 共享进程明确声明 PerMonitorV2 |
| Diagnostics/OcrVerification.cs | Windows 可重复 OCR 样本与实际识别报告 |

没有换 OCR 引擎，没有增加新的 UI 框架或在线服务依赖。Logo 沿用现有资源并统一引用。
