# H5MotaSaveDecoder

一个使用 C# / Windows Forms 编写的 H5 魔塔存档转换工具，支持将 LZString Base64 存档解码为 JSON，以及将编辑后的 JSON 重新打包为存档。

## Jint 分支

本分支使用 Jint.dll 在 .NET 进程内执行 LZString 1.5.0 JavaScript 库，不再由 C# 实现压缩算法。
界面、存档格式、输出目录和命名规则与 main 分支一致。
JSON 解析、格式化和 Unicode 转义仍由 C# 处理，保留原有大整数精度。

- Jint.dll：使用本机已有的 2.11.58 版本，随源码保存在 H5MotaSaveDecoder/Libraries，构建时自动复制。
- LZString：源码位于 H5MotaSaveDecoder/JavaScript/lz-string.js，并嵌入程序，不需要单独复制 JS 文件。
- 只执行内置 JavaScript，存档作为字符串参数传入；每次调用使用独立引擎。
- 保留损坏数据检查和 64 MiB UTF-16 字符数上限；单次 JavaScript 调用最多运行 2 分钟。
- 构建不需要从 NuGet 下载 Jint；运行不需要 Node.js、浏览器或游戏安装目录。
- 分发时请复制整个发布目录，包括 Jint.dll、运行配置和许可说明。

## 功能

- **解码存档**：选择无扩展名存档或使用相同格式的 `.h5save` 文件。
- **打包存档**：选择 JSON，压缩为游戏兼容的存档文本。
- 所有转换均在本机完成，不上传文件；不依赖 Node.js 或游戏安装目录。
- 输入文件保持不变。已有输出文件夹直接复用；同名输出文件会询问是否覆盖。
- JSON 无效时提示错误，不覆盖已有输出文件。

这里的“加密”实际指 LZString 压缩和 Base64 编码，不是密码学加密。

## 输出路径与命名

每次转换都以**当前选择的文件所在目录**为基准。

| 操作 | 输出子文件夹 | 文件名规则 |
| --- | --- | --- |
| 解码 | `H5MTSD_decode_json` | 完整原文件名加 `_encode.json` |
| 打包 | `H5MTSD_encode` | 删除末尾 `_encode.json`；否则仅删除最后的扩展名 |

例如选择 `D:/存档/demo_save2`，输出为 `D:/存档/H5MTSD_decode_json/demo_save2_encode.json`。
再选择这个 JSON 打包，输出为 `D:/存档/H5MTSD_decode_json/H5MTSD_encode/demo_save2`。

`custom.json` 打包后的文件名是 `custom`；`example.h5save_encode.json` 则还原为 `example.h5save`。

## 环境与构建

- Windows
- Visual Studio 2022 17.12 或更新版本，安装“.NET 桌面开发”工作负载；本项目在 17.14 上验证。
- .NET 9 SDK；运行框架依赖版本时需要 .NET 9 Desktop Runtime。

用 Visual Studio 打开 `H5MotaSaveDecoder.sln` 并运行，或在仓库根目录执行：

```powershell
dotnet build H5MotaSaveDecoder.sln -c Release
dotnet publish H5MotaSaveDecoder/H5MotaSaveDecoder.csproj -c Release -o 程序
```

随后运行 `程序/H5MotaSaveDecoder.exe`。仓库仅包含源码和测试资料，不包含本地编译产物。

## 验证

```powershell
dotnet run --project Verification/Verification.csproj -c Release
```

包含 96 项检查，覆盖 LZString 与 JavaScript 参考实现的兼容性、JSON 往返、文件命名、输出目录、覆盖保护、中文、UTF-8 BOM、大整数和窗体构造。
仓库内测试数据均为合成样本，不包含真实游戏存档。

## 兼容范围

支持采用 LZString Base64 JSON 格式的 H5 魔塔存档。
保留完整 JSON 结构，包括 `route` 字段和 `.h5save` 的外层包装；不单独解码录像，也不自动转换不同游戏的自定义存档格式。

## 许可证

本项目采用 [MIT License](LICENSE)。LZString 算法的来源及许可声明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
