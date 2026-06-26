using System.Collections.Generic;

namespace K3CloudDataDictionary.Cli
{
    /// <summary>
    /// 字典扩展方法（兼容 .NET Framework 4.7.2）
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// 获取字典中指定键的值，如果键不存在则返回默认值
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}
