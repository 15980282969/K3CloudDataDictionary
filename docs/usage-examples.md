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

#### 状态值中文注释说明

`billstatus` 命令输出的每个状态项都包含 `annotation` 字段，提供中文注释和常用标识：

| 状态值 | 中文注释 | 常用标识 |
|--------|----------|----------|
| Z | 暂存 | ← 常用 |
| A | 待审核 | ← 常用 |
| B | 审核中 | - |
| C | 已审核 | ← 常用 |
| D | 重新审核 | - |
| E | 已驳回 | ← 常用 |

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
        { "value": "Z", "name": "暂存", "annotation": "暂存 ← 常用" },
        { "value": "A", "name": "创建", "annotation": "待审核 ← 常用" },
        { "value": "B", "name": "已审核", "annotation": "审核中" },
        { "value": "C", "name": "已反审", "annotation": "已审核 ← 常用" },
        { "value": "D", "name": "重新审核", "annotation": "重新审核" },
        { "value": "E", "name": "已驳回", "annotation": "已驳回 ← 常用" }
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

## 案例：单据头→明细字段批量同步（sql sync 命令）

### 场景说明

在金蝶 K3Cloud 中，部分自定义字段同时存在于单据头和明细体中（如"追加采购原因"），但数据可能只填写在单据头，明细体为空。`sql sync` 命令可自动生成将单据头字段值批量同步到明细体的 SQL 语句，仅当单据头不为空且明细为空时更新，避免覆盖已有数据。

> **安全说明**：`sql sync` 命令仅生成 SQL 模板文本，不会执行任何写操作。输出的 SQL 需要复制到数据库管理工具中手动执行。

### 参数说明

| 参数 | 必填 | 说明 |
|------|------|------|
| `--form <identifier>` | 是 | 表单标识，如 `PUR_Requisition` |
| `--field <keyword>` | 是 | 字段关键词，支持中文名、字段 Key、物理列名 |
| `--connection, -c <id>` | 否 | 指定连接 ID |
| `--pretty` | 否 | 格式化 JSON 输出 |

### 使用示例

#### 基本用法：按中文名称同步

```bash
k3cli sql sync --form PUR_Requisition --field 追加采购原因 --pretty
```

#### 按字段 Key 同步

```bash
k3cli sql sync --form PUR_Requisition --field F_ROV_ZJCGYY --pretty
```

#### 同步委外订单的字段

```bash
k3cli sql sync --form SUB_SUBREQORDER --field 追加采购原因 --pretty
```

### 输出示例

```json
{
  "success": true,
  "command": "sql sync",
  "data": {
    "formIdentifier": "PUR_Requisition",
    "formName": "采购申请单",
    "fieldMapping": {
      "headField": {
        "name": "追加采购原因",
        "key": "F_ROV_ZJCGYY",
        "fieldName": "F_ROV_ZJCGYY",
        "table": "T_PUR_Requisition",
        "entityKey": "Requisition"
      },
      "entryField": {
        "name": "追加采购原因",
        "key": "F_RHHE_ZJCGYY",
        "fieldName": "F_RHHE_ZJCGYY",
        "table": "T_PUR_ReqEntry",
        "entityKey": "ReqEntry"
      }
    },
    "updateSql": "UPDATE T_PUR_ReqEntry\nSET F_RHHE_ZJCGYY = h.F_ROV_ZJCGYY\nFROM T_PUR_ReqEntry d\nINNER JOIN T_PUR_Requisition h ON d.FID = h.FID\nWHERE h.F_ROV_ZJCGYY IS NOT NULL\n  AND h.F_ROV_ZJCGYY <> ''\n  AND (d.F_RHHE_ZJCGYY IS NULL OR d.F_RHHE_ZJCGYY = '');",
    "previewSql": "SELECT d.FENTRYID, d.FID, h.F_ROV_ZJCGYY AS HeadValue, d.F_RHHE_ZJCGYY AS DetailValue\nFROM T_PUR_ReqEntry d\nINNER JOIN T_PUR_Requisition h ON d.FID = h.FID\nWHERE h.F_ROV_ZJCGYY IS NOT NULL\n  AND h.F_ROV_ZJCGYY <> ''\n  AND (d.F_RHHE_ZJCGYY IS NULL OR d.F_RHHE_ZJCGYY = '');",
    "hint": "建议先执行 previewSql 预览受影响的数据，确认无误后再执行 updateSql。"
  }
}
```

### 输出字段说明

