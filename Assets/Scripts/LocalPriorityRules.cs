using System;

namespace MvpArSubmitter
{
    /// <summary>Bonus 2 本地判级建议结果：priority 与后端契约同形（low/medium/high）。</summary>
    [Serializable]
    public class PrioritySuggestion
    {
        public string priority;
        public string reason;
        public bool matched;
    }

    /// <summary>
    /// Bonus 2：轻量本地规则智能判级。纯 C# 静态类——零 Unity 依赖、零网络、零外部 AI。
    /// 可解释：命中即给出原因与命中词；可回退：未命中/异常一律 matched=false，由调用方保持用户选择。
    /// 冲突策略：标题与描述共同参与每一级判断——任一处命中 high 即 high；
    /// 无 high 而任一处命中 medium 即 medium；无 high/medium 而任一处命中 low 即 low。
    /// 同一等级标题与描述都命中时，优先用标题中的匹配词解释原因（只影响原因文案，不改变等级）。
    /// </summary>
    public static class LocalPriorityRules
    {
        // 词表按园区巡检语境克制取词；增删关键词只改这几组数组即可。
        private static readonly string[] HighKeywords =
            { "漏电", "火灾", "燃气", "触电", "坍塌", "坠落", "人身", "危险", "紧急", "重大", "砸", "急" };

        private static readonly string[] MediumKeywords =
            { "故障", "异常", "损坏", "渗水", "漏水", "噪音", "失灵", "停用", "卡住", "影响使用", "无法" };

        private static readonly string[] LowKeywords =
            { "划痕", "轻微", "建议", "优化", "观感", "灰尘", "清洁", "摆放", "标识不清", "美化" };

        /// <summary>对标题+描述做规则评估。绝不抛异常；未命中返回 matched=false。</summary>
        public static PrioritySuggestion Evaluate(string title, string description)
        {
            try
            {
                string t = Normalize(title);
                string d = Normalize(description);
                if (t.Length == 0 && d.Length == 0)
                {
                    return new PrioritySuggestion { matched = false, priority = "", reason = "" };
                }
                // 标题与描述共同参与每一级判断：任一处命中该级词表即为该级；
                // 同级双命中时优先取标题中的匹配词解释原因（不影响等级）。
                PrioritySuggestion hit =
                    ScanLevel(t, HighKeywords, "high", "涉及安全风险，建议优先处理") ??
                    ScanLevel(d, HighKeywords, "high", "涉及安全风险，建议优先处理") ??
                    ScanLevel(t, MediumKeywords, "medium", "设备或设施异常，影响正常使用") ??
                    ScanLevel(d, MediumKeywords, "medium", "设备或设施异常，影响正常使用") ??
                    ScanLevel(t, LowKeywords, "low", "低影响问题，可计划处理") ??
                    ScanLevel(d, LowKeywords, "low", "低影响问题，可计划处理");
                if (hit == null)
                {
                    return new PrioritySuggestion { matched = false, priority = "", reason = "未命中关键词" };
                }
                return hit;
            }
            catch (Exception)
            {
                // 规则异常绝不能影响提交流程：回退为"无建议"。
                return new PrioritySuggestion { matched = false, priority = "", reason = "" };
            }
        }

        // 归一化：去首尾空白 + 英文转小写。中文子串匹配对标点/空格天然免疫，不做分词。
        private static string Normalize(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : text.Trim().ToLowerInvariant();
        }

        private static PrioritySuggestion ScanLevel(string text, string[] words, string priority, string reason)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (text.Contains(words[i]))
                {
                    return new PrioritySuggestion
                    {
                        matched = true,
                        priority = priority,
                        reason = reason + "（命中:" + words[i] + "）",
                    };
                }
            }
            return null;
        }
    }
}
