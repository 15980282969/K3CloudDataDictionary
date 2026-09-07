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
  probe           探测物理表列（支持通配符）
  sql             生成 SQL 辅助信息（支持拆分表、sync 子命令）
  query           常用代码查询（用户许可等）
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
  k3cli connections update --id 1 --server 192.168.1.200
  k3cli connections delete --id 2 --force
  k3cli connections test --id 1

  # 探测物理表列（支持 * 通配符匹配拆分表）
  k3cli probe --table t_PUR_POOrderEntry --keyword BASE
  k3cli probe --table ""t_PUR_POOrderEntry*"" --keyword FDELIVERYDATE

  # 生成 SQL 辅助信息（自动识别拆分表）
  k3cli sql --form PUR_PurchaseOrder --fields ""累计收料数量,剩余收料数量""
  k3cli sql --form PUR_PurchaseOrder --fields ""FReceiveQty,FRemainReceiveQty""
  k3cli sql --form PUR_PurchaseOrder --fields ""最终确认交期""

  # 生成单据头→明细字段批量同步 SQL
  k3cli sql sync --form PUR_Requisition --field 追加采购原因
  k3cli sql sync --form SUB_SUBREQORDER --field 追加采购原因
");
        }

        public static void ShowFieldsHelp()
        {
            Console.WriteLine(@"用法: k3cli fields [options]

选项:
  --form <identifier>       表单标识（必填），如 PUR_PurchaseOrder
  --entity <key>            实体 Key（可选），如 FK_BillEntry
  --keyword <keyword>       字段搜索关键词（可选），支持多关键词（逗号/分号分隔）
  --exact, -e               精确匹配模式（完全相等，不区分大小写）
                            默认使用模糊匹配（包含关键词）
  --type <type>             字段类型过滤（可选）：
                            entry  - 只显示明细实体字段
                            head   - 只显示头部实体字段
                            oid    - 只显示OID相关字段
                            normal - 只显示普通业务字段
  --compare                 对比模式：对比单据头和明细体字段差异
                            显示仅单据头有、仅明细体有、两者都有的字段
  --physical                物理列模式：只输出物理列名列表（逗号分隔纯文本，
                            非 JSON，便于直接粘贴到 SQL 中）
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  # 查询表单所有字段
  k3cli fields --form PUR_PurchaseOrder

  # 查询指定实体的所有字段
  k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry

  # 模糊搜索字段（支持多关键词）
  k3cli fields --form PUR_PurchaseOrder --keyword ""物料""
  k3cli fields --form PUR_PurchaseOrder --keyword ""物料,供应商,日期""
  k3cli fields --form PUR_PurchaseOrder --keyword ""物料;供应商""

  # 精确搜索字段
  k3cli fields --form PUR_PurchaseOrder --keyword ""FMaterialId"" --exact

  # 按类型过滤字段
  k3cli fields --form PUR_PurchaseOrder --type entry
  k3cli fields --form PUR_PurchaseOrder --type head
  k3cli fields --form PUR_PurchaseOrder --type oid

  # 对比单据头和明细体字段差异
  k3cli fields --form PUR_Requisition --compare

  # 只输出物理列名（便于直接粘贴到 SQL 中）
  k3cli fields --form PRD_PickMtrl --entity FEntity --physical

提示:
  当 --entity 指定的实体 Key 不存在时，会自动返回该表单的可用实体列表。
  搜索关键词支持括号容错匹配（全角/半角括号等价）。
  多关键词之间是""或""关系，匹配任意一个即返回。
  --compare 模式可快速了解头/明细字段分布，便于字段同步操作。
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
  --limit <n>               最大返回结果数（默认 100）
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

  # 限制返回数量
  k3cli search --keyword ""物料"" --limit 50
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
  4. 每个状态项包含：状态值、状态名称、中文注释
  5. 常见状态注释：Z(暂存)、A(待审核)、C(已审核)、E(已驳回)
  6. 常用状态会标记 ← 常用 标识
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
  list                          列出所有连接（含上次成功连接时间）
  add                           添加新连接
  update --id <id>              修改已有连接的信息
  delete --id <id>              删除指定连接
  test [--id <id>]              测试连接（不指定 --id 则测试默认连接）
  get-connection-string [--id <id>]
                                导出完整连接字符串（含解密后的密码），
                                供 pyodbc/sqlcmd 等外部工具复用
  set-default --id <id>         设为默认连接

add 选项:
  --server <ip>                 SQL Server 地址（必填）
  --port <port>                 端口号（默认 1433）
  --db <database>               数据库名（必填）
  --user <username>             用户名（必填）
  --password <password>         密码
  --name <name>                 连接名称（默认使用数据库名）
  --default                     同时设为默认连接

update 选项:
  --id <id>                     要修改的连接 ID（必填）
  --server <ip>                 新的 SQL Server 地址
  --port <port>                 新的端口号
  --db <database>               新的数据库名
  --user <username>             新的用户名
  --password <password>         新的密码
  --name <name>                 新的连接名称

delete 选项:
  --id <id>                     要删除的连接 ID（必填）
  --force                       跳过删除确认

示例:
  k3cli connections list
  k3cli connections add --server 192.168.1.100 --db AISC001 --user sa --password xxx --default
  k3cli connections update --id 1 --server 192.168.1.200 --port 1434
  k3cli connections update --id 1 --name ""生产环境""
  k3cli connections delete --id 2
  k3cli connections delete --id 2 --force
  k3cli connections test
  k3cli connections test --id 1
  k3cli connections get-connection-string
  k3cli connections get-connection-string --id 1
  k3cli connections set-default --id 1

说明:
  - 执行查询命令前会自动检测连接可达性
  - 连接成功后会记录时间戳，可通过 list 查看
  - update 仅更新提供的参数，未提供的字段保持不变
  - delete 默认会提示确认，使用 --force 可跳过
");
        }

        public static void ShowProbeHelp()
        {
            Console.WriteLine(@"用法: k3cli probe [options]

选项:
  --table <tableName>       物理表名（必填），支持 * 通配符
  --keyword <keyword>       列名关键词（可选），模糊匹配
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

说明:
  当字典中查不到某个字段时，可使用 probe 命令直接查询物理表的列信息。
  该命令通过 SQL Server 的 sys.columns 系统视图查询，不受字典覆盖范围限制。
  表名支持 * 通配符，可同时匹配主表和所有拆分表（如 _D、_L 后缀表）。

示例:
  # 单表探测
  k3cli probe --table t_PUR_POOrderEntry --keyword BASE

  # 批量匹配所有拆分表
  k3cli probe --table ""t_PUR_POOrderEntry*"" --keyword FDELIVERYDATE
");
        }

        public static void ShowSqlHelp()
        {
            Console.WriteLine(@"用法: k3cli sql [options]

选项:
  --form <identifier>       表单标识（必填），如 PUR_PurchaseOrder
  --fields <field1,field2>  逗号分隔的字段列表（必填），支持中文名或英文 key
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

说明:
  根据指定的表单和字段，自动生成 SQL 辅助信息，包括：
  - 单据头和明细体的物理表名
  - 目标字段的物理列名（含拆分表信息）
  - 行号字段（FSeq）和单据编号字段
  - JOIN 条件（自动包含拆分表 JOIN）
  - LK 关联表信息（自动检测）
  - 可直接使用的 SELECT 和 UPDATE SQL 模板

示例:
  # 基本用法
  k3cli sql --form PUR_PurchaseOrder --fields ""累计收料数量,剩余收料数量""
  k3cli sql --form PUR_PurchaseOrder --fields ""FReceiveQty,FRemainReceiveQty""

  # 拆分表字段（自动 JOIN 拆分表）
  k3cli sql --form PUR_PurchaseOrder --fields ""最终确认交期""

提示:
  1. 当字段有 splitSuffix 时，SQL 会自动使用拆分表并生成正确的 JOIN
  2. LK 关联表信息会自动检测，超时后降级提示
  3. 生成的 SQL 仅供参考，请在测试环境验证后使用

子命令:
  sync                      生成单据头→明细字段批量同步 SQL
                            用法: k3cli sql sync --form <identifier> --field <keyword>
                            示例: k3cli sql sync --form PUR_Requisition --field 追加采购原因
                            详情: k3cli sql sync --help
");
        }

        public static void ShowSqlSyncHelp()
        {
            Console.WriteLine(@"用法: k3cli sql sync [options]

说明:
  生成单据头→明细字段批量同步 SQL。
  将单据头指定字段的值同步到明细体对应字段，仅当单据头不为空且明细为空时更新。

选项:
  --form <identifier>       表单标识（必填），如 PUR_Requisition
  --field <keyword>         字段关键词（必填），支持中文名、字段Key、物理列名
  --connection, -c <id>     指定连接 ID
  --pretty                  格式化 JSON 输出

示例:
  # 同步采购申请单的追加采购原因
  k3cli sql sync --form PUR_Requisition --field 追加采购原因

  # 同步委外订单的追加采购原因
  k3cli sql sync --form SUB_SUBREQORDER --field 追加采购原因

  # 使用字段 Key
  k3cli sql sync --form PUR_Requisition --field F_ROV_ZJCGYY

提示:
  1. 命令会自动识别单据头和明细体中的同名字段，生成 UPDATE SQL
  2. 输出包含 previewSql 和 updateSql
  3. 建议先执行 previewSql 确认数据无误后再执行 updateSql
  4. 若头/明细字段物理列名不同，需手动调整生成的 SQL
");
        }

        public static void ShowQueryHelp()
        {
            Console.WriteLine(@"用法: k3cli query <queryName> [options]
      k3cli query --sql ""<SELECT 语句>"" [--params ""值1,值2""] [options]

说明:
  常用代码查询功能，快速调用预定义 SQL 返回业务数据。
  --sql 模式可直接执行任意只读 SELECT（写操作一律被拒绝），
  --params 中的值按位置映射为 @p1、@p2... 实现参数化查询。

可用查询:
  list                    列出所有可用查询
  user-licenses           查询用户许可分配（组织、用户、许可分组）
  blocking                查询数据库阻塞/死锁进程信息
  mo-pick-summary         按生产订单查询领料汇总（物料、仓库、单位分组）
  mo-return-summary       按生产订单查询退料汇总（物料、仓库、单位分组）
  mo-instock-summary      按生产订单查询入库汇总（物料、仓库、单位分组）
  bill-by-no              按单据编号查询任意表单的头表与明细表数据

user-licenses 选项:
  --org <keyword>         按组织名称模糊过滤
  --user <keyword>        按用户名称模糊过滤
  --connection, -c <id>   指定连接 ID
  --pretty                格式化 JSON 输出

blocking 选项:
  --connection, -c <id>   指定连接 ID
  --pretty                格式化 JSON 输出

mo-pick-summary / mo-return-summary / mo-instock-summary 选项:
  --mo <生产订单号>        按生产订单号模糊过滤（可选，不传则汇总全部）

bill-by-no 选项:
  --form <表单标识>        表单标识（必填），如 STK_MisDelivery
  --no <单据编号>          单据编号（必填）

--sql 选项:
  --sql <SELECT 语句>     要执行的只读 SQL（必须以 SELECT 开头，仅允许单语句）
  --params <值1,值2>      参数值，逗号分隔，按位置映射为 @p1、@p2...
  --timeout <秒>          查询超时时间（默认 60 秒）

示例:
  # 列出所有可用查询
  k3cli query list

  # 查询所有用户许可分配
  k3cli query user-licenses

  # 按组织过滤
  k3cli query user-licenses --org ""荣耀""

  # 按用户过滤
  k3cli query user-licenses --user ""Harrison""

  # 组合过滤
  k3cli query user-licenses --org ""荣耀"" --user ""Harrison"" --pretty

  # 查询数据库阻塞/死锁进程
  k3cli query blocking --pretty

  # 直接执行只读 SQL
  k3cli query --sql ""SELECT FBillNo, FDate FROM T_PRD_PICKMTRL WHERE FID = 123"" --pretty

  # 参数化查询（值按位置映射为 @p1）
  k3cli query --sql ""SELECT * FROM T_PRD_PICKMTRL WHERE FBillNo = @p1"" --params ""SBKF00086786""

  # 按生产订单查领料/退料/入库汇总
  k3cli query mo-pick-summary --mo ""0001-W260677"" --pretty
  k3cli query mo-return-summary --mo ""0001-W260677"" --pretty
  k3cli query mo-instock-summary --mo ""0001-W260677"" --pretty

  # 按单据编号查任意表单
  k3cli query bill-by-no --form STK_MisDelivery --no ""QTCK121553"" --pretty
");
        }
    }
}
