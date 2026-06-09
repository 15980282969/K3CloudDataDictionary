# K3CloudDataDictionary

金蝶 K3 Cloud BOS 平台数据字典工具，用于浏览和查询金蝶 BOS 平台的实体映射元数据。

## 功能特性

- **三级业务模块树** — 按顶层分类 → 子系统 → 表单三级结构展示业务模块
- **标签页数据浏览** — 点击树节点或双击数据行逐级钻取：表单 → 实体 → 字段
- **本地数据存储** — 元数据刷新后保存到本地 SQLite，浏览数据无需远程连接
- **多连接管理** — 支持配置多个金蝶数据库连接，持久化存储，密码 DPAPI 加密
- **表单搜索** — 支持按表单名称/标识/表名搜索，提供等于、包含、左包含、右包含四种匹配方式
- **双击联查** — 双击字段行自动识别联查类型：引用对象 → 实体详情、枚举类型 → 枚举项、单据类型 → 单据类型列表
- **显示所有字段** — 在实体列表页一键展开当前表单所有实体的全部字段
- **列头过滤** — DataGrid 列头内置过滤输入框，支持实时筛选
- **列排序** — 点击列头排序，三态循环（升序 → 降序 → 取消）
- **右键复制** — 右键点击单元格即可复制内容到剪贴板
- **标签页管理** — 支持关闭当前/左侧/右侧/其他标签页，标签可滚动，下拉列表快速切换
- **扩展元数据刷新** — 支持仅刷新存在扩展的元数据，增量更新
- **快捷键** — `Ctrl+W` 关闭当前标签页

## 技术栈

| 类别 | 技术 |
|------|------|
| 框架 | .NET Framework 4.7.2 + WPF |
| UI 库 | HandyControl 3.5.1 |
| 本地存储 | System.Data.SQLite |
| 远程访问 | System.Data.SqlClient |
| 密码保护 | DPAPI (DataProtectionScope.CurrentUser) |
| 架构模式 | MVVM |

## 项目结构

```
K3CloudDataDictionary/
├── Helpers/
│   ├── DbHelper.cs              # 远程 SQL Server 连接与查询
│   ├── SQLiteHelper.cs          # 本地连接信息 SQLite 存储
│   └── PasswordHelper.cs        # DPAPI 密码加解密
├── Models/
│   ├── ConnectionInfo.cs         # 数据库连接信息模型
│   ├── FormInfo.cs               # 表单信息模型
│   ├── FormEntityInfo.cs         # 实体信息模型
│   ├── FieldInfo.cs              # 字段信息模型
│   ├── AllFieldInfo.cs           # 所有字段信息模型
│   ├── BillTypeInfo.cs           # 单据类型模型
│   ├── EnumItemInfo.cs           # 枚举项模型
│   ├── OperatorItem.cs           # 搜索运算符模型
│   ├── ModuleTabItem.cs          # 标签页数据模型
│   └── ModuleTreeItem.cs         # 树节点模型
├── ViewModels/
│   ├── MainViewModel.cs          # 主视图模型
│   └── RelayCommand.cs           # ICommand 实现
├── Views/
│   ├── MetadataExtractor.cs      # 元数据提取核心（继承链+扩展链合并）
│   ├── MetadataSqliteWriter.cs   # 元数据批量写入本地 SQLite
│   ├── MetadataDbHelper.cs       # 远程元数据读取
│   ├── ExtractEntities.cs        # XML 实体提取
│   ├── ExtractFields.cs          # XML 字段提取
│   └── ExtractSplits.cs          # XML 分录提取
├── Resources/
│   └── app.ico                   # 应用图标
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── ConnectionDialog.xaml / ConnectionDialog.xaml.cs
└── TabContentTemplateSelector.cs
```

## 数据模型说明

### 表单信息

| 列名 | 说明 |
|------|------|
| 单据标识 | 表单的唯一标识符（如 BD_PurchaseOrder） |
| 单据名称 | 表单的中文名称 |
| 类型 | 模型类型（基础资料/单据等） |
| 所属子系统 | 所属子系统名称 |

### 实体信息

| 列名 | 说明 |
|------|------|
| 表单标识 | 所属表单的标识符 |
| 单据名称 | 所属表单的中文名称 |
| 标识 | 实体的 Key 值 |
| ORM实体名 | 实体的 EntryName，对应 ORM 中的实体属性名 |
| 实体名称 | 实体的中文名称 |
| 表名 | 实体对应的数据库表名 |
| 分录主键 | 分录实体的主键字段名 |
| 元素类型 | 实体的元素类型（如单据类型） |

### 字段信息

| 列名 | 说明 |
|------|------|
| 标识 | 字段的 Key 值 |
| 名称 | 字段的中文名称 |
| 字段名 | 字段在数据库中的列名 |
| 绑定实体属性 | 字段绑定到 ORM 实体的属性名 |
| 元素类型 | 字段的元素类型 |
| 引用对象 | 引用对象的表单标识（双击可联查） |
| 枚举类型 | 枚举类型名称（双击可联查） |
| 拆分表 | 字段所属的拆分表后缀 |
| 拆分说明 | 拆分表的描述信息 |

## 使用方式

### 环境要求

- Windows 7 及以上
- .NET Framework 4.7.2 及以上
- 金蝶 K3 Cloud 数据库访问权限（仅刷新元数据时需要）

### 构建

```bash
dotnet build
```

或使用 Visual Studio 2019+ 打开 `.sln` 文件直接构建。

### 运行流程

1. **添加连接** — 点击标题栏「实体映射管理」→「连接管理」，配置金蝶数据库连接信息
2. **刷新元数据** — 点击「重新获取元数据」，从远程数据库提取元数据并保存到本地 `data/metadata.db`
3. **浏览数据** — 刷新完成后，所有数据从本地 SQLite 加载，展开左侧模块树，点击节点查看表单列表
4. **逐级钻取** — 双击表单行查看实体列表，双击实体行查看字段详情
5. **联查跳转** — 双击字段行自动联查：引用对象→实体、枚举→枚举项、单据类型→单据类型列表
6. **搜索** — 在搜索栏输入关键词，选择匹配方式后查询

### 本地数据

- 连接信息存储：`data/connections.db`
- 元数据存储：`data/metadata.db`

刷新元数据后，浏览数据不再需要远程连接，仅刷新时需要访问远程 SQL Server。

## 元数据提取原理

工具从金蝶 K3 Cloud 的 `T_META_OBJECTTYPE` 表提取元数据，处理流程：

1. **加载基础信息** — 一次性读取所有对象的基础信息（不含 XML）
2. **构建继承链** — 根据 `FINHERITPATH` 解析继承关系
3. **构建扩展链** — 根据 `FBASEOBJECTID` 解析扩展关系
4. **合并元数据** — 按继承链+扩展链顺序逐层合并实体、字段、分录信息
5. **写入本地** — 批量写入 SQLite，支持事务和进度回调

## 许可证

[MIT License](LICENSE)