| 字段 | 含义 |
|------|------|
| `formIdentifier` | 表单标识 |
| `formName` | 表单名称 |
| `fieldMapping.headField` | 单据头字段信息（名称、Key、物理列名、所在表） |
| `fieldMapping.entryField` | 明细体字段信息（名称、Key、物理列名、所在表） |
| `updateSql` | 可直接使用的 UPDATE 同步 SQL |
| `previewSql` | 预览受影响数据的 SELECT SQL |
| `hint` | 操作建议 |

### 典型使用流程

```
1. 先用 fields --compare 确认字段在单据头和明细体中的分布
   k3cli fields --form PUR_Requisition --compare --keyword 追加采购原因 --pretty

2. 用 sql sync 生成同步 SQL
   k3cli sql sync --form PUR_Requisition --field 追加采购原因 --pretty

3. 复制 previewSql 到 SSMS 预览受影响的数据

4. 确认无误后执行 updateSql
```

### 注意事项

1. `sql sync` 命令**仅生成文本**，不会执行任何 SQL
2. 生成的 SQL 使用 `FROM ... INNER JOIN` 语法（SQL Server 扩展 UPDATE 语法）
3. 同步条件为：单据头字段 `IS NOT NULL AND <> ''`，且明细字段 `IS NULL OR = ''`
4. 若单据头和明细体的物理列名不同，命令会自动识别各自的列名
5. 建议先执行 `previewSql` 预览数据，确认无误后再执行 `updateSql`
6. 如果字段在明细体中不存在，命令会返回错误提示

---

## 案例：对比单据头和明细体字段（fields --compare）

### 场景说明

在编写单据头→明细同步 SQL 之前，需要了解哪些字段仅存在于单据头、哪些仅存在于明细体、哪些两者都有。`fields --compare` 参数可快速对比单据头和明细体的字段分布，帮助定位需要同步的字段。

### 参数说明

| 参数 | 必填 | 说明 |
|------|------|------|
| `--form <identifier>` | 是 | 表单标识 |
| `--compare` | 是 | 启用对比模式 |
| `--keyword <keyword>` | 否 | 按关键词过滤对比结果 |
| `--connection, -c <id>` | 否 | 指定连接 ID |
| `--pretty` | 否 | 格式化 JSON 输出 |

### 使用示例

#### 对比所有字段

```bash
k3cli fields --form PUR_Requisition --compare --pretty
```

#### 按关键词过滤

```bash
k3cli fields --form PUR_Requisition --compare --keyword 追加采购 --pretty
```

### 输出示例

```json
{
  "success": true,
  "command": "fields compare",
  "data": {
    "formIdentifier": "PUR_Requisition",
    "formName": "采购申请单",
    "headEntity": {
      "key": "Requisition",
      "name": "基本信息",
      "table": "T_PUR_Requisition"
    },
    "entryEntity": {
      "key": "ReqEntry",
      "name": "明细信息",
      "table": "T_PUR_ReqEntry"
    },
    "headOnlyCount": 8,
    "entryOnlyCount": 35,
    "bothCount": 3,
    "headOnly": [
      { "name": "补单天数", "key": "F_ROV_BDTS", "fieldName": "F_ROV_BDTS", "table": "T_PUR_Requisition" },
      { "name": "补单原因", "key": "F_ROV_BDYY", "fieldName": "F_ROV_BDYY", "table": "T_PUR_Requisition" },
      { "name": "追加采购原因", "key": "F_ROV_ZJCGYY", "fieldName": "F_ROV_ZJCGYY", "table": "T_PUR_Requisition" }
    ],
    "entryOnly": [
      { "name": "行号", "key": "F_ROV_HH", "fieldName": "F_ROV_HH", "table": "T_PUR_ReqEntry" },
      { "name": "采购员", "key": "F_ROV_PURCHASERID", "fieldName": "F_ROV_PURCHASERID", "table": "T_PUR_ReqEntry" }
    ],
    "both": [
      { "name": "制单超期天数", "key": "F_ROV_ZDCQTS", "fieldName": "F_ROV_ZDCQTS", "headTable": "T_PUR_Requisition", "entryTable": "T_PUR_ReqEntry" }
    ]
  }
}
```

### 输出字段说明

