using UnityEngine;
using UnityEngine.UI;

namespace MvpArSubmitter
{
    /// <summary>
    /// 结果行着色（只写 Text.color，文案/位置/尺寸/字号/对齐一律不动）：
    /// 缓存上次文案，仅在文案字符串变化时按固定顺序重新判定并写入：
    /// 1) 空文案或以「提交中…」开头 → #E5E5EA（中性）
    /// 2) 以「提交成功」开头 → #34C759（成功绿）
    /// 3) 以「请先点击地面放置标记」开头 → #FF9500（提示橙）
    /// 4) 其余 → #FF3B30（失败红）
    /// Text 引用由 SubmitUIBootstrap 传入（禁止运行时搜索）。
    /// </summary>
    public class ResultTextColorizer : MonoBehaviour
    {
        /// <summary>中性 #E5E5EA。</summary>
        private static readonly Color Neutral = new Color32(0xE5, 0xE5, 0xEA, 0xFF);

        /// <summary>成功 #34C759。</summary>
        private static readonly Color Success = new Color32(0x34, 0xC7, 0x59, 0xFF);

        /// <summary>放置提示 #FF9500。</summary>
        private static readonly Color PlacementHint = new Color32(0xFF, 0x95, 0x00, 0xFF);

        /// <summary>失败 #FF3B30。</summary>
        private static readonly Color Failure = new Color32(0xFF, 0x3B, 0x30, 0xFF);

        private Text target;
        private string lastText;

        /// <summary>由 SubmitUIBootstrap 传入结果行引用（禁止运行时搜索）。</summary>
        public void Bind(Text resultText)
        {
            target = resultText;
            lastText = null;
            Apply();
        }

        private void Update()
        {
            if (target == null || target.text == lastText)
            {
                return;
            }
            Apply();
        }

        private void Apply()
        {
            if (target == null)
            {
                return;
            }
            lastText = target.text;
            target.color = Resolve(lastText);
        }

        private static Color Resolve(string text)
        {
            if (string.IsNullOrEmpty(text) || text.StartsWith("提交中…"))
            {
                return Neutral;
            }
            if (text.StartsWith("提交成功"))
            {
                return Success;
            }
            if (text.StartsWith("请先点击地面放置标记"))
            {
                return PlacementHint;
            }
            return Failure;
        }
    }
}
