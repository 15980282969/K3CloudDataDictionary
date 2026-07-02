## 1. 核心策略
该代码库采用 **.NET 标准的 `try-catch` 异常处理模型**，未引入自定义异常类型（Custom Exceptions）或全局错误中间件。错误处理主要依赖于以下三种模式：
- **输出参数模式 (Out Parameter)**：在底层工具类中，通过 `out string errorMessage` 返回错误信息，避免直接抛出异常，便于调用方进行逻辑判断。
- **UI 即时反馈**：在 WPF 桌面端，捕获异常后直接通过 `MessageBox.Show` 向用户展示错误详情。
- **结构化错误输出**：在 CLI 命令行工具中，顶层捕获所有未处理异常，并通过 `JsonOutputWriter` 将错误序列化为标准的 JSON 格式输出到 `stderr`。

## 2. 关键实现细节

### 2.1 数据库连接层 (`Helpers/DbHelper.cs`)
`DbHelper.TestConnection` 采用了“尝试-返回”模式：
```csharp
public static bool TestConnection(string connectionString, out string errorMessage)
{
    errorMessage = null;
    try { /* 执行连接 */ return true; }
    catch (Exception ex) { errorMessage = ex.Message; return false; }
}
```
这种设计将异常转换为布尔返回值和错误字符串，简化了上层业务逻辑（如 `ConnectionDialog`）的判断流程。

### 2.2 CLI 工具层 (`K3CloudDataDictionary.Cli/Program.cs`)
CLI 入口程序在 `Main` 方法中设置了**全局异常屏障**：
```csharp
try { /* 执行命令 */ }
catch (Exception ex)
{
    JsonOutputWriter.WriteError(command, ex.Message);
    return 1;
}
```
- **统一出口**：所有子命令（如 `fields`, `search`）产生的未捕获异常都会在此处被拦截。
- **标准化响应**：错误信息被封装为 `{ "success": false, "command": "...", "error": "..." }` 的 JSON 对象，确保自动化脚本可以稳定解析错误状态。

### 2.3 数据持久化层 (`Helpers/SQLiteHelper.cs`)
在数据库 schema 迁移过程中，使用了**静默忽略**策略：
```csharp
try { /* ALTER TABLE ADD COLUMN */ }
catch { /* 列已存在则忽略 */ }
```
这种处理方式用于处理幂等性操作，避免因重复执行迁移脚本而导致程序崩溃。

## 3. 开发者规范与建议
- **禁止吞没异常**：除了 `SQLiteHelper` 中明确的 schema 迁移场景外，严禁使用空的 `catch` 块。所有捕获的异常都应记录日志或向用户反馈。
- **CLI 错误输出**：在开发新的 CLI 命令时，应优先让异常向上冒泡至 `Program.Main` 进行统一处理，或在业务逻辑中使用 `JsonOutputWriter.WriteError` 输出错误，避免直接使用 `Console.WriteLine` 打印错误信息。
- **UI 交互反馈**：在 WPF 模块中，涉及文件 IO 或网络请求的操作必须包裹在 `try-catch` 中，并使用 `MessageBoxImage.Error` 图标提供清晰的视觉反馈。
- **缺乏错误码体系**：目前系统仅依赖 `ex.Message` 传递错误原因，未定义统一的错误码（Error Codes）。在后续迭代中，建议针对常见的业务错误（如“连接超时”、“元数据不存在”）定义枚举或常量，以提高错误定位效率。