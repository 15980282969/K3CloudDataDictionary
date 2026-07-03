# K3Cloud CLI 使用案例文档

## 案例：通过 lookUpObject 查询关联表单的完整流程

### 场景说明

当查询某个字段时，发现其 `elementType` 为 `13`（基础资料）或 `30`（辅助资料），且 `lookUpObject` 字段有值时，说明该字段的可选值来自另一个表单。需要通过以下三步流程查询关联表单的详细信息。

### 使用流程

#### 第一步：通过字段名称查找获取 lookUpObject ID

```bash
k3cli fields --form PUR_PurchaseOrder --keyword "供应商" --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "fields",
  "data": [
    {
      "formName": "采购订单",
      "entityName": "基本信息",
      "table": "t_PUR_POOrder",
      "key": "FSupplierId",
      "name": "供应商",
      "fieldName": "FSUPPLIERID",
      "propertyName": "SupplierId",
      "elementType": "13",
      "elementTypeName": "基础资料",
      "tagName": "BaseDataField",
      "lookUpObject": "6099b796-9e56-434e-895e-a1628d12d4c2",
      "enumType": "",
      "splitSuffix": "",
      "splitDescription": "",
      "updateActionCount": 13
    }
  ],
  "count": 1
}
```

**关键信息**：
- `elementType`: `"13"` → 基础资料字段
- `lookUpObject`: `"6099b796-9e56-434e-895e-a1628d12d4c2"` → 关联对象的 ID

#### 第二步：使用 resolve 命令解析 lookUpObject ID 得到目标表单 ID

```bash
k3cli resolve --id 6099b796-9e56-434e-895e-a1628d12d4c2 --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "resolve",
  "data": [
    {
      "lookupId": "6099b796-9e56-434e-895e-a1628d12d4c2",
      "formId": "BD_Supplier",
      "tableName": "t_BD_Supplier",
      "pkFieldName": "FSupplierId",
      "orgFieldName": "FUseOrgId"
    }
  ],
  "count": 1
}
```

**关键信息**：
- `formId`: `"BD_Supplier"` → 供应商基础资料的表单标识
- `tableName`: `"t_BD_Supplier"` → 对应的数据库表名
- `pkFieldName`: `"FSupplierId"` → 主键字段名

#### 第三步：查询目标表单的所有字段信息

```bash
k3cli fields --form BD_Supplier --pretty
```

输出示例（部分）：

```json
{
  "success": true,
  "command": "fields",
  "data": [
    {
      "formName": "供应商",
      "entityName": "基本信息",
      "table": "t_BD_Supplier",
      "key": "FNumber",
      "name": "编码",
      "fieldName": "FNUMBER",
      "propertyName": "Number",
      "elementType": "1",
      "elementTypeName": "文本",
      "tagName": "TextField",
      "lookUpObject": "",
      "enumType": "",
      "splitSuffix": "",
      "splitDescription": "",
      "updateActionCount": 0
    },
    {
      "formName": "供应商",
      "entityName": "基本信息",
      "table": "t_BD_Supplier",
      "key": "FName",
      "name": "名称",
      "fieldName": "FNAME",
      "propertyName": "Name",
      "elementType": "1",
      "elementTypeName": "文本",
      "tagName": "TextField",
      "lookUpObject": "",
      "enumType": "",
      "splitSuffix": "",
      "splitDescription": "",
      "updateActionCount": 0
    }
  ],
  "count": 20
}
```

### 适用场景

| elementType | tagName | lookUpObject 含义 | 查询目标 |
|---|---|---|---|
| 13 | BaseDataField | 基础资料对象 ID | 基础资料表单（如供应商、物料、客户） |
| 30 | AssistantField | 辅助资料对象 ID | 辅助资料选项列表 |

### 完整命令链

```bash
# 查找采购订单中的"供应商"字段
k3cli fields --form PUR_PurchaseOrder --keyword "供应商" --pretty

# 解析 lookUpObject 得到表单标识 BD_Supplier
k3cli resolve --id 6099b796-9e56-434e-895e-a1628d12d4c2 --pretty

# 查询供应商基础资料的所有字段
k3cli fields --form BD_Supplier --pretty
```

### 注意事项

1. `lookUpObject` 为空时，表示该字段没有关联其他表单，无需执行 resolve 步骤
2. `elementType=13` 时，resolve 返回的是基础资料表单标识，可继续用 `fields` 命令查询
3. `elementType=30` 时，resolve 返回的是辅助资料对象，可用 `assistantdata` 命令查询选项列表
4. resolve 命令的 `formId` 返回值为表单标识（如 `BD_Supplier`），可直接作为 `fields --form` 的参数

