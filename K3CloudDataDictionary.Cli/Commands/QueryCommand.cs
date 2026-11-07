using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// query 命令：常用代码查询（快速调用预定义 SQL 返回数据）
    /// 支持 --sql 临时只读查询（仅允许 SELECT）
    /// </summary>
    public static class QueryCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowQueryHelp();
                return 0;
            }

            // --sql 临时查询：必须在取查询名之前判断
            var adhocSql = Program.GetArgValue(args, "sql");
            var sqlFile = Program.GetArgValue(args, "file");
            if (string.IsNullOrWhiteSpace(adhocSql) && !string.IsNullOrWhiteSpace(sqlFile))
            {
                if (!File.Exists(sqlFile))
                {
                    JsonOutputWriter.WriteError("query", $"SQL 文件不存在: {sqlFile}");
                    return 1;
                }
                adhocSql = ReadTextAuto(sqlFile);
            }

            if (!string.IsNullOrWhiteSpace(adhocSql))
            {
                return ExecuteAdhocSql(args, options, adhocSql);
            }

            var queryName = args[0].ToLowerInvariant();
            var queryArgs = args.Skip(1).ToArray();

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                using (var service = new MetadataQueryService(connectionString))
                {

                    switch (queryName)
                    {
                        case "user-licenses":
                            return ExecuteUserLicenses(queryArgs, service);

                        case "blocking":
                            return ExecuteBlocking(service);

                        case "mo-pick-summary":
                            return ExecuteMoSummary(queryArgs, mo => service.QueryMoPickSummary(mo));

                        case "mo-return-summary":
                            return ExecuteMoSummary(queryArgs, mo => service.QueryMoReturnSummary(mo));

                        case "mo-instock-summary":
                            return ExecuteMoSummary(queryArgs, mo => service.QueryMoInstockSummary(mo));

                        case "bill-by-no":
                            return ExecuteBillByNo(queryArgs, service);

                        case "role-permissions":
                            return ExecuteRolePermissions(service);

                        case "user-role-permissions":
                            return ExecuteUserRolePermissions(queryArgs, service);

                        case "offboard-assets":
                            return ExecuteOffboardAssets(queryArgs, service);

                        case "offboard-user":
                            return ExecuteOffboardUser(queryArgs, service);

                        case "list":
                            var queries = service.GetAvailableQueries();
                            JsonOutputWriter.WriteSuccess("query", queries);
                            return 0;

                        default:
                            JsonOutputWriter.WriteError("query", $"未知的查询名称: {queryName}。使用 'k3cli query list' 查看可用查询。");
                            return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("query", ex.Message);
                return 1;
            }
        }

        private static int ExecuteUserLicenses(string[] args, MetadataQueryService service)
        {
            var orgName = Program.GetArgValue(args, "org");
            var userName = Program.GetArgValue(args, "user");

            var results = service.QueryUserLicenses(orgName, userName);
            JsonOutputWriter.WriteSuccess("query", results);
            return 0;
        }

        private static int ExecuteBlocking(MetadataQueryService service)
        {
            var results = service.QueryBlockingProcesses();
            JsonOutputWriter.WriteSuccess("query", results);
            return 0;
        }

        private static int ExecuteMoSummary(string[] args, Func<string, List<Dictionary<string, object>>> query)
        {
            var moBillNo = Program.GetArgValue(args, "mo");
            var results = query(moBillNo);
            JsonOutputWriter.WriteSuccess("query", results);
            return 0;
        }

        private static int ExecuteBillByNo(string[] args, MetadataQueryService service)
        {
            var formIdentifier = Program.GetArgValue(args, "form");
            var billNo = Program.GetArgValue(args, "no");
            if (string.IsNullOrEmpty(formIdentifier) || string.IsNullOrEmpty(billNo))
            {
                JsonOutputWriter.WriteError("query", "bill-by-no 需要参数 --form <表单标识> 和 --no <单据编号>");
                return 1;
            }

            var result = service.QueryBillByNo(formIdentifier, billNo);
            if (result.ContainsKey("error"))
            {
                JsonOutputWriter.WriteError("query", result["error"].ToString());
                return 1;
            }

            JsonOutputWriter.WriteSuccess("query", result);
            return 0;
        }

        private static int ExecuteRolePermissions(MetadataQueryService service)
        {
            var results = service.QueryRolePermissions();
            JsonOutputWriter.WriteSuccess("query", results);
            return 0;
        }

        private static int ExecuteUserRolePermissions(string[] args, MetadataQueryService service)
        {
            var userIdArg = Program.GetArgValue(args, "user");
            long? userId = null;
            if (!string.IsNullOrEmpty(userIdArg))
            {
                if (!long.TryParse(userIdArg, out long parsed))
                {
                    JsonOutputWriter.WriteError("query", "--user 必须是有效的用户ID（整数）");
                    return 1;
                }
                userId = parsed;
            }

            var formIdentifier = Program.GetArgValue(args, "form");
            var permissionItem = Program.GetArgValue(args, "permission");

            int? limit = null;
            var limitArg = Program.GetArgValue(args, "limit");
            if (!string.IsNullOrEmpty(limitArg))
            {
                if (!int.TryParse(limitArg, out int parsedLimit) || parsedLimit <= 0)
                {
                    JsonOutputWriter.WriteError("query", "--limit 必须是大于 0 的整数");
                    return 1;
                }
                limit = parsedLimit;
            }

            var results = Program.HasOption(args, "summary")
                ? service.QueryUserRolePermissionSummary(userId, formIdentifier, permissionItem)
                : service.QueryUserRolePermissions(userId, formIdentifier, permissionItem, limit);
            JsonOutputWriter.WriteSuccess("query", results);
            return 0;
        }

        /// <summary>
        /// 按工号查询名下固定资产卡片
        /// </summary>
        private static int ExecuteOffboardAssets(string[] args, MetadataQueryService service)
        {
            var staffNo = GetStaffNo(args);
            if (string.IsNullOrEmpty(staffNo))
            {
                JsonOutputWriter.WriteError("query", "offboard-assets 需要参数 --staff-no <工号>，可选 --category <资产类别>、--limit <条数>");
                return 1;
            }

            int? limit;
            if (!TryParseLimit(args, out limit)) return 1;

            var result = service.QueryStaffAssets(staffNo, Program.GetArgValue(args, "category"), limit);
            JsonOutputWriter.WriteSuccess("query", result);
            return 0;
        }

        /// <summary>
        /// 按工号一次取齐离职核查信息：账号状态、许可分组、角色汇总、名下资产
        /// </summary>
        private static int ExecuteOffboardUser(string[] args, MetadataQueryService service)
        {
            var staffNo = GetStaffNo(args);
            if (string.IsNullOrEmpty(staffNo))
            {
                JsonOutputWriter.WriteError("query", "offboard-user 需要参数 --no <工号>，可选 --category <资产类别>");
                return 1;
            }

            var result = service.QueryStaffOffboardProfile(staffNo, Program.GetArgValue(args, "category"));
            JsonOutputWriter.WriteSuccess("query", result);
            return 0;
        }

        /// <summary>
        /// 工号入参：--staff-no 与 --no 等价
        /// </summary>
        private static string GetStaffNo(string[] args)
        {
            var staffNo = Program.GetArgValue(args, "staff-no");
            return string.IsNullOrEmpty(staffNo) ? Program.GetArgValue(args, "no") : staffNo;
        }

        private static bool TryParseLimit(string[] args, out int? limit)
        {
            limit = null;
            var limitArg = Program.GetArgValue(args, "limit");
            if (string.IsNullOrEmpty(limitArg)) return true;

            if (!int.TryParse(limitArg, out int parsed) || parsed <= 0)
            {
                JsonOutputWriter.WriteError("query", "--limit 必须是大于 0 的整数");
                return false;
            }

            limit = parsed;
            return true;
        }

        /// <summary>
        /// --sql 临时查询：校验只读后执行，支持 --params 按位置映射 @p1、@p2...
        /// </summary>
        private static int ExecuteAdhocSql(string[] args, GlobalOptions options, string sql)
        {
            string validationError;
            if (!ValidateReadOnlySql(sql, out validationError))
            {
                JsonOutputWriter.WriteError("query", "SQL 校验失败（仅允许只读查询）: " + validationError);
                return 1;
            }

            try
            {
                var parameters = BuildSqlParameters(args);

                int timeout = 60;
                var timeoutArg = Program.GetArgValue(args, "timeout");
                if (!string.IsNullOrEmpty(timeoutArg) && (!int.TryParse(timeoutArg, out timeout) || timeout <= 0))
                {
                    JsonOutputWriter.WriteError("query", "--timeout 必须是大于 0 的整数（秒）");
                    return 1;
                }

                var connectionString = Program.ResolveConnectionString(options);
                using (var service = new MetadataQueryService(connectionString))
                {
                    var results = service.ExecuteSql(sql, parameters, timeout);
                    JsonOutputWriter.WriteSuccess("query", results);
                }
                return 0;
            }
            catch (SqlException ex) when (ex.Number == -2 || ex.Number == 4060 || ex.Number == 11 || ex.Number == 4053)
            {
                JsonOutputWriter.WriteError("query", $"数据库连接失败（错误码 {ex.Number}）: {ex.Message}");
                return 2;
            }
            catch (SqlException ex)
            {
                JsonOutputWriter.WriteError("query", $"SQL 执行错误 [{ex.Number}]: {ex.Message}");
                return 3;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("query", ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// 组装 --sql 的查询参数：--param name=值 可重复且值原样保留，--params 值1,值2 按位置映射为 @p1、@p2...
        /// </summary>
        private static Dictionary<string, object> BuildSqlParameters(string[] args)
        {
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in Program.GetArgValues(args, "param"))
            {
                int separator = pair.IndexOf('=');
                if (separator <= 0)
                    throw new ArgumentException($"--param 需要写成 --param 名称=值，当前: {pair}");

                var name = pair.Substring(0, separator).Trim().TrimStart('@');
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException($"--param 参数名为空: {pair}");

                parameters["@" + name] = pair.Substring(separator + 1);
            }

            var paramsArg = Program.GetArgValue(args, "params");
            if (!string.IsNullOrEmpty(paramsArg))
            {
                var values = paramsArg.Split(',');
                for (int i = 0; i < values.Length; i++)
                    parameters["@p" + (i + 1)] = values[i].Trim();
            }

            return parameters;
        }

        /// <summary>
        /// 读取 SQL 文件：带 BOM 或可按 UTF-8 严格解码时按 UTF-8，否则回退系统 ANSI（中文 Windows 为 GBK）
        /// </summary>
        private static string ReadTextAuto(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var offset = 0;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                offset = 3;

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.Default.GetString(bytes, offset, bytes.Length - offset);
            }
        }

        /// <summary>
        /// 校验 SQL 为只读单语句：仅允许以 SELECT 开头，拒绝一切写操作与危险关键词
        /// </summary>
        internal static bool ValidateReadOnlySql(string sql, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(sql))
            {
                error = "SQL 不能为空";
                return false;
            }

            // 去除注释，防止关键词隐藏在注释中绕过校验
            var cleaned = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            cleaned = Regex.Replace(cleaned, @"--[^\r\n]*", " ");

            // 将字符串字面量内容置空，避免字面量中的关键词造成误报
            var withoutLiterals = Regex.Replace(cleaned, @"'(?:[^']|'')*'", "''");

            // 只允许单语句（容忍末尾分号）
            var body = withoutLiterals.Trim().TrimEnd(';');
            if (body.IndexOf(';') >= 0)
            {
                error = "不允许多条语句，一次只能执行一条 SELECT";
                return false;
            }

            // 必须以 SELECT 开头（WITH/EXEC/INSERT 等一律拒绝）
            if (!Regex.IsMatch(cleaned.TrimStart(), @"^SELECT\b", RegexOptions.IgnoreCase))
            {
                error = "只允许 SELECT 查询";
                return false;
            }

            // 危险关键词黑名单（词边界匹配，字面量已置空，不会误伤数据值）
            var forbidden = new[]
            {
                "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE",
                "EXEC", "EXECUTE", "MERGE", "GRANT", "DENY", "REVOKE",
                "INTO", "DECLARE", "OPENROWSET", "OPENDATASOURCE",
                "BACKUP", "RESTORE", "KILL", "SHUTDOWN", "WAITFOR", "DBCC"
            };
            foreach (var kw in forbidden)
            {
                if (Regex.IsMatch(body, @"\b" + kw + @"\b", RegexOptions.IgnoreCase))
                {
                    error = "检测到不允许的关键词: " + kw;
                    return false;
                }
            }
            if (Regex.IsMatch(body, @"\b(?:SP_|XP_)\w+", RegexOptions.IgnoreCase))
            {
                error = "检测到不允许的系统存储过程调用（sp_/xp_）";
                return false;
            }

            return true;
        }
    }
}
