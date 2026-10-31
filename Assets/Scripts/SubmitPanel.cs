using UnityEngine;
using UnityEngine.UI;

namespace MvpArSubmitter
{
    /// <summary>
    /// 提交面板：标题/描述/优先级 → POST /api/issues。
    /// 坐标来源：PlaneTapPlacer 放置的标记（真实 AR 平面命中点），未放置标记不允许提交。
    /// </summary>
    public class SubmitPanel : MonoBehaviour
    {
        [Header("场景引用（由 SceneBuilder 绑定）")]
        public InputField titleInput;
        public InputField descInput;
        // 优先级：0=低 1=中 2=高。由三按钮组在运行时写入，默认中。
        public int priorityIndex = 1;
        public Button submitButton;
        public Text resultText;

        // 选项下标 → 契约 priority 枚举（低/中/高）。
        private static readonly string[] PriorityValues = { "low", "medium", "high" };

        public Button[] priorityButtons;

        // 本次提交的坐标快照：请求发出前记录，回调时显示用（防止回调期间用户移动标记导致提示串味）。
        private Vector3 lastSubmittedPos;

        private static readonly Color PriorityNormalColor = new Color(0.2f, 0.45f, 0.8f, 1f);
        private static readonly Color PrioritySelectedColor = new Color(0.15f, 0.7f, 0.35f, 1f);

        // —— Bonus 2：本地规则智能判级 ——（建议只展示，采用才生效；提交仍走 priorityIndex）

        public Text suggestionText;
        public Button adoptButton;

        // 最近一次本地规则评估结果与"手动锁定"标记：手动点过低/中/高后停止自动刷新建议。
        private PrioritySuggestion lastSuggestion;
        private bool manualLocked;

        /// <summary>由 低/中/高 按钮调用：视为手动选择，停止自动建议刷新。</summary>
        public void SetPriority(int index)
        {
            manualLocked = true;
            ApplyPriority(index);
            RefreshSuggestionDisplay();
        }

        /// <summary>初始化默认值用：不高亮为手动锁定（Bootstrap 启动时调用）。</summary>
        public void SetPriorityInitial(int index)
        {
            ApplyPriority(index);
        }

        /// <summary>由"采用建议"按钮调用：采用当前建议等级（不视为手动锁定）。</summary>
        public void OnAdoptSuggestion()
        {
            if (lastSuggestion == null || !lastSuggestion.matched)
            {
                return;
            }
            int index = System.Array.IndexOf(PriorityValues, lastSuggestion.priority);
            if (index < 0)
            {
                return;
            }
            ApplyPriority(index);
            RefreshSuggestionDisplay();
        }

        /// <summary>输入变化回调（Bootstrap 经 onValueChanged 绑定）：同步评估并立即刷新建议。</summary>
        public void OnInputChanged()
        {
            EvaluateSuggestionNow();
        }

        // 实际评估：本地规则，零网络；异常兜底为"无建议"，绝不影响提交流程。
        private void EvaluateSuggestionNow()
        {
            try
            {
                lastSuggestion = LocalPriorityRules.Evaluate(titleInput.text, descInput.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SubmitPanel] 本地判级异常（已回退为无建议）: " + e);
                lastSuggestion = new PrioritySuggestion { matched = false, priority = "", reason = "" };
            }
            RefreshSuggestionDisplay();
        }

        private static string LabelOf(string priority)
        {
            switch (priority)
            {
                case "high": return "高";
                case "low": return "低";
                default: return "中";
            }
        }

        // 刷新建议展示：无建议/空输入显示引导文案并禁用"采用"按钮；不改动 priorityIndex。
        private void RefreshSuggestionDisplay()
        {
            if (suggestionText == null)
            {
                return;
            }
            bool titleEmpty = titleInput == null || string.IsNullOrWhiteSpace(titleInput.text);
            bool descEmpty = descInput == null || string.IsNullOrWhiteSpace(descInput.text);
            if (lastSuggestion == null || !lastSuggestion.matched)
            {
                suggestionText.text = (titleEmpty && descEmpty)
                    ? "输入标题或描述后给出建议"
                    : "暂无自动建议，请手动选择";
                if (adoptButton != null)
                {
                    adoptButton.interactable = false;
                }
                return;
            }
            string prefix = manualLocked ? "已手动选择｜建议" : "建议";
            suggestionText.text = prefix + ":" + LabelOf(lastSuggestion.priority) + " | " + lastSuggestion.reason;
            if (adoptButton != null)
            {
                adoptButton.interactable = true;
            }
        }