---

## 案例：查询单据类型

### 场景说明

当字段 `elementType=44`（单据类型字段）时，该字段关联的是单据类型列表。`billtype` 命令支持三种查询模式：按表单查列表、按 ID 查详情、按关键词模糊搜索。

### 使用流程

#### 模式一：按表单查询单据类型列表

```bash
k3cli billtype --form PUR_PurchaseOrder --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "billtype",
  "data": [
    {
      "billTypeId": "83d822ca3e374b4ab01e5dd46a0062bd",
      "billFormId": "PUR_PurchaseOrder",
      "number": "CGDD01_SYS",
      "name": "采购订单",
      "description": "标准采购订单的单据类型"
    },
    {
      "billTypeId": "6d01d059713d42a28bb976c90a121142",
      "billFormId": "PUR_PurchaseOrder",
      "number": "CGDD02_SYS",
      "name": "委外订单",
      "description": "标准委外订单的单据类型"
    }
  ],
  "count": 2
}
```

#### 模式二：按 ID 精确查询单据类型详情

```bash
k3cli billtype --id 83d822ca3e374b4ab01e5dd46a0062bd --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "billtype",
  "data": [
    {
      "billTypeId": "83d822ca3e374b4ab01e5dd46a0062bd",
      "billFormId": "PUR_PurchaseOrder",
      "number": "CGDD01_SYS",
      "name": "采购订单",
      "description": "标准采购订单的单据类型"
    }
  ],
  "count": 1
}
```

#### 模式三：模糊搜索单据类型

```bash
k3cli billtype --keyword "采购" --pretty
```

---

## 案例：查询辅助资料列表

### 场景说明

当字段 `elementType=30`（辅助资料字段）时，`lookUpObject` 指向辅助资料对象。需要查询该辅助资料的所有可选值。

### 使用流程

#### 第一步：查找辅助资料字段

```bash
k3cli fields --form PUR_PurchaseOrder --keyword "辅助" --pretty
```

找到 `elementType=30` 且 `lookUpObject` 有值的字段。

#### 第二步：查询辅助资料选项列表

```bash
k3cli assistantdata --id <lookUpObject值> --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "assistantdata",
  "data": [
    {
      "id": "辅助资料ID",
      "number": "编码",
      "name": "辅助资料名称",
      "entryId": "条目ID",
      "entryNumber": "条目编码",
      "dataValue": "数据值"
    }
  ],
  "count": 1
}
```

---

## 案例：查询下拉列表枚举值

### 场景说明

当字段 `elementType=9`（下拉列表字段）时，`enumType` 字段存储枚举类型 ID。需要查询该枚举的所有选项值。

### 使用流程

#### 第一步：查找下拉列表字段

```bash
k3cli fields --form PUR_PurchaseOrder --keyword "合同类型" --pretty
```

找到 `elementType=9` 且 `enumType` 有值的字段。

#### 第二步：查询枚举值列表

```bash
k3cli enum --id <enumType值> --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "enum",
  "data": [
    {
      "id": "枚举类型ID",
      "name": "枚举名称",
      "value": "0",
      "enumId": "枚举项ID",
      "caption": "否"
    },
    {
      "id": "枚举类型ID",
      "name": "枚举名称",
      "value": "1",
      "enumId": "枚举项ID",
      "caption": "是"
    }
  ],
  "count": 2
}
```

---

## 案例：从搜索表到查询字段

### 场景说明

当只知道表单名称的关键词（如"采购"），不知道表单标识时，需要先通过 `search` 命令搜索表单，再用 `fields` 命令查询字段。

### 使用流程

#### 第一步：搜索表单

```bash
k3cli search --keyword "采购订单" --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "search",
  "data": [
    {
      "formId": "PUR_PurchaseOrder",
      "formIdentifier": "PUR_PurchaseOrder",
      "formName": "采购订单",
      "entityKey": "FBillHead",
      "entityName": "基本信息",
      "table": "t_PUR_POOrder",
      "elementType": "单据头",
      "fieldCount": 0
    },
    {
      "formId": "PUR_PurchaseOrder",
      "formIdentifier": "PUR_PurchaseOrder",
      "formName": "采购订单",
      "entityKey": "FK_BillEntry",
      "entityName": "明细信息",
      "table": "t_PUR_POOrderEntry",
      "elementType": "单据体",
      "fieldCount": 0
    }
  ],
  "count": 2
}
```

