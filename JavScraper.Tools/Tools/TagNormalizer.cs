using JavScraper.Tools.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace JavScraper.Tools.Tools
{
    /// <summary>
    /// Tag/Genre 标准化工具：根据配置对列表中的词条执行替换和清理。
    /// </summary>
    internal static class TagNormalizer
    {
        /// <summary>
        /// 对 tag/genre 列表应用替换映射和移除规则。
        /// </summary>
        /// <param name="tags">原始标签列表。</param>
        /// <param name="config">Tag 替换/清理配置。</param>
        /// <returns>处理后去重的标签列表。</returns>
        public static List<string> Normalize(List<string> tags, TagReplaceConfig config)
        {
            if (tags == null || tags.Count == 0)
                return tags ?? new List<string>();

            if (!config.Enabled)
                return tags;

            // 建立别名 -> 目标名称映射（大小写不敏感）
            var aliasToTarget = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var kv in config.Replacements ?? new Dictionary<string, List<string>>())
            {
                var target = kv.Key?.Trim();
                if (string.IsNullOrEmpty(target))
                    continue;

                foreach (var alias in kv.Value ?? new List<string>())
                {
                    if (string.IsNullOrWhiteSpace(alias))
                        continue;

                    var a = alias.Trim();
                    if (!aliasToTarget.ContainsKey(a))
                        aliasToTarget[a] = target;
                }

                // 目标名称本身也映射到自身，防止被其他别名覆盖
                if (!aliasToTarget.ContainsKey(target))
                    aliasToTarget[target] = target;
            }

            // 构建 RemoveTerms 集合（大小写不敏感）
            var removeSet = new HashSet<string>(
                config.RemoveTerms ?? new List<string>(),
                System.StringComparer.OrdinalIgnoreCase);

            var result = new List<string>();
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                var trimmed = tag.Trim();

                // 先执行替换
                var normalized = aliasToTarget.TryGetValue(trimmed, out var replaced) ? replaced : trimmed;

                // 移除规则（替换后再判断）
                if (removeSet.Contains(normalized))
                    continue;

                if (!string.IsNullOrWhiteSpace(normalized) && !result.Contains(normalized))
                    result.Add(normalized);
            }

            return result;
        }
    }
}
