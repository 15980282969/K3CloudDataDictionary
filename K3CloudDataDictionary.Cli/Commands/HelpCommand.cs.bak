using System;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// 帮助命令
    /// </summary>
    public static class HelpCommand
    {
        public static void ShowHelp()
        {
            Console.WriteLine(@"K3Cloud 数据字典 CLI 工具 (k3cli)

用法: k3cli <command> [options]

命令:
  fields          查询表单字段信息
  search          模糊搜索字段或表
  form            查询表单元数据
  billtype        查询单据类型（列表/详情）
  billstatus      查询单据状态字段枚举值（elementType=40）
  assistantdata   查询辅助资料列表
  enum            查询枚举值列表（下拉列表）
  resolve         解析对象 ID 对应的表单信息
  connections     管理数据库连接
  help            显示此帮助信息

全局选项:
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  # 查询表单字段
  k3cli fields --form PUR_PurchaseOrder
  k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry

  # 模糊搜索
  k3cli search --keyword ""物料""
  k3cli search --keyword ""FMaterialId"" --type field
  k3cli search --keyword ""PO_Order"" --type table

  # 查询表单元数据
  k3cli form --id PUR_PurchaseOrder

  # 查询单据类型
  k3cli billtype --form PUR_PurchaseOrder
  k3cli billtype --id <billTypeId>
  k3cli billtype --keyword ""采购""

  # 查询单据状态字段枚举值
  k3cli billstatus --form PUR_PurchaseOrder
  k3cli billstatus --form PUR_PurchaseOrder --field FDocumentStatus
  k3cli billstatus --form PUR_PurchaseOrder --keyword ""已审核""

  # 查询辅助资料列表
  k3cli assistantdata --id <lookUpObjectId>

  # 查询枚举值列表（下拉列表选项）
  k3cli enum --id <enumTypeId>

  # 解析 lookUpObject 对应的表单
  k3cli resolve --id 6099b796-9e56-434e-895e-a1628d12d4c2

  # 使用指定连接
  k3cli fields --form PUR_PurchaseOrder --connection 1

  # 管理连接
  k3cli connections list
  k3cli connections add --server 192.168.1.100 --db AISC001 --user sa --password xxx --default
  k3cli connections test --id 1
");
        }

        public static void ShowFieldsHelp()
        {
            Console.WriteLine(@"用法: k3cli fields [options]

选项:
  --form <identifier>       表单标识（必填），如 PUR_PurchaseOrder
  --entity <key>            实体 Key（可选），如 FK_BillEntry
  --keyword <keyword>       字段搜索关键词（可选），支持模糊/精确匹配
  --exact, -e               精确匹配模式（完全相等，不区分大小写）
                            默认使用模糊匹配（包含关键词）
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  # 查询表单所有字段
  k3cli fields --form PUR_PurchaseOrder

  # 查询指定实体的所有字段
  k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry

  # 模糊搜索字段（在指定表单/实体内）
  k3cli fields --form PUR_PurchaseOrder --keyword ""物料""
  k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry --keyword ""FMaterialId""

  # 精确搜索字段
  k3cli fields --form PUR_PurchaseOrder --keyword ""FMaterialId"" --exact
  k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry --keyword ""物料"" -e
");
        }

        public static void ShowSearchHelp()
        {
            Console.WriteLine(@"用法: k3cli search [options]

选项:
  --keyword <keyword>       搜索关键词（必填）
  --type <field|table>      搜索类型：field（字段）或 table（表），默认 table
  --exact, -e               精确匹配模式（完全相等，不区分大小写）
                            默认使用模糊匹配（包含关键词）
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  # 模糊搜索（默认）
  k3cli search --keyword ""物料""
  k3cli search --keyword ""FMaterialId"" --type field
  k3cli search --keyword ""PO_Order"" --type table

  # 精确搜索
  k3cli search --keyword ""FMaterialId"" --exact
  k3cli search --keyword ""物料"" -e --type field
");
        }

        public static void ShowFormHelp()
        {
            Console.WriteLine(@"用法: k3cli form [options]

选项:
  --id <identifier>         表单标识（必填），如 PUR_PurchaseOrder
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  k3cli form --id PUR_PurchaseOrder
");
        }

        public static void ShowBillTypeHelp()
        {
            Console.WriteLine(@"用法: k3cli billtype [options]

选项:
  --form <identifier>       表单标识（可选），按表单查询单据类型列表
  --id <billTypeId>         单据类型 ID（可选），精确查询
  --keyword <keyword>       搜索关键词（可选），模糊搜索（编码/名称/描述）
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

注意:
  --form、--id、--keyword 至少需要指定一个

示例:
  # 查询表单关联的单据类型列表
  k3cli billtype --form PUR_PurchaseOrder

  # 精确查询指定单据类型
  k3cli billtype --id <billTypeId>

  # 模糊搜索单据类型
  k3cli billtype --keyword ""采购""
  k3cli billtype --keyword ""PO""

提示:
  1. 先用 fields 命令查询字段，获取 elementType=44 的字段的 lookUpObject 值
  2. 再用 billtype --id 命令查询该单据类型的详细信息（含描述）
  3. 输出字段包括：billTypeId、billFormId、number、name、description
");
        }

        public static void ShowBillStatusHelp()
        {
            Console.WriteLine(@"用法: k3cli billstatus [options]

选项:
  --form <identifier>       表单标识（必填），如 PUR_PurchaseOrder
  --field <fieldKey>        字段 Key（可选），精确匹配指定字段
  --keyword <keyword>       搜索关键词（可选），模糊搜索状态名称/值
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  # 查询表单所有单据状态字段的枚举值
  k3cli billstatus --form PUR_PurchaseOrder

  # 查询指定字段的单据状态
  k3cli billstatus --form PUR_PurchaseOrder --field FDocumentStatus

  # 模糊搜索状态值
  k3cli billstatus --form PUR_PurchaseOrder --keyword ""已审核""
  k3cli billstatus --form PUR_PurchaseOrder --keyword ""A""

提示:
  1. 先用 fields 命令查询字段，获取 elementType=40 的字段（BillStatusField）
  2. 再用 billstatus 命令查询该字段的单据状态枚举值
  3. 单据状态值存储在 XML 元数据中，不在单独的数据库表中
  4. 每个状态项包含：状态 ID、状态名称、状态值（如 A/B/C）
");
        }

        public static void ShowAssistantDataHelp()
        {
            Console.WriteLine(@"用法: k3cli assistantdata [options]

选项:
  --id <lookUpObjectId>     辅助资料 ID（必填），即字段的 LookUpObjectID
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  k3cli assistantdata --id <lookUpObjectId>
  k3cli assistantdata --id <lookUpObjectId> --pretty
");
        }

        public static void ShowEnumHelp()
        {
            Console.WriteLine(@"用法: k3cli enum [options]

选项:
  --id <enumTypeId>           枚举类型 ID（必填），即字段的 EnumType / FEnumType
  --connection, -c <id>       指定连接 ID
  --pretty                    格式化 JSON 输出

示例:
  k3cli enum --id <enumTypeId>
  k3cli enum --id <enumTypeId> --pretty

提示:
  1. 先用 fields 命令查询字段，获取 elementType=9 的字段的 enumType 值
  2. 再用 enum 命令查询该枚举类型的所有选项值
");
        }

        public static void ShowResolveHelp()
        {
            Console.WriteLine(@"用法: k3cli resolve [options]

选项:
  --id <objectId>             对象 ID（必填），即字段的 lookUpObject 值
  --connection, -c <id>       指定连接 ID
  --pretty                    格式化 JSON 输出

示例:
  # 解析 lookUpObject 对应的表单
  k3cli resolve --id 6099b796-9e56-434e-895e-a1628d12d4c2
  k3cli resolve --id 6099b796-9e56-434e-895e-a1628d12d4c2 --pretty

提示:
  1. 先用 fields 命令查询字段，获取 lookUpObject 值
  2. 再用 resolve 命令查询该 ID 对应的表单标识和名称
  3. 返回的 formId 即为表单标识，可直接用于 fields 命令
");
        }

        public static void ShowConnectionsHelp()
        {
            Console.WriteLine(@"用法: k3cli connections <subcommand> [options]

子命令:
  list                          列出所有连接
  add                           添加新连接
  test --id <id>                测试连接
  set-default --id <id>         设为默认连接

add 选项:
  --server <ip>                 SQL Server 地址（必填）
  --port <port>                 端口号（默认 1433）
  --db <database>               数据库名（必填）
  --user <username>             用户名（必填）
  --password <password>         密码
  --name <name>                 连接名称（默认使用数据库名）
  --default                     同时设为默认连接

示例:
  k3cli connections list
  k3cli connections add --server 192.168.1.100 --db AISC001 --user sa --password xxx --default
  k3cli connections test --id 1
  k3cli connections set-default --id 1
");
        }
    }
}