**关键信息**：
- `formIdentifier`: `"PUR_PurchaseOrder"` → 表单标识，用于后续 `fields` 命令
- `entityKey`: `"FBillHead"` / `"FK_BillEntry"` → 实体 Key，用于 `fields --entity` 参数

#### 第二步：查询表单所有字段

```bash
k3cli fields --form PUR_PurchaseOrder --pretty
```

#### 第三步：查询指定实体的字段

```bash
k3cli fields --form PUR_PurchaseOrder --entity FBillHead --pretty
```

---

## 案例：根据表+实体查询子项明细字段

### 场景说明

已知数据库表名和实体 Key，需要查询该实体下的所有字段明细。这是最常见的精确查询场景。

### 使用流程

#### 第一步：通过表名搜索定位表单和实体

```bash
k3cli search --keyword "POOrderEntry" --pretty
```

#### 第二步：查询该实体的所有字段

```bash
k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry --pretty
```

#### 第三步：在实体内精确搜索字段

```bash
# 模糊搜索
k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry --keyword "物料" --pretty

# 精确搜索
k3cli fields --form PUR_PurchaseOrder --entity FK_BillEntry --keyword "FMaterialId" --exact --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "fields",
  "data": [
    {
      "formName": "采购订单",
      "entityName": "明细信息",
      "table": "t_PUR_POOrderEntry",
      "key": "FMaterialId",
      "name": "物料编码",
      "fieldName": "FMATERIALID",
      "propertyName": "MaterialId",
      "elementType": "13",
      "elementTypeName": "基础资料",
      "tagName": "BaseDataField",
      "lookUpObject": "624b39cf-5504-42e0-9124-7d75e64a05f1",
      "enumType": "",
      "splitSuffix": "",
      "splitDescription": "",
      "updateActionCount": 11
    }
  ],
  "count": 1
}
```

---

## 案例：查询单据状态字段枚举值

### 场景说明

当字段 `elementType=40`（单据状态字段）时，状态值存储在 XML 元数据中。需要查询该字段的所有状态选项。

### 使用流程

#### 第一步：查找单据状态字段

```bash
k3cli fields --form PUR_PurchaseOrder --keyword "单据状态" --pretty
```

找到 `elementType=40` 的字段。

#### 第二步：查询单据状态枚举值

```bash
# 查询所有单据状态字段
k3cli billstatus --form PUR_PurchaseOrder --pretty

# 查询指定字段的状态值
k3cli billstatus --form PUR_PurchaseOrder --field FDocumentStatus --pretty

# 模糊搜索状态值
k3cli billstatus --form PUR_PurchaseOrder --keyword "已审核" --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "billstatus",
  "data": [
    {
      "formId": "PUR_PurchaseOrder",
      "formName": "采购订单",
      "entityName": "基本信息",
      "table": "t_PUR_POOrder",
      "fieldKey": "FDocumentStatus",
      "fieldName": "单据状态",
      "dbFieldName": "FDOCUMENTSTATUS",
      "propertyName": "DocumentStatus",
      "elementType": "40",
      "elementTypeName": "BillStatusField",
      "statusItems": [
        { "value": "Z", "name": "暂存" },
        { "value": "A", "name": "创建" },
        { "value": "B", "name": "已审核" },
        { "value": "C", "name": "已反审" },
        { "value": "D", "name": "重新审核" }
      ]
    }
  ],
  "count": 1
}
```

---

## 案例：括号/符号容错搜索

### 场景说明

字段名称中可能包含全角括号`（）`或半角括号`()`，用户输入时可能使用任意一种。容错搜索会自动归一化这些符号，使两种写法都能匹配到同一字段。

### 使用示例

以下三种写法等价，都能匹配到"累计收料数量（基本）"字段：

```bash
# 半角括号
k3cli fields --form PUR_PurchaseOrder --keyword "收料数量(基本)" --pretty

# 全角括号
k3cli fields --form PUR_PurchaseOrder --keyword "收料数量（基本）" --pretty

# 无括号（也能匹配）
k3cli fields --form PUR_PurchaseOrder --keyword "收料数量基本" --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "fields",
  "data": [
    {
      "formName": "采购订单",
      "entityName": "明细信息",
      "entityKey": "FPOORDERENTRY",
      "table": "t_PUR_POOrderEntry",
      "key": "FBASERECEIVEQTY",
      "name": "累计收料数量(基本)",
      "fieldName": "FBASERECEIVEQTY",
      "elementType": "47",
      "elementTypeName": "基本单位数量",
      "tagName": "BaseQtyField"
    }
  ],
  "count": 1
}
```