| 字段 | 含义 |
|------|------|
| `headEntity` | 单据头实体信息（Key、名称、物理表） |
| `entryEntity` | 明细体实体信息（Key、名称、物理表） |
| `headOnlyCount` | 仅单据头有的字段数量 |
| `entryOnlyCount` | 仅明细体有的字段数量 |
| `bothCount` | 两者都有的字段数量 |
| `headOnly` | 仅单据头有的字段列表 |
| `entryOnly` | 仅明细体有的字段列表 |
| `both` | 两者都有的字段列表 |

### 典型使用场景

1. **字段同步前分析**：用 `--compare` 查看哪些字段仅在单据头，然后用 `sql sync` 生成同步 SQL
2. **字段分布概览**：快速了解表单的字段在头/明细中的分布情况
3. **关键词过滤**：用 `--keyword` 缩小范围，快速定位特定字段

### 注意事项

1. 对比基于字段名称（Name）匹配，同名字段会被归类到 `both`
2. 如果单据头和明细体中同名字段的物理列名不同，`both` 列表会分别显示各自的表名
3. 对比结果可配合 `sql sync` 命令使用，快速完成字段同步

---

## 案例：多关键词批量查询字段

### 场景说明

当需要同时查询多个字段信息时，无需逐条执行命令。`fields` 命令的 `--keyword` 参数支持逗号（`,`）或分号（`;`）分隔的多关键词批量查询，系统会拆分关键词并对每个关键词独立查询，最终返回所有匹配结果的合并集合。

### 使用示例

#### 使用逗号分隔多个关键词

```bash
k3cli fields --form SUB_SUBREQORDER --keyword "追加采购原因,修改类别,单据状态" --pretty
```

#### 使用分号分隔多个关键词

```bash
k3cli fields --form PUR_PurchaseOrder --keyword "物料;供应商;日期" --pretty
```

#### 混合使用中英文分隔符

```bash
# 以下写法均有效
k3cli fields --form PUR_PurchaseOrder --keyword "物料编码,供应商" --pretty
k3cli fields --form PUR_PurchaseOrder --keyword "物料编码；供应商" --pretty
k3cli fields --form PUR_PurchaseOrder --keyword "物料编码，供应商" --pretty
```

### 输出示例

```json
{
  "success": true,
  "command": "fields",
  "data": [
    {
      "formName": "采购申请单",
      "entityName": "基本信息",
      "table": "T_PUR_Requisition",
      "key": "F_ROV_ZJCGYY",
      "name": "追加采购原因",
      "fieldName": "F_ROV_ZJCGYY",
      "elementType": "1",
      "elementTypeName": "文本"
    },
    {
      "formName": "采购申请单",
      "entityName": "明细信息",
      "table": "T_PUR_ReqEntry",
      "key": "F_RHHE_ZJCGYY",
      "name": "追加采购原因",
      "fieldName": "F_RHHE_ZJCGYY",
      "elementType": "1",
      "elementTypeName": "文本"
    },
    {
      "formName": "采购申请单",
      "entityName": "基本信息",
      "table": "T_PUR_Requisition",
      "key": "FDocumentStatus",
      "name": "单据状态",
      "fieldName": "FDOCUMENTSTATUS",
      "elementType": "40",
      "elementTypeName": "单据状态"
    }
  ],
  "count": 3
}
```

### 匹配规则

1. 多关键词之间为"**或**"关系，匹配任意一个即返回
2. 每个关键词独立进行模糊匹配（或精确匹配，配合 `--exact`）
3. 支持的分隔符：英文逗号 `,`、中文逗号 `，`、英文分号 `;`、中文分号 `；`
4. 空关键词会被自动忽略

### 配合精确匹配使用

```bash
# 精确匹配多个字段（字段名必须完全相等）
k3cli fields --form PUR_PurchaseOrder --keyword "FMaterialId,FSupplierId" --exact --pretty
```

---

## 案例：按字段类型过滤（fields --type）

### 场景说明

当表单字段较多时，可能需要只查看特定类型的字段。`--type` 参数支持按字段所属实体类型或字段属性进行过滤。

### 参数说明

| 类型值 | 说明 |
|--------|------|
| `entry` | 只显示明细实体（单据体）字段 |
| `head` | 只显示头部实体（单据头）字段 |
| `oid` | 只显示 OID 相关字段 |
| `normal` | 只显示普通业务字段（非 OID） |

### 使用示例

#### 只显示明细体字段

```bash
k3cli fields --form PUR_PurchaseOrder --type entry --pretty
```

#### 只显示单据头字段

```bash
k3cli fields --form PUR_PurchaseOrder --type head --pretty
```

#### 只显示普通业务字段（排除 OID）

