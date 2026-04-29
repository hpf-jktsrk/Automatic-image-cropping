# Auto Cutout Studio

一个 Windows 桌面自动抠图工具，用于把商品、人像或主体图片快速导出为透明 PNG。

## 功能

- 打开或拖拽图片
- 自动从图片边缘识别背景并移除
- 调节背景容差、边缘羽化、边缘清理
- 可选保留柔和阴影
- 本地处理，不上传图片
- 导出透明 PNG

## 运行

```powershell
dotnet run --project .\src\AutoCutoutStudio\AutoCutoutStudio.csproj
```

## 构建

```powershell
dotnet build .\AutoCutoutStudio.sln -c Release
```

生成的程序位于：

```text
src\AutoCutoutStudio\bin\Release\net10.0-windows\AutoCutoutStudio.exe
```