### 归一化规则

| 输入 | 归一化后 | 匹配效果 |
|------|---------|---------|
| `收料数量(基本)` | `收料数量基本` | 匹配 |
| `收料数量（基本）` | `收料数量基本` | 匹配 |
| `收料数量 基本` | `收料数量基本` | 匹配 |
| `收料数量( 基本 )` | `收料数量基本` | 匹配 |

---

## 案例：实体 Key 错误自动提示

### 场景说明

当使用 `fields --entity` 指定了错误的实体 Key 时，系统不会返回空结果，而是自动列出该表单所有可用的实体，帮助用户快速定位正确的实体标识。

### 使用示例

```bash
k3cli fields --form PUR_PurchaseOrder --entity WRONG_ENTITY --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "fields",
  "data": [
    {
      "hint": "entity_not_found",
      "message": "未找到实体 'WRONG_ENTITY' 的字段。该表单包含以下实体：",
      "availableEntities": [
        { "entityKey": "", "entityName": "基本信息", "table": "t_PUR_POOrder" },
        { "entityKey": "FPOOrderEntry", "entityName": "明细信息", "table": "t_PUR_POOrderEntry" },
        { "entityKey": "FPOOrderFinance", "entityName": "财务信息", "table": "T_PUR_POORDERFIN" }
      ]
    }
  ],
  "count": 1
}
```

### 使用提示

1. 看到 `hint: "entity_not_found"` 后，从 `availableEntities` 中选择正确的 `entityKey`
2. 用正确的 `entityKey` 重新执行 `fields` 命令
3. 也可以先用 `search --keyword "采购订单"` 查看表单的实体列表

---

## 案例：探测物理表列（probe 命令）

### 场景说明

当字典中查不到某个字段时（可能是自定义字段或字典未收录的字段），可使用 `probe` 命令直接查询 SQL Server 物理表的列信息。该命令通过 `sys.columns` 系统视图查询，不受字典覆盖范围限制。

### 使用示例

```bash
# 查询物理表的所有列
k3cli probe --table t_PUR_POOrderEntry --pretty

# 按关键词过滤列名
k3cli probe --table t_PUR_POOrderEntry --keyword BASE --pretty

# 查找特定列
k3cli probe --table t_PUR_POOrderEntry --keyword FBASEREMAIN --pretty
```

输出示例（`--keyword BASE`）：

```json
{
  "success": true,
  "command": "probe",
  "data": [
    {
      "columnName": "FBASEUNITID",
      "dataType": "int",
      "maxLength": 4,
      "precision": 10,
      "scale": 0,
      "isNullable": false
    },
    {
      "columnName": "FBASEUNITQTY",
      "dataType": "decimal",
      "maxLength": 13,
      "precision": 23,
      "scale": 10,
      "isNullable": false
    },
    {
      "columnName": "FBASECONSUMESUMQTY",
      "dataType": "decimal",
      "maxLength": 13,
      "precision": 23,
      "scale": 10,
      "isNullable": true
    }
  ],
  "count": 3
}
```

### 输出字段说明

| 字段 | 含义 |
|------|------|
| `columnName` | 物理列名 |
| `dataType` | 数据类型（int, decimal, nvarchar 等） |
| `maxLength` | 最大长度（字节） |
| `precision` | 精度（数值类型） |
| `scale` | 小数位数（数值类型） |
| `isNullable` | 是否允许 NULL |

### 典型使用场景

1. 字典中查不到某个字段 → 用 `probe` 确认物理表中是否存在该列
2. 需要确认字段的数据类型和精度 → 用 `probe` 查看列定义
3. 查找衍生字段（如基本单位字段）→ 用 `probe --keyword BASE` 批量查找

---

## 案例：生成 SQL 辅助信息（sql 命令）

### 场景说明

根据表单标识和字段列表，自动生成 SQL 辅助信息，包括物理表名、列名、JOIN 条件、SELECT 和 UPDATE 模板。适用于需要手写 SQL 查询或更新业务数据的场景。

> **安全说明**：`sql` 命令仅生成 SQL 模板文本，不会执行任何写操作。输出的 SQL 需要复制到数据库管理工具中手动执行。

### 使用示例

