该项目采用基于 **.NET Framework 4.7.2** 的 **NuGet** 包管理系统，通过 SDK 风格的 `.csproj` 文件直接声明第三方依赖。

### 1. 依赖声明方式
- **SDK 风格项目文件**：使用 `<PackageReference>` 元素在 `K3CloudDataDictionary.csproj` 和 `K3CloudDataDictionary.Cli/K3CloudDataDictionary.Cli.csproj` 中显式声明依赖包及其版本号。
- **无全局配置**：未发现 `Directory.Build.props`、`nuget.config` 或 `global.json` 等全局依赖配置文件，依赖解析遵循 NuGet 默认行为（通常指向 nuget.org）。

### 2. 核心依赖包
- **数据库访问**：
  - `System.Data.SqlClient` (v4.8.6)：用于连接金蝶云 SQL Server 数据库。
  - `System.Data.SQLite.Core` (v1.0.119)：用于本地元数据持久化存储。
- **UI 框架**：
  - `HandyControl` (v3.5.1)：WPF 桌面端的 UI 控件库。
- **数据处理**：
  - `Newtonsoft.Json` (v13.0.3)：CLI 工具中用于 JSON 格式输出。

### 3. 代码共享与依赖复用
- **源文件链接**：CLI 项目通过 `<Compile Include="..\..." Link="..." />` 的方式直接引用主项目的 `Helpers`、`Models` 和 `Views` 目录下的源文件。这种“链接”机制使得 CLI 工具能够复用主项目的业务逻辑和数据模型，而无需将其打包为独立的类库（Class Library），从而简化了依赖拓扑。

### 4. 开发者规范
- **版本一致性**：由于两个项目都引用了相同的底层数据库驱动（SqlClient 和 SQLite），在更新这些包的版本时，需确保两个 `.csproj` 文件中的版本号保持同步，以避免潜在的运行时冲突。
- **依赖添加**：新增第三方库时，应直接在对应的 `.csproj` 文件的 `<ItemGroup>` 中添加 `<PackageReference>`，并确保目标框架 `net472` 兼容该库。