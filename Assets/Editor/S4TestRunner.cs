using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MvpArSubmitter
{
    /// <summary>
    /// S4 自动化验证入口：
    /// Unity.exe -batchmode -quit -executeMethod S4TestRunner.Run -logFile -
    /// 真实 POST 一条带时间戳的 issue（假坐标），把结果写入 Logs/s4-test.log，
    /// 成功退出码 0，失败退出码 1。
    /// </summary>
    public static class S4TestRunner
    {
        private const string LogPath = "Logs/s4-test.log";

        public static void Run()
        {
            StringBuilder log = new StringBuilder();
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string title = "S4-自动测试-" + stamp;
            int before = FetchTotal();

            CreateIssueRequest request = new CreateIssueRequest
            {
                title = title,
                description = "S4 batchmode 自动化验证（假坐标提交）",
                priority = "medium",
                position = new Vector3Payload { x = 1.0f, y = 0.0f, z = 1.0f },
            };

            SubmitResult result = IssueApi.SubmitSync(request);
            int after = FetchTotal();

            log.AppendLine("=== S4 自动化验证 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
            log.AppendLine("请求标题: " + title);
            log.AppendLine("请求体: " + JsonUtility.ToJson(request));
            log.AppendLine("HTTP 状态码: " + result.statusCode);
            log.AppendLine("成功: " + result.success);
            log.AppendLine("后端返回 JSON: " + result.rawBody);
            log.AppendLine("issues.json 对应条数: 前=" + before + " 后=" + after);
            if (!result.success)
            {
                log.AppendLine("错误消息: " + result.errorMessage);
            }

            string summary = log.ToString();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(LogPath, summary, Encoding.UTF8);
            Debug.Log("[S4TestRunner]\n" + summary);

            EditorApplication.Exit(result.success && after == before + 1 ? 0 : 1);
        }

        // 用 GET /api/issues 的 total 字段做条数对比（与提交共用同一后端地址出处 ApiConfig）。
        private static int FetchTotal()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(ApiConfig.BaseUrl + "/api/issues"))
            {
                request.timeout = 8;
                IssueApi.WaitBounded(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    return -1;
                }
                TotalEnvelope parsed = JsonUtility.FromJson<TotalEnvelope>(request.downloadHandler.text);
                return parsed != null ? parsed.total : -1;
            }
        }

        [Serializable]
        private class TotalEnvelope
        {
            public int total;
        }
    }
}