```bash
# 按中文名称查询
k3cli sql --form PUR_PurchaseOrder --fields "物料编码,累计收料数量" --pretty

# 按英文 Key 查询
k3cli sql --form PUR_PurchaseOrder --fields "FMaterialId,FReceiveBaseQty" --pretty

# 混合使用（支持中英文逗号分隔）
k3cli sql --form PUR_PurchaseOrder --fields "FMaterialId，累计收料数量" --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "sql",
  "data": {
    "formIdentifier": "PUR_PurchaseOrder",
    "formName": "采购订单",
    "tables": [
      { "alias": "h", "table": "t_PUR_POOrder", "entityName": "基本信息", "type": "单据头" },
      { "alias": "e", "table": "t_PUR_POOrderEntry", "entityName": "明细信息", "type": "明细体" }
    ],
    "seqField": "(未找到行号字段)",
    "billNoField": "FBILLNO",
    "matchedFields": [
      {
        "searchKeyword": "FMaterialId",
        "name": "物料编码",
        "fieldName": "FMATERIALID",
        "table": "t_PUR_POOrderEntry",
        "elementType": "13",
        "elementTypeName": "基础资料"
      }
    ],
    "unmatchedKeywords": ["FReceiveBaseQty"],
    "hint": "以下关键词未匹配到字段，可能是字典未收录。请使用 probe 命令探测物理表列。",
    "selectSql": "SELECT\n    e.FMATERIALID AS [物料编码]\nFROM t_PUR_POOrder h\nINNER JOIN t_PUR_POOrderEntry e ON e.FID = h.FID\nWHERE h.FBILLNO = @BillNo;",
    "updateSql": "UPDATE t_PUR_POOrderEntry\nSET\n    FMATERIALID = @NewValue_FMATERIALID\nWHERE FEntryID = (\n    SELECT e.FEntryID\n    FROM t_PUR_POOrderEntry e\n    INNER JOIN t_PUR_POOrder h ON e.FID = h.FID\n    WHERE h.FBILLNO = @BillNo\n);"
  }
}
```

### 输出字段说明

| 字段 | 含义 |
|------|------|
| `formIdentifier` | 表单标识 |
| `formName` | 表单名称 |
| `tables` | 单据头和明细体的物理表信息（含别名、表名、实体Key） |
| `seqField` | 行号字段（用于明细行定位） |
| `billNoField` | 单据编号字段（用于 WHERE 条件） |
| `matchedFields` | 匹配到的字段列表（含物理列名、表名、元素类型） |
| `unmatchedKeywords` | 未匹配的关键词（可能是字典未收录） |
| `selectSql` | 可直接使用的 SELECT 模板 |
| `updateSql` | 可直接使用的 UPDATE 模板 |

### 典型使用流程

```
1. 用 sql 命令生成 SQL 模板
   k3cli sql --form PUR_PurchaseOrder --fields "物料编码" --pretty

2. 如果有未匹配字段，用 probe 命令探测物理表
   k3cli probe --table t_PUR_POOrderEntry --keyword ReceiveBaseQty --pretty

3. 将 selectSql/updateSql 复制到 SSMS 中，替换参数值后执行
```

### 注意事项

1. `sql` 命令**仅生成文本**，不会执行任何 SQL
2. 未匹配的关键词会提示使用 `probe` 命令进一步探测
3. SELECT 模板使用 `@BillNo` 参数，执行时需替换为实际单据编号
4. UPDATE 模板使用子查询定位行，避免误更新

---


---

## 案例：拆分表（Split Table）支持

### 场景说明

金蝶 K3Cloud 中，部分字段会被拆分到后缀表中（如 `_D` 表示日期拆分表、`_L` 表示长文本拆分表）。`fields` 命令输出中的 `splitTable` 字段会直接显示完整的拆分表名，`sql` 命令会自动生成正确的拆分表 JOIN 语句。

### fields 命令输出中的 splitTable 字段

`ash
k3cli fields --form PUR_PurchaseOrder --keyword "最终确认交期" --pretty
`

输出示例：

`json
{
  "data": [
    {
      "table": "t_PUR_POOrderEntry",
      "splitSuffix": "D",
      "splitTable": "t_PUR_POOrderEntry_D",
      "fieldName": "FDELIVERYDATE",
      "name": "最终确认交期"
    }
  ]
}
`

**字段说明**：

| 字段 | 含义 |
|------|------|
| `table` | 实体主表名 |
| `splitSuffix` | 拆分后缀（如 `D`、`L`），空表示无拆分 |
| `splitTable` | 完整拆分表名（`{主表}_{后缀}`），空表示无拆分 |

### sql 命令自动处理拆分表

`ash
k3cli sql --form PUR_PurchaseOrder --fields "最终确认交期" --pretty
`

输出中的 SQL 模板会自动包含拆分表 JOIN：

