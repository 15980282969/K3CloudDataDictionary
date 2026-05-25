# K3CloudDataDictionary

金蝶 K3 Cloud BOS 平台数据字典工具，用于浏览和查询金蝶 BOS 平台的实体映射元数据。

## 功能特性

- **三级业务模块树** — 按顶层分类 → 子系统 → 表单三级结构展示业务模块
- **标签页数据浏览** — 点击树节点或双击数据行逐级钻取：表单 → 实体 → 字段
- **本地数据存储** — 元数据刷新后保存到本地 SQLite，浏览数据无需远程连接
- **多连接管理** — 支持配置多个金蝶数据库连接，持久化存储
- **列头过滤** — DataGrid 列头内置过滤输入框，支持实时筛选
- **右键复制** — 右键点击单元格即可复制内容到剪贴板
- **快捷键** — `Ctrl+W` 关闭当前标签页

## 技术栈

| 类别 | 技术 |
|------|------|
| 框架 | .NET Framework 4.7.2 + WPF |
| UI 库 | HandyControl 3.5.1 |
| 本地存储 | System.Data.SQLite |
| 远程访问 | System.Data.SqlClient |
| 架构模式 | MVVM |

## 项目结构

```
K3CloudDataDictionary/
├── Helpers/
│   ├── DbHelper.cs              # 远程 SQL Server 连接工具
│   └── SQLiteHelper.cs          # 本地连接信息 SQLite 存储
├── Models/
│   ├── ConnectionInfo.cs         # 数据库连接信息模型
│   ├── FormInfo.cs               # 表单信息模型
│   ├── FormEntityInfo.cs         # 实体信息模型
│   ├── FieldInfo.cs              # 字段信息模型
│   ├── ModuleTabItem.cs          # 标签页数据模型
│   └── ModuleTreeItem.cs         # 树节点模型
├── ViewModels/
│   └── MainViewModel.cs          # 主视图模型
├── Views/
│   ├── MetadataExtractor.cs      # 元数据提取核心（继承链+扩展链合并）
│   ├── MetadataSqliteWriter.cs   # 元数据批量写入本地 SQLite
│   ├── DbHelper.cs               # 远程元数据读取
│   ├── ExtractEntities.cs        # 实体提取
│   ├── ExtractFields.cs          # 字段提取
│   └── ExtractSplits.cs          # 分录提取
├── Resources/
│   └── app.ico                   # 应用图标
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── ConnectionDialog.xaml / ConnectionDialog.xaml.cs
└── TabContentTemplateSelector.cs
```

## 使用方式

### 环境要求

- Windows 7 及以上
- .NET Framework 4.7.2 及以上
- 金蝶 K3 Cloud 数据库访问权限（仅刷新元数据时需要）

### 构建

```bash
dotnet build
```

或使用 Visual Studio 2019+ 打开 `.csproj` 文件直接构建。

### 运行流程

1. **添加连接** — 点击标题栏「实体映射管理」→「连接管理」，配置金蝶数据库连接信息
2. **刷新元数据** — 点击「刷新元数据」，从远程数据库提取元数据并保存到本地 `data/metadata.db`
3. **浏览数据** — 刷新完成后，所有数据从本地 SQLite 加载，展开左侧模块树，点击节点查看表单列表
4. **逐级钻取** — 双击表单行查看实体列表，双击实体行查看字段详情

### 本地数据

- 连接信息存储：`data/connections.db`
- 元数据存储：`data/metadata.db`

刷新元数据后，浏览数据不再需要远程连接，仅刷新时需要访问远程 SQL Server。

## 许可证

[MIT License](LICENSE)