```bash
k3cli fields --form PUR_PurchaseOrder --type normal --pretty
```

#### 组合使用：明细体 + 关键词过滤

```bash
k3cli fields --form PUR_PurchaseOrder --type entry --keyword "物料" --pretty
```

### 典型使用场景

1. **快速了解明细字段**：使用 `--type entry` 快速查看单据体所有字段
2. **单据头字段分析**：使用 `--type head` 查看单据头字段分布
3. **配合其他参数**：可与 `--keyword`、`--entity` 等参数组合使用

---

## 案例：连接管理功能增强

### 场景说明

CLI 工具现在支持更完善的连接管理功能，包括自动连接检测、连接历史追踪和简化的连接测试。

### 3.1 自动连接检测

执行任何查询命令（`fields`、`search`、`form` 等）前，系统会自动检测连接可达性。若连接失败，会显示明确的错误提示。

```bash
k3cli fields --form PUR_PurchaseOrder --pretty
```

输出示例（连接失败时）：

```
正在检测连接: 采购系统 (AISC001)...
连接检测失败: 网络相关或特定于实例的错误...
网络连接失败，请检查 VPN 是否已开启或数据库连接配置是否正确
```

### 3.2 连接历史追踪

`connections list` 输出新增 `lastSuccessfulConnection` 字段，显示上次连接成功的时间戳。

```bash
k3cli connections list --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "connections",
  "data": [
    {
      "id": 1,
      "name": "采购系统",
      "server": "192.168.1.100,1433",
      "database": "AISC001",
      "user": "sa",
      "isDefault": true,
      "displayName": "采购系统 (AISC001)",
      "lastSuccessfulConnection": "2026-07-29 10:30:00"
    },
    {
      "id": 2,
      "name": "测试环境",
      "server": "192.168.1.200,1433",
      "database": "AISC_TEST",
      "user": "sa",
      "isDefault": false,
      "displayName": "测试环境 (AISC_TEST)",
      "lastSuccessfulConnection": "从未连接"
    }
  ],
  "count": 2
}
```

### 3.3 默认连接测试

`connections test` 命令现在支持不带 `--id` 参数，默认测试当前设置的默认连接。

```bash
# 测试默认连接（无需指定 --id）
k3cli connections test

# 测试指定连接
k3cli connections test --id 2
```

输出示例：

```json
{
  "success": true,
  "command": "connections",
  "data": {
    "connectionId": 1,
    "name": "采购系统",
    "server": "192.168.1.100,1433",
    "database": "AISC001",
    "success": true,
    "message": "连接成功",
    "lastSuccessfulConnection": "2026-07-29 10:35:00"
  }
}
```

### 连接管理最佳实践

1. **定期检查连接状态**：使用 `connections list --pretty` 查看上次成功连接时间
2. **快速验证连接**：使用 `connections test` 测试默认连接是否正常
3. **多环境管理**：使用 `connections test --id <id>` 测试不同环境的连接

---

## 命令速查表

| 命令 | 用途 | 关键参数 |
|------|------|---------|
| `fields` | 查询表单字段（支持多关键词+类型过滤） | `--form`, `--entity`, `--keyword`, `--exact`, `--type` |
| `search` | 搜索表单或字段 | `--keyword`, `--type field\|table`, `--exact` |
| `form` | 查询表单元数据 | `--id` |
| `billtype` | 查询单据类型（列表/详情） | `--form`, `--id`, `--keyword` |
| `billstatus` | 查询单据状态枚举值（含中文注释） | `--form`, `--field`, `--keyword` |
| `enum` | 查询下拉列表枚举值 | `--id` (enumType) |
| `assistantdata` | 查询辅助资料选项 | `--id` (lookUpObject) |
| `resolve` | 解析 lookUpObject 对应表单 | `--id` (lookUpObject) |
| `connections` | 管理数据库连接（含自动检测+历史追踪） | `list`, `add`, `test`, `set-default` |
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

---

## 案例：ORM 实体名与实体标识的区别

### 场景说明

在金蝶 K3Cloud 中，每个实体有两个重要的标识：

| 字段 | 含义 | 示例 |
|------|------|------|
| `entityKey` | 实体标识（Entity Key） | `FEntity`、`FPOOrderEntry` |
| `ormEntityName` | ORM 实体名（EntryName） | `ReqEntry`、`POOrderEntry` |

**重要**：在代码中访问 `DynamicObject` 对象实体时，应当使用 **ORM 实体名**（`ormEntityName`）进行访问，而非实体标识（`entityKey`）。