`sql
-- SELECT 模板（自动 JOIN 拆分表）
SELECT
    p_d.FDELIVERYDATE AS [最终确认交期]
FROM t_PUR_POOrder h
INNER JOIN t_PUR_POOrderEntry e ON e.FID = h.FID
INNER JOIN t_PUR_POOrderEntry_D p_d ON p_d.FENTRYID = e.FENTRYID
WHERE h.FBILLNO = @BillNo AND e.FSEQ = @Seq;

-- UPDATE 模板（直接更新拆分表）
UPDATE t_PUR_POOrderEntry_D
SET FDELIVERYDATE = @NewValue_FDELIVERYDATE
WHERE FENTRYID = (
    SELECT e.FENTRYID
    FROM t_PUR_POOrderEntry e
    INNER JOIN t_PUR_POOrder h ON e.FID = h.FID
    WHERE h.FBILLNO = @BillNo AND e.FSEQ = @Seq
);
`

**处理逻辑**：
1. 检测到字段的 `splitSuffix` 不为空
2. 物理表名使用 `{实体主表}_{splitSuffix}`
3. 自动生成 `INNER JOIN {拆分表} {别名} ON {别名}.FENTRYID = e.FENTRYID`
4. 同一拆分表的多个字段只 JOIN 一次

---

## 案例：probe 命令通配符匹配

### 场景说明

查找拆分表时需要手动猜测表名后缀（`_D`、`_L` 等），效率低。`probe` 命令支持 `*` 通配符，可同时匹配多个表。

### 使用示例

`ash
# 单表探测（字段不在主表中，返回空）
k3cli probe --table t_PUR_POOrderEntry --keyword FDELIVERYDATE

# 通配符匹配（找到拆分表中的字段）
k3cli probe --table "t_PUR_POOrderEntry*" --keyword FDELIVERYDATE --pretty
`

输出示例：

`json
{
  "success": true,
  "command": "probe",
  "data": [
    {
      "table": "T_PUR_POORDERENTRY_D",
      "columnName": "FDELIVERYDATE",
      "dataType": "datetime",
      "maxLength": 8,
      "precision": 23,
      "scale": 3,
      "isNullable": true
    },
    {
      "table": "T_pur_POORDERENTRY_D_0212BACK",
      "columnName": "FDELIVERYDATE",
      "dataType": "datetime",
      "maxLength": 8,
      "precision": 23,
      "scale": 3,
      "isNullable": true
    }
  ],
  "count": 2
}
`

### 通配符规则

| 模式 | 匹配范围 | 示例 |
|------|---------|------|
| `t_PUR_POOrderEntry` | 仅主表 | 精确匹配 |
| `t_PUR_POOrderEntry*` | 主表 + 所有拆分表 | 包含 `_D`、`_L` 等后缀表 |
| `t_PUR_PO*` | 所有采购订单相关表 | 更广泛的匹配 |

## 案例：LK 关联表自动检测

### 场景说明

在金蝶云星空中，单据转换后上下游单据之间的关系存储在 **LK 表（关联关系表）** 中。LK 表命名规则为 `{实体表名}_LK`，例如 `t_PUR_POOrderEntry_LK`。

`sql` 命令会自动检测表单实体是否存在对应的 LK 表，并在输出中提供关联信息。

### LK 表核心字段

| 字段 | 说明 |
|------|------|
| `FENTRYID` | 关联单据关联配置中单据体实体的主键 |
| `FRuleID` | 单据转换规则 ID |
| `FSBillID` | 源单的单据头 ID |
| `FSID` | 源单明细 ID 或单据头 ID（取决于配置） |

### 使用示例

```bash
k3cli sql --form PUR_PurchaseOrder --fields "FMaterialId"
```

输出示例（包含 LK 表信息）：

```json
{
  "success": true,
  "command": "sql",
  "data": {
    "formIdentifier": "PUR_PurchaseOrder",
    "formName": "采购订单",
    "tables": [
      { "alias": "h", "table": "t_PUR_POOrder", "entityName": "基本信息", "type": "单据头" },
      { "alias": "e", "table": "t_PUR_POOrderEntry", "entityName": "明细信息", "type": "明细体" }
    ],
    "lkTables": [
      {
        "lkTable": "t_PUR_POOrderEntry_LK",
        "entityTable": "t_PUR_POOrderEntry",
        "entityKey": "FPOOrderEntry",
        "entityName": "明细信息",
        "joinCondition": "lk.FENTRYID = e.FEntryID",
        "sourceJoinCondition": "lk.FSBILLID = src.FID AND lk.FSID = src.FEntryID",
        "description": "t_PUR_POOrderEntry 的关联关系表，用于追溯上下游单据关系"
      }
    ],
    "lkHint": "发现 1 个 LK 关联表。LK 表用于存储单据转换后的上下游关联关系，可通过 FSBILLID（源单单据头ID）和 FSID（源单明细ID）追溯源单。"
  }
}
```

