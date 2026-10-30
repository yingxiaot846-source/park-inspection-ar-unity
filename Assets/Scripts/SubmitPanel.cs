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
        public Dropdown priorityDropdown;
        public Button submitButton;
        public Text resultText;

        // Dropdown 选项下标 → 契约 priority 枚举（低/中/高）。
        private static readonly string[] PriorityValues = { "low", "medium", "high" };

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

            int priorityIndex = priorityDropdown.value;
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
