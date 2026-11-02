using UnityEngine;
using UnityEngine.UI;

namespace MvpArSubmitter
{
    /// <summary>
    /// 优先级三按钮颜色一致性校正（只写 Image.color 与 Text.color）：
    /// 只读 SubmitPanel.priorityIndex——禁止赋值、禁止调用 SubmitPanel 的任何方法。
    /// LateUpdate 中计算三项目标色，与当前值逐分量比较，任一差值 &gt; 0.004 才写入，
    /// 避免每帧无条件写造成的持续 dirty 与潜在闪烁；
    /// 无论 SubmitPanel.ApplyPriority / SetPriorityInitial 何时覆盖，下一帧即被纠正。
    /// 引用由 SubmitUIBootstrap 创建后直接传入（禁止运行时搜索），文字引用取一次并缓存。
    /// </summary>
    public class PriorityButtonColorSync : MonoBehaviour
    {
        /// <summary>写入阈值：任一 RGBA 分量差值超过它才落笔。</summary>
        private const float Tolerance = 0.004f;

        /// <summary>选中底色 #0071E3。</summary>
        private static readonly Color SelectedBg = new Color32(0x00, 0x71, 0xE3, 0xFF);

        /// <summary>未选中底色 rgba(255,255,255,0.10)。</summary>
        private static readonly Color UnselectedBg = new Color(1f, 1f, 1f, 0.10f);

        /// <summary>选中文字 #FFFFFF。</summary>
        private static readonly Color SelectedText = Color.white;

        /// <summary>未选中文字 #E5E5EA。</summary>
        private static readonly Color UnselectedText = new Color32(0xE5, 0xE5, 0xEA, 0xFF);

        private SubmitPanel panel;
        private Image[] backgrounds;
        private Text[] labels;

        /// <summary>由 SubmitUIBootstrap 创建后直接传入全部引用（禁止 Find / FindObjectOfType）。</summary>
        public void Bind(SubmitPanel targetPanel, Image[] buttonImages, Text[] buttonTexts)
        {
            panel = targetPanel;
            backgrounds = buttonImages;
            labels = buttonTexts;
        }

        private void LateUpdate()
        {
            if (panel == null || backgrounds == null || labels == null)
            {
                return;
            }

            int index = panel.priorityIndex; // 只读，不写入
            for (int i = 0; i < backgrounds.Length; i++)
            {
                bool selected = i == index;
                Color targetBg = selected ? SelectedBg : UnselectedBg;
                Color targetText = selected ? SelectedText : UnselectedText;

                Image background = backgrounds[i];
                if (background != null && Differs(background.color, targetBg))
                {
                    background.color = targetBg;
                }

                Text label = labels[i];
                if (label != null && Differs(label.color, targetText))
                {
                    label.color = targetText;
                }
            }
        }

        private static bool Differs(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) > Tolerance
                || Mathf.Abs(a.g - b.g) > Tolerance
                || Mathf.Abs(a.b - b.b) > Tolerance
                || Mathf.Abs(a.a - b.a) > Tolerance;
        }
    }
}
