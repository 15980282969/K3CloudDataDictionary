using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace K3CloudDataDictionary.Cli.Services
{
    /// <summary>
    /// JSON 输出格式化器
    /// </summary>
    public static class JsonOutputWriter
    {
        private static bool _prettyPrint = false;

        /// <summary>
        /// 设置是否格式化输出
        /// </summary>
        public static void SetPrettyPrint(bool pretty)
        {
            _prettyPrint = pretty;
        }

        /// <summary>
        /// 写入成功结果
        /// </summary>
        public static void WriteSuccess(string command, object data, int? count = null)
        {
            var result = new JObject
            {
                ["success"] = true,
                ["command"] = command,
                ["data"] = data != null ? JToken.FromObject(data) : new JArray(),
                ["count"] = count ?? (data is ICollection<object> col ? col.Count : 0)
            };

            WriteJson(result);
        }

        /// <summary>
        /// 写入成功结果（列表数据）
        /// </summary>
        public static void WriteSuccess<T>(string command, List<T> data)
        {
            var result = new JObject
            {
                ["success"] = true,
                ["command"] = command,
                ["data"] = JArray.FromObject(data),
                ["count"] = data.Count
            };

            WriteJson(result);
        }

        /// <summary>
        /// 写入错误结果
        /// </summary>
        public static void WriteError(string command, string message)
        {
            var result = new JObject
            {
                ["success"] = false,
                ["command"] = command,
                ["error"] = message
            };

            Console.Error.WriteLine(_prettyPrint 
                ? result.ToString(Formatting.Indented) 
                : result.ToString(Formatting.None));
        }

        /// <summary>
        /// 写入原始 JSON
        /// </summary>
        public static void WriteJson(JObject json)
        {
            Console.WriteLine(_prettyPrint 
                ? json.ToString(Formatting.Indented) 
                : json.ToString(Formatting.None));
        }

        /// <summary>
        /// 将字典列表转换为 JSON 友好的格式
        /// </summary>
        public static List<Dictionary<string, object>> ConvertRows(List<Dictionary<string, object>> rows)
        {
            return rows;
        }
    }
}