### LK 表关联查询示例

根据 `sql` 命令输出的 LK 表信息，可以构建上下游单据追溯查询：

```sql
-- 查询收料通知单及其关联的采购订单
SELECT
    T0.FBILLNO AS 收料通知单号,
    T3.FBILLNO AS 采购订单号,
    T1.FMATERIALID AS 物料
FROM T_PUR_Receive T0
LEFT JOIN T_PUR_ReceiveEntry T1 ON T1.FID = T0.FID
LEFT JOIN T_PUR_ReceiveEntry_LK T2 ON T2.FENTRYID = T1.FENTRYID
LEFT JOIN t_PUR_POOrderEntry T3 ON T3.FENTRYID = T2.FSID AND T3.FID = T2.FSBILLID
WHERE T0.FBILLNO = @BillNo;
```

**关联逻辑**：
1. `T_PUR_Receive` → `T_PUR_ReceiveEntry`：通过 `FID` 关联单据头和明细
2. `T_PUR_ReceiveEntry` → `T_PUR_ReceiveEntry_LK`：通过 `FENTRYID = FENTRYID` 关联 LK 表
3. `T_PUR_ReceiveEntry_LK` → `t_PUR_POOrderEntry`：通过 `FSBILLID = FID AND FSID = FENTRYID` 关联源单

### 输出字段说明

| 字段 | 含义 |
|------|------|
| `lkTable` | LK 关联表名 |
| `entityTable` | 对应的实体表名 |
| `entityKey` | 实体 Key |
| `entityName` | 实体名称 |
| `joinCondition` | LK 表与目标单的 JOIN 条件 |
| `sourceJoinCondition` | LK 表与源单的 JOIN 条件 |
| `description` | LK 表用途说明 |

---

## 案例：实体主键字段和序号字段

### 场景说明

每个实体（单据头/单据体）都有主键字段和可选的序号字段。这些信息对于编写 SQL 查询和更新语句至关重要。

- **entryPkFieldName**：实体的主键字段名，用于定位记录
- **seqFieldKey**：分录实体的序号字段名，用于标识行顺序

### 主键字段默认规则

| 情况 | entryPkFieldName 值 |
|------|---------------------|
| XML 中有 `<EntryPkFieldName>` 标签 | 使用标签值（如 `FEntryID`、`FDetailId`） |
| 单据头（主表），无标签 | 默认 `FID` |
| 分录实体（子表），无标签 | 默认 `FEntryId` |

### 序号字段说明

| 情况 | seqFieldKey 值 |
|------|----------------|
| XML 中有 `<SeqFieldKey>` 标签 | 使用标签值（如 `FSeq`、`FSEQ`） |
| 单据头（主表） | 通常为空 `""` |
| 分录实体，无标签 | 通常为空 `""` |

### 使用示例：通过 form 命令查看实体的主键和序号字段

```bash
k3cli form --id PUR_PurchaseOrder --pretty
```

输出示例（entities 部分）：

```json
{
  "entities": [
    {
      "entityKey": "",
      "entityName": "基本信息",
      "table": "t_PUR_POOrder",
      "entryName": "POOrder",
      "elementType": "34",
      "seqFieldKey": "",
      "entryPkFieldName": "FID",
      "serviceRuleCount": 23,
      "updateActionCount": 0
    },
    {
      "entityKey": "FPOOrderEntry",
      "entityName": "明细信息",
      "table": "t_PUR_POOrderEntry",
      "entryName": "POOrderEntry",
      "elementType": "35",
      "seqFieldKey": "FSeq",
      "entryPkFieldName": "FEntryID",
      "serviceRuleCount": 38,
      "updateActionCount": 0
    },
    {
      "entityKey": "FEntryDeliveryPlan",
      "entityName": "交货明细",
      "table": "t_PUR_POENTRYDELIPLAN",
      "entryName": "POOrderEntryDeliPlan",
      "elementType": "60502",
      "seqFieldKey": "FSEQ",
      "entryPkFieldName": "FDetailId",
      "serviceRuleCount": 3,
      "updateActionCount": 0
    }
  ]
}
```

**解读**：