        /// <summary>设置优先级并刷新三个按钮的高亮（手动/初始化/采用共用的实际生效路径）。</summary>
        private void ApplyPriority(int index)
        {
            if (index < 0 || index >= PriorityValues.Length)
            {
                return;
            }
            priorityIndex = index;
            // 同步给放置器：随后放置的标记按当前选中优先级上色。
            var placerForColor = PlaneTapPlacer.Instance;
            if (placerForColor != null)
            {
                placerForColor.SetCurrentPriority(PriorityValues[index]);
            }
            if (priorityButtons == null)
            {
                return;
            }
            for (int i = 0; i < priorityButtons.Length; i++)
            {
                if (priorityButtons[i] == null)
                {
                    continue;
                }
                Image image = priorityButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = (i == index) ? PrioritySelectedColor : PriorityNormalColor;
                }
            }
        }

        // 坐标取 3 位小数，避免真机核对时看到一长串浮点噪声。
        private static float Round3(float value)
        {
            return (float)System.Math.Round((double)value, 3);
        }

        public void OnClickSubmit()
        {
            // 防重复点击：提交中按钮已禁用，这里再同步拦一道。
            if (!submitButton.interactable)
            {
                return;
            }

            var placer = PlaneTapPlacer.Instance;
            if (placer == null || !placer.HasMarker)
            {
                SetResult("请先点击地面放置标记");
                return;
            }

            if (string.IsNullOrWhiteSpace(titleInput.text))
            {
                SetResult("标题不能为空");
                return;
            }

            if (priorityIndex < 0 || priorityIndex >= PriorityValues.Length)
            {
                SetResult("优先级无效");
                return;
            }

            Vector3 markerPos = placer.MarkerPosition;
            CreateIssueRequest request = new CreateIssueRequest
            {
                title = titleInput.text.Trim(),
                description = descInput.text,
                priority = PriorityValues[priorityIndex],
                position = new Vector3Payload
                {
                    x = Round3(markerPos.x),
                    y = Round3(markerPos.y),
                    z = Round3(markerPos.z),
                },
            };

            submitButton.interactable = false;
            SetResult("提交中…");
            lastSubmittedPos = markerPos;
            IssueApi.Submit(this, request, OnSubmitFinished);
        }

        private void OnSubmitFinished(SubmitResult result)
        {
            submitButton.interactable = true;
            if (result.success)
            {
                SetResult("提交成功 @ (" + Round3(lastSubmittedPos.x) + ", " +
                          Round3(lastSubmittedPos.y) + ", " + Round3(lastSubmittedPos.z) + ")");
                titleInput.text = string.Empty;
                descInput.text = string.Empty;
                Debug.Log("[SubmitPanel] 提交成功 id=" + result.issue.id +
                          " createdAt=" + result.issue.createdAt +
                          " position=(" + Round3(lastSubmittedPos.x) + ", " +
                          Round3(lastSubmittedPos.y) + ", " + Round3(lastSubmittedPos.z) + ")");
                // 加分项 1：提交成功才按后端返回的 priority/status 刷新标记颜色；失败不动标记。
                var placerForStyle = PlaneTapPlacer.Instance;
                if (placerForStyle != null)
                {
                    placerForStyle.ApplyIssueStyle(result.issue.priority, result.issue.status);
                }
            }
            else
            {
                // 失败：显示后端 error.message 原文；连接失败为统一文案。保留用户输入。
                SetResult(result.errorMessage);
            }
        }

        private void SetResult(string message)
        {
            if (resultText != null)
            {
                resultText.text = message;
            }
        }
    }
}
