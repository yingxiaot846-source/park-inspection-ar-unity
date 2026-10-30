using UnityEngine;
using UnityEngine.UI;

namespace MvpArSubmitter
{
    /// <summary>
    /// 提交面板：标题/描述/优先级 → POST /api/issues。
    /// 本阶段坐标为假坐标常量（S5 接入 AR 后替换为射线检测的真实命中点）。
    /// </summary>
    public class SubmitPanel : MonoBehaviour
    {
        // 假坐标常量：本阶段唯一允许的 position 来源。
        public const float FakeX = 1.0f;
        public const float FakeY = 0.0f;
        public const float FakeZ = 1.0f;

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

        public void OnClickSubmit()
        {
            // 防重复点击：提交中按钮已禁用，这里再同步拦一道。
            if (!submitButton.interactable)
            {
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

            CreateIssueRequest request = new CreateIssueRequest
            {
                title = titleInput.text.Trim(),
                description = descInput.text,
                priority = PriorityValues[priorityIndex],
                position = new Vector3Payload { x = FakeX, y = FakeY, z = FakeZ },
            };

            submitButton.interactable = false;
            SetResult("提交中…");
            IssueApi.Submit(this, request, OnSubmitFinished);
        }

        private void OnSubmitFinished(SubmitResult result)
        {
            submitButton.interactable = true;
            if (result.success)
            {
                SetResult("提交成功");
                titleInput.text = string.Empty;
                descInput.text = string.Empty;
                Debug.Log("[SubmitPanel] 提交成功 id=" + result.issue.id +
                          " createdAt=" + result.issue.createdAt);
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