### 使用示例

```bash
k3cli fields --form PUR_PurchaseOrder --keyword "物料编码" --exact --pretty
```

输出示例：

```json
{
  "data": [
    {
      "formName": "采购订单",
      "entityName": "明细信息",
      "entityKey": "FPOORDERENTRY",
      "ormEntityName": "POOrderEntry",
      "table": "t_PUR_POOrderEntry",
      "key": "FMaterialId",
      "name": "物料编码"
    }
  ]
}
```

### 字段说明

| 字段 | 含义 | 用途 |
|------|------|------|
| `entityKey` | 实体标识 | 用于 CLI 命令的 `--entity` 参数 |
| `ormEntityName` | ORM 实体名 | 用于代码中访问 DynamicObject 实体 |
| `entityName` | 实体显示名称 | 用于界面显示（如"明细信息"） |

### 代码访问示例

```csharp
// 错误：使用 entityKey 访问实体
var entry = dynamicObject["FPOOrderEntry"];  // ❌

// 正确：使用 ormEntityName 访问实体
var entry = dynamicObject["POOrderEntry"];   // ✅
```

### 常见实体的 ORM 实体名对照

| 表单 | entityKey | ormEntityName | 说明 |
|------|-----------|---------------|------|
| 采购订单-明细 | `FPOORDERENTRY` | `POOrderEntry` | 明细信息实体 |
| 采购订单-财务 | `FPOORDERFINANCE` | `POOrderFinance` | 财务信息实体 |
| 采购申请单-明细 | `FEntity` | `ReqEntry` | 明细信息实体 |
| 应收单-明细 | `FEntityDetail` | `ReceivableEntry` | 明细信息实体 |

### 注意事项

1. **CLI 命令参数**：使用 `entityKey`（如 `--entity FPOOrderEntry`）
2. **代码访问实体**：使用 `ormEntityName`（如 `dynamicObject["POOrderEntry"]`）
3. **数据库表名**：使用 `table` 字段（如 `t_PUR_POOrderEntry`）
4. **三者不可混用**，否则会导致访问失败

---

## 案例：常用代码查询（query 命令）

### 场景说明

`query` 命令提供常用业务数据的快速查询能力，通过预定义的 SQL 语句直接返回业务数据，无需手动编写 SQL。

### 使用流程

#### 第一步：查看可用查询列表

```bash
k3cli query list --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "query",
  "data": [
    {
      "name": "user-licenses",
      "description": "查询用户许可分配（组织、用户、许可分组）",
      "parameters": "--org <组织名称关键词>, --user <用户名称关键词>"
    }
  ],
  "count": 1
}
```

#### 第二步：执行查询

```bash
# 查询所有用户许可分配
k3cli query user-licenses --pretty

# 按组织名称过滤
k3cli query user-licenses --org "荣耀" --pretty

# 按用户名称过滤
k3cli query user-licenses --user "Harrison" --pretty

# 组合过滤
k3cli query user-licenses --org "荣耀" --user "Harrison" --pretty
```

输出示例：

```json
{
  "success": true,
  "command": "query",
  "data": [
    {
      "组织名称": "荣耀...",
      "组织编码": "300",
      "用户ID": 100218,
      "用户名称": "郑...",
      "许可分组代码": "BOS",
      "许可分组名称": "BOS运行平台"
    },
    {
      "组织名称": "荣耀...",
      "组织编码": "300",
      "用户ID": 3442639,
      "用户名称": "张...",
      "许可分组代码": "BOS_Integration",
      "许可分组名称": "BOS运行平台-融合开发"
    }
  ],
  "count": 2
}
```

### 许可分组代码对照表

