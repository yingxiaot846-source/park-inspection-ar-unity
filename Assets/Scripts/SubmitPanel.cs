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

        /// <summary>由 UI 按钮调用：设置优先级并刷新三个按钮的高亮。</summary>
        public void SetPriority(int index)
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