| 实体 | seqFieldKey | entryPkFieldName | 说明 |
|------|------------|-----------------|------|
| 基本信息（单据头） | `""` | `FID` | 主表无序号字段，主键为 FID（默认规则） |
| 明细信息（单据体） | `FSeq` | `FEntryID` | XML 中有 EntryPkFieldName 标签 |
| 交货明细（子单据体） | `FSEQ` | `FDetailId` | 子表有独立的主键和序号字段 |

### 使用示例：通过 fields 命令查看字段所属实体的主键和序号信息

```bash
k3cli fields --form PUR_PurchaseOrder --keyword "FMaterialId" --exact --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "fields",
  "data": [
    {
      "formName": "采购订单",
      "entityName": "明细信息",
      "entityKey": "FPOORDERENTRY",
      "seqFieldKey": "FSeq",
      "entryPkFieldName": "FEntryID",
      "table": "t_PUR_POOrderEntry",
      "key": "FMaterialId",
      "name": "物料编码",
      "fieldName": "FMATERIALID",
      "elementType": "13",
      "elementTypeName": "基础资料",
      "tagName": "BaseDataField"
    }
  ],
  "count": 1
}
```

**解读**：
- `seqFieldKey: "FSeq"` → 该实体使用 FSeq 作为行序号字段
- `entryPkFieldName: "FEntryID"` → 该实体使用 FEntryID 作为主键

### 这些字段在 SQL 命令中的应用

`sql` 命令会自动使用这些信息生成正确的 WHERE 条件：

```bash
k3cli sql --form PUR_PurchaseOrder --fields "FMaterialId" --pretty
```

输出中的 SQL 模板：

```sql
-- SELECT 模板（使用 seqFieldKey 作为行定位条件）
SELECT
    e.FMATERIALID AS [物料编码]
FROM t_PUR_POOrder h
INNER JOIN t_PUR_POOrderEntry e ON e.FID = h.FID
WHERE h.FBILLNO = @BillNo AND e.FSEQ = @Seq;

-- UPDATE 模板（使用 entryPkFieldName 作为主键定位）
UPDATE t_PUR_POOrderEntry
SET
    FMATERIALID = @NewValue_FMATERIALID
WHERE FEntryID = (
    SELECT e.FEntryID
    FROM t_PUR_POOrderEntry e
    INNER JOIN t_PUR_POOrder h ON e.FID = h.FID
    WHERE h.FBILLNO = @BillNo AND e.FSEQ = @Seq
);
```

**关键点**：
- `WHERE FEntryID = ...` → 使用 `entryPkFieldName` 定位要更新的行
- `AND e.FSEQ = @Seq` → 使用 `seqFieldKey` 定位具体的行序号

---

## 命令速查表

| 命令 | 用途 | 关键参数 |
|------|------|---------|
| `fields` | 查询表单字段（支持括号容错+实体提示） | `--form`, `--entity`, `--keyword`, `--exact` |
| `search` | 搜索表单或字段 | `--keyword`, `--type field\|table`, `--exact` |
| `form` | 查询表单元数据 | `--id` |
| `billtype` | 查询单据类型（列表/详情） | `--form`, `--id`, `--keyword` |
| `billstatus` | 查询单据状态枚举值 | `--form`, `--field`, `--keyword` |
| `enum` | 查询下拉列表枚举值 | `--id` (enumType) |
| `assistantdata` | 查询辅助资料选项 | `--id` (lookUpObject) |
| `resolve` | 解析 lookUpObject 对应表单 | `--id` (lookUpObject) |
| `connections` | 管理数据库连接 | `list`, `add`, `test`, `set-default` |
| `probe` | 探测物理表列（字典未收录时使用） | `--table`, `--keyword` |
| `sql` | 生成 SQL 辅助信息（模板文本，不执行） | `--form`, `--fields` |

## elementType 速查

| elementType | tagName | 说明 | 关联查询命令 |
|---|---|---|---|
| 1 | TextField | 文本 | - |
| 7 | OrgField | 组织 | - |
| 8 | CheckBoxField | 复选框 | - |
| 9 | ComboField | 下拉列表 | `enum --id <enumType>` |
| 12 | BillNoField | 单据编号 | - |
| 13 | BaseDataField | 基础资料 | `resolve --id <lookUpObject>` → `fields --form <formId>` |
| 30 | AssistantField | 辅助资料 | `assistantdata --id <lookUpObject>` |
| 40 | BillStatusField | 单据状态 | `billstatus --form <formId> --field <fieldKey>` |
| 44 | BillTypeField | 单据类型 | `billtype --form <formId>` |