| 代码 | 名称 | 代码 | 名称 |
|------|------|------|------|
| `FIN` | 财务会计云 | `SCM` | 供应链云 |
| `FIN_SCM` | 财务会计+供应链 | `MFG` | 智能制造云 |
| `FIN_SCM_MFG` | 财务会计+供应链+标准制造 | `MFG_AdvMFG` | 高级制造云 |
| `FIN_SCM_MFG_AdvMFG` | 财务会计+供应链+高级制造 | `MA` | 管理会计云 |
| `BMCloud` | 预算管理云 | `CRCloud` | 合并报表云 |
| `QM` | 质量管理云 | `B2C_EBus` | B2C电商云 |
| `AllChannels` | 全渠道营销云 | `BBC` | BBC营销云 |
| `CRM` | 客户关系管理 | `SupplierCollaboration` | 供应协同云 |
| `EmployeeService` | 员工服务云 | `PLM` | PLM云 |
| `BI` | 经营分析 | `QING` | 数据服务云 |
| `BOS` | BOS运行平台 | `BOS_Indie` | BOS运行时-独立开发 |
| `BOS_Integration` | BOS运行平台-融合开发 | `BOS_ISV` | 行业产品BOS运行平台 |
| `BOS_Mobile` | 移动BOS运行平台 | `Pro` | 专业应用组 |
| `All` | 全员应用组 | `ViewOnly` | 仅查询应用 |
| `K3Cloud_ERP_RI` | 零售云 | `SmartShop` | 智能导购助手 |
| `WisdomWorkshop` | 智慧车间MES云 | `DeviceCloud` | 设备云 |
| `EKanban` | 电子看板 | `Kanban` | 数字大屏 |
| `DSStock` | 动态安全库存 | `YDTM` | 移动条码 |
| `MobileReport` | 移动工序报工 | | |

### 案例：查询角色功能权限（role-permissions 查询）

#### 场景说明

当需要审计或排查某个角色的功能权限配置时，可以使用 `role-permissions` 查询一次性获取所有已启用角色的完整权限明细，包括业务领域、子系统、业务对象、权限项及权限状态。

#### 使用方式

```bash
# 查询所有角色的功能权限明细
k3cli query role-permissions --pretty
```

#### 输出字段说明

| 字段 | 说明 |
|------|------|
| `FRoleNumber` | 角色编码 |
| `FRoleName` | 角色名称 |
| `FRoleID` | 角色内码 |
| `FTopClassName` | 业务领域（顶级分类）名称 |
| `FSubSystemNumber` | 子系统编码 |
| `FSubSystemName` | 子系统名称 |
| `FObjectTypeName` | 业务对象名称 |
| `FPermissionItemNumber` | 权限项编码（如 BOS_VIEW、BOS_NEW） |
| `FPermissionItemName` | 权限项名称（如 查看、新增） |
| `FPermissionStatusName` | 权限状态（有权 / 禁止 / 无权） |
| `FForbidStatusName` | 角色是否禁用（是 / 否） |

#### 输出示例

```json
{
  "success": true,
  "command": "query",
  "data": [
    {
      "FRoleNumber": "BD01_SYS",
      "FRoleName": "administrator",
      "FRoleID": 6,
      "FTopClassName": "财务会计",
      "FSubSystemNumber": "ER",
      "FSubSystemName": "费用管理",
      "FObjectTypeName": "掌上报销自定义字段设置",
      "FPermissionItemNumber": "BOS_NEW",
      "FPermissionItemName": "新增",
      "FPermissionItemIndex": 1,
      "FPermissionStatusName": "有权",
      "FForbidStatusName": "否"
    }
  ],
  "count": 1
}
```

#### 注意事项

- 查询结果已按角色编码、业务领域、子系统、业务对象、权限项序号排序
- 仅返回已启用（`FFORBIDSTATUS = 'A'`）的角色，已禁用的角色不显示
- 权限状态为"有权"表示该角色拥有对应权限项，"禁止"表示显式禁止，"无权"表示未授权

### 案例：查询用户角色权限（user-role-permissions 查询）

#### 场景说明

当需要审计或排查某个具体用户在各个组织下通过角色继承的功能权限时，可以使用 `user-role-permissions` 查询。与 `role-permissions` 不同，此查询关联了用户-角色关系表（`t_sec_userrolemap`）和用户组织关系表（`T_SEC_USERORG`），能够展示用户在每个组织下通过角色获得的具体权限明细。

#### 使用方式

```bash
# 查询指定用户的角色权限明细
k3cli query user-role-permissions --user 110792 --pretty

# 查询全部已启用用户的角色权限明细
k3cli query user-role-permissions --pretty

# 按表单标识过滤（只返回指定表单的权限对象记录）
k3cli query user-role-permissions --user 110792 --form FIN_YFD_SYS --pretty
```

#### 输出字段说明

| 字段 | 说明 |
|------|------|
| `FUserName` | 用户名称 |
| `FUserID` | 用户内码 |
| `FForbidStatusName` | 用户是否禁用（是 / 否） |
| `FOrgNumber` | 组织编码 |
| `FOrgName` | 组织名称 |
| `FRoleName` | 角色名称 |
| `FTopClassName` | 业务领域（顶级分类）名称 |
| `FSubSystemName` | 子系统名称 |
| `FObjectTypeName` | 业务对象名称 |
| `FPermissionItemName` | 权限项名称（如 查看、新增、修改） |
| `FPermissionStatusName` | 权限状态（有权 / 禁止 / 无权） |
| `FIDENTITYID` | 行序号（按用户、组织、业务领域、子系统、业务对象、权限项排序） |

