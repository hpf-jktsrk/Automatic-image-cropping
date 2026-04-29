# Auto Cutout Studio

`Automatic-image-cropping` 是一个 Windows 桌面自动抠图工具，用于把商品、人像或主体图片快速导出为透明 PNG。

它适合主体在中间、背景相对统一或干净的图片，例如商品图、证件照风格人像、白底或浅色背景图。当前版本使用本地图像算法处理，不上传图片，也不依赖网页。

## 主要功能

- 打开本地图片
- 支持把图片直接拖拽到窗口
- 自动从图片边缘识别背景并移除
- 调节背景容差、边缘羽化、边缘清理
- 可选保留柔和阴影
- 点击按钮后按当前参数重新抠图
- 导出透明 PNG
- 支持打包成单文件 Windows exe

## 使用说明

### 打开图片

点击 `打开图片` 选择一张本地图片。导入后软件会按默认参数自动抠一次图，让你先看到初始效果。

也可以直接把图片拖进窗口。

### 调整参数

调整参数时，图片不会立刻重新处理。参数变化后，状态栏会提示当前参数已调整。

点击 `应用参数并重新抠图` 后，软件才会按当前参数重新生成结果。

### 保存图片

点击 `保存 PNG` 可以把当前抠好的结果导出为透明背景 PNG。

PNG 会保留透明通道，适合继续放到 PPT、设计软件、商品主图或其他应用中使用。

## 参数说明

### 背景容差

控制哪些颜色会被认为是背景。

数值越低，背景移除越保守，主体边缘更不容易被误删，但可能留下背景残边。

数值越高，背景移除越强，浅色背景、灰白背景更容易清干净，但如果主体颜色和背景接近，主体边缘也可能被吃掉。

建议范围：

- 白底商品图：`30-45`
- 背景有轻微灰色或阴影：`40-60`
- 主体边缘被误删：调低
- 背景残留明显：调高

### 边缘羽化

控制主体边缘的柔和程度。

数值越低，边缘越硬、更锐利，适合图标、硬边商品、清晰轮廓。

数值越高，边缘越柔和，可以减少锯齿感，但太高会让主体边缘发虚。

建议范围：

- 商品硬边：`1-3`
- 人像、毛发、布料边缘：`3-6`
- 边缘发虚：调低
- 边缘锯齿明显：调高

### 边缘清理

控制是否向主体边缘内侧多清理一点背景色。

数值越低，越保留主体，适合复杂边缘。

数值越高，越容易去掉白边、灰边、背景残线，但也可能把主体外圈削薄。

建议范围：

- 默认：`1`
- 有明显白边：`2-3`
- 主体被削掉：调到 `0`

### 保留柔和阴影

开启后，软件会尝试保留主体下面或边缘附近较淡的阴影。

这个选项适合商品图，可以让主体看起来不那么悬空。如果阴影被误保留成脏边，可以关闭。

## 推荐调参顺序

1. 先调 `背景容差`，让大部分背景消失。
2. 如果边缘有白边或灰边，再加一点 `边缘清理`。
3. 最后用 `边缘羽化` 修边，让轮廓自然。
4. 每次调完后点击 `应用参数并重新抠图` 查看效果。

## 运行

```powershell
dotnet run --project .\src\AutoCutoutStudio\AutoCutoutStudio.csproj
```

也可以直接双击：

```text
Start-AutoCutoutStudio.bat
```

## 构建

```powershell
dotnet build .\AutoCutoutStudio.sln -c Release
```

普通构建生成的程序位于：

```text
src\AutoCutoutStudio\bin\Release\net10.0-windows\AutoCutoutStudio.exe
```

## 打包单文件 exe

生成可复制到其他 Windows 64 位电脑运行的独立单文件版本：

```powershell
dotnet publish .\src\AutoCutoutStudio\AutoCutoutStudio.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish\single-file --source https://api.nuget.org/v3/index.json
```

输出文件：

```text
publish\single-file\AutoCutoutStudio.exe
```

## 当前限制

当前版本使用传统图像算法，不是 AI 深度模型。它对纯色、浅色、背景相对干净的图片效果最好。

如果图片背景很复杂，或者需要处理发丝、透明物体、复杂边缘，后续可以升级为 AI 模型版，例如接入 ONNX、U2Net、MODNet 或 RMBG 类模型。

## 许可

本项目沿用仓库中的 Unlicense 公共领域许可。
