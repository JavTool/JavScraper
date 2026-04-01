using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JavScraper.Tools.Tools
{
    /// <summary>
    /// 演员名称替换工具
    /// </summary>
    internal static class ActorNameReplacer
    {
        /// <summary>
        /// 根据配置替换演员列表中的别名为统一名称。
        /// </summary>
        /// <param name="actors">原始演员列表（会返回新的列表，不修改原列表）</param>
        /// <param name="replacements">键为目标名称，值为该目标名称的一组别名</param>
        /// <returns>替换后的演员列表</returns>
        public static List<string> ReplaceActors(List<string> actors, Dictionary<string, List<string>> replacements)
        {
            if (actors == null || actors.Count == 0 || replacements == null || replacements.Count == 0)
                return actors ?? [];

            // 建立别名->目标映射以便快速查找
            var aliasToTarget = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in replacements)
            {
                var target = kv.Key?.Trim();
                if (string.IsNullOrEmpty(target))
                    continue;

                foreach (var alias in kv.Value ?? [])
                {
                    if (string.IsNullOrEmpty(alias))
                        continue;

                    var a = alias.Trim();
                    if (!aliasToTarget.ContainsKey(a))
                        aliasToTarget[a] = target;
                }

                // 也把目标名称自身映射到自己，防止重复替换为其他别名
                if (!aliasToTarget.ContainsKey(target))
                    aliasToTarget[target] = target;
            }

            var result = new List<string>();

            foreach (var actor in actors)
            {
                if (string.IsNullOrWhiteSpace(actor))
                    continue;

                var trimmed = actor.Trim();

                // 移除括号内的注释（如演员后面的额外信息）
                var normalized = Regex.Replace(trimmed, "\\s*（.*?）\\s*", string.Empty);

                if (aliasToTarget.TryGetValue(normalized, out var targetName))
                {
                    if (!result.Contains(targetName))
                        result.Add(targetName);
                }
                else
                {
                    if (!result.Contains(normalized))
                        result.Add(normalized);
                }
            }

            return result;
        }
    }
}