#### 输出示例

```json
{
  "success": true,
  "command": "query",
  "data": [
    {
      "FUserName": "陈爱芳",
      "FUserID": 110792,
      "FForbidStatusName": "否",
      "FOrgNumber": "100",
      "FOrgName": "福建荣耀健康科技股份有限公司",
      "FRoleName": "资产管理员",
      "FTopClassName": "BOS",
      "FSubSystemName": "应用框架",
      "FObjectTypeName": "编码规则",
      "FPermissionItemName": "查看",
      "FPermissionStatusNumber": 0,
      "FPermissionStatusName": "有权",
      "FIDENTITYID": 1
    }
  ],
  "count": 1
}
```

#### 注意事项

- 查询结果已按用户名称、组织编码、业务领域、子系统、业务对象、权限项名称、权限状态排序
- 仅返回已启用（`FFORBIDSTATUS = 'A'`）的用户，已禁用的用户不显示
- 不传 `--user` 参数时返回所有已启用用户的权限明细，数据量可能较大，建议指定用户ID
- 使用 `--form <表单标识>` 可按表单过滤，只返回该表单对应的权限对象记录（如 `FIN_YFD_SYS` 应付单）
- 权限状态为"有权"表示该用户通过角色拥有对应权限项，"禁止"表示显式禁止，"无权"表示未授权

### 添加新查询

要添加新的常用查询，需要修改以下文件：

1. **`MetadataQueryService.cs`**：添加查询方法
   ```csharp
   public List<Dictionary<string, object>> QueryXxx(string param = null)
   {
       string sql = @"SELECT ... FROM ...";
       return ExecuteSql(sql, parameters);
   }
   ```

2. **`GetAvailableQueries()`**：注册查询信息
   ```csharp
   new Dictionary<string, object>
   {
       ["name"] = "xxx",
       ["description"] = "查询描述",
       ["parameters"] = "--param <说明>"
   }
   ```

3. **`QueryCommand.cs`**：添加路由
   ```csharp
   case "xxx":
       return ExecuteXxx(queryArgs, service);
   ```

4. **`HelpCommand.cs`**：更新帮助文档

### 注意事项

1. `query` 命令直接执行 SQL 查询数据库，**仅支持只读查询**
2. 查询结果中的列名使用中文别名（如"组织名称"、"用户名称"）
3. 过滤参数支持模糊匹配（LIKE %keyword%）
4. 新增查询需要重新编译 CLI 工具

---

## 案例：查询数据库阻塞/死锁（blocking 查询）

### 场景说明

当数据库出现性能问题或用户反馈操作卡顿时，可以使用 `blocking` 查询快速定位阻塞链，找出哪些进程在阻塞其他进程，以及被阻塞的 SQL 语句内容。

### 使用方式

```bash
# 查询当前所有阻塞/死锁进程
k3cli query blocking --pretty
```

### 输出示例

```json
{
  "success": true,
  "command": "query",
  "data": [
    {
      "SPID": 55,
      "BLOCKED": 0,
      "WAITTIME": 0,
      "LASTWAITTYPE": "",
      "WAITRESOURCE": "",
      "OPEN_TRAN": 1,
      "STATUS": "sleeping",
      "DBID": 10,
      "CPU": 1250,
      "PHYSICAL_IO": 0,
      "MEMUSAGE": 2,
      "LOGIN_TIME": "2026-07-10 09:30:00",
      "LAST_BATCH": "2026-07-10 10:15:00",
      "HOSTNAME": "APP-SERVER-01",
      "program_name": ".Net SqlClient Data Provider",
      "HOSTPROCESS": "12345",
      "CMD": "AWAITING COMMAND",
      "NT_DOMAIN": "DOMAIN",
      "NT_USERNAME": "user01",
      "NET_ADDRESS": "00:11:22:33:44:55",
      "NET_LIBRARY": "TCP/IP",
      "LOGINAME": "sa",
      "TEXT": "UPDATE T_PUR_POOrder SET FDOCUMENTSTATUS = 'B' WHERE FID = 100001"
    },
    {
      "SPID": 62,
      "BLOCKED": 55,
      "WAITTIME": 30000,
      "LASTWAITTYPE": "LCK_M_X",
      "WAITRESOURCE": "KEY: 10:...",
      "OPEN_TRAN": 0,
      "STATUS": "suspended",
      "DBID": 10,
      "CPU": 500,
      "PHYSICAL_IO": 0,
      "MEMUSAGE": 2,
      "LOGIN_TIME": "2026-07-10 09:30:00",
      "LAST_BATCH": "2026-07-10 10:15:30",
      "HOSTNAME": "APP-SERVER-02",
      "program_name": ".Net SqlClient Data Provider",
      "HOSTPROCESS": "12346",
      "CMD": "SELECT",
      "NT_DOMAIN": "DOMAIN",
      "NT_USERNAME": "user02",
      "NET_ADDRESS": "00:11:22:33:44:66",
      "NET_LIBRARY": "TCP/IP",
      "LOGINAME": "sa",
      "TEXT": "SELECT * FROM T_PUR_POOrder WHERE FID = 100001"
    }
  ],
  "count": 2
}
```

