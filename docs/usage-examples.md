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

## 命令速查表

| 命令 | 用途 | 关键参数 |
|------|------|---------|
| `fields` | 查询表单字段 | `--form`, `--entity`, `--keyword`, `--exact` |
| `search` | 搜索表单或字段 | `--keyword`, `--type field\|table`, `--exact` |
| `form` | 查询表单元数据 | `--id` |
| `billtype` | 查询单据类型（列表/详情） | `--form`, `--id`, `--keyword` |
| `billstatus` | 查询单据状态枚举值 | `--form`, `--field`, `--keyword` |
| `enum` | 查询下拉列表枚举值 | `--id` (enumType) |
| `assistantdata` | 查询辅助资料选项 | `--id` (lookUpObject) |
| `resolve` | 解析 lookUpObject 对应表单 | `--id` (lookUpObject) |
| `connections` | 管理数据库连接 | `list`, `add`, `test`, `set-default` |

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