### 输出字段说明

| 字段 | 含义 | 排查要点 |
|------|------|---------|
| `SPID` | 进程 ID | 唯一标识每个连接 |
| `BLOCKED` | 被谁阻塞（0=未被阻塞） | **> 0 表示该进程被 SPID=BLOCKED 值的进程阻塞** |
| `WAITTIME` | 等待时间（毫秒） | 值越大说明阻塞越严重 |
| `LASTWAITTYPE` | 等待类型 | `LCK_M_X` = 排他锁等待，`LCK_M_S` = 共享锁等待 |
| `WAITRESOURCE` | 等待的资源 | 被锁定的具体资源标识 |
| `OPEN_TRAN` | 未提交事务数 | **> 0 表示有未提交事务，可能是阻塞源头** |
| `STATUS` | 进程状态 | `sleeping` + `OPEN_TRAN > 0` = 持有锁但未操作 |
| `LOGINAME` | 登录名 | 定位是哪个用户/应用发起的 |
| `HOSTNAME` | 主机名 | 定位是哪台服务器发起的 |
| `program_name` | 程序名称 | 定位是哪个应用发起的 |
| `TEXT` | 执行的 SQL 文本 | **最关键字段，显示阻塞相关的 SQL 语句** |

### 阻塞链分析

从输出中可以构建阻塞链：

```
SPID 55 (BLOCKED=0, OPEN_TRAN=1)  ← 阻塞源头
  └── SPID 62 (BLOCKED=55, WAITTIME=30000)  ← 被阻塞
```

**分析步骤**：

1. 找到 `BLOCKED = 0` 且 `OPEN_TRAN > 0` 的进程 → **阻塞源头**
2. 查看其 `TEXT` 字段 → 确认正在执行的 SQL
3. 找到 `BLOCKED > 0` 的进程 → **被阻塞的进程**
4. 根据 `WAITTIME` 判断阻塞严重程度

### 常见阻塞场景

| 场景 | 特征 | 处理建议 |
|------|------|---------|
| 长事务未提交 | `STATUS=sleeping`, `OPEN_TRAN>0`, `LASTWAITTYPE` 为空 | 找到应用端提交或回滚事务 |
| 锁等待 | `LASTWAITTYPE=LCK_M_X` 或 `LCK_M_S` | 优化 SQL 减少锁范围 |
| 死锁 | 多个进程互相阻塞 | SQL Server 会自动选择牺牲者 |
| 索引缺失导致表锁 | `WAITRESOURCE` 显示表级锁 | 添加合适的索引 |

### 权限要求

执行 `blocking` 查询需要当前登录用户具有 **`VIEW SERVER STATE`** 权限。

```sql
-- 授予权限（需要 sysadmin 角色执行）
GRANT VIEW SERVER STATE TO [用户名];
```

如果权限不足，会返回错误：
```
拒绝了对对象 'server' (数据库 'master')的 VIEW SERVER STATE 权限。
用户没有执行此操作的权限。
```

### 注意事项

1. `blocking` 查询**无需额外参数**，直接执行即可
2. 查询结果包含**阻塞链中的所有进程**（阻塞者和被阻塞者）
3. `TEXT` 字段显示的是进程最近执行的 SQL，不一定是当前正在执行的
4. 如果没有阻塞，返回空结果 `{"count": 0}`
5. 该查询访问 `master` 数据库的系统视图，需要相应权限
