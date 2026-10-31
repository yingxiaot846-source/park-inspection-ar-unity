using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MvpArSubmitter
{
    /// <summary>一次提交的结果：成功携带完整 Issue；失败携带可读错误消息（绝不静默）。</summary>
    public class SubmitResult
    {
        public bool success;
        public long statusCode;
        public IssueResponse issue;
        public string errorMessage;
        public string rawBody;
    }

    /// <summary>
    /// POST /api/issues 的网络层。所有失败路径都产出带可读消息的结果，不做静默吞错。
    /// </summary>
    public static class IssueApi
    {
        /// <summary>连接失败 / 响应无法解析时的统一文案（与管理端一致）。</summary>
        public const string UnreachableMessage = "无法连接后端";

        /// <summary>运行时提交（Play 模式，由 MonoBehaviour 宿主承载协程）。</summary>
        public static Coroutine Submit(MonoBehaviour host, CreateIssueRequest data, Action<SubmitResult> onComplete)
        {
            return host.StartCoroutine(SubmitRoutine(data, onComplete));
        }

        /// <summary>编辑器 -executeMethod 同步提交（仅编辑器主线程使用）。</summary>
        public static SubmitResult SubmitSync(CreateIssueRequest data)
        {
            using (UnityWebRequest request = BuildCreateRequest(data))
            {
                WaitBounded(request);
                return Parse(request);
            }
        }

        /// <summary>
        /// 主线程阻塞等待请求结束。2022.3 没有提供同步等待方法，这里用有上限的轮询；
        /// 超过上限主动 Abort，保证 batchmode 绝不会无限挂住。
        /// </summary>
        public static bool WaitBounded(UnityWebRequest request, int timeoutMs = 15000)
        {
            UnityWebRequestAsyncOperation op = null;
            try
            {
                op = request.SendWebRequest();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Api] WaitBounded 发送异常 " + e);
                return false;
            }
            if (op == null)
            {
                Debug.LogError("[Api] WaitBounded op=null");
                return false;
            }
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            while (!op.isDone && sw.ElapsedMilliseconds < timeoutMs)
            {
                System.Threading.Thread.Sleep(5);
            }
            if (!op.isDone)
            {
                request.Abort();
                return false;
            }
            return true;
        }

        private static IEnumerator SubmitRoutine(CreateIssueRequest data, Action<SubmitResult> onComplete)
        {
            UnityWebRequest request = BuildCreateRequest(data);
            UnityWebRequestAsyncOperation op = null;
            bool sendFailed = false;
            try { op = request.SendWebRequest(); }
            catch (System.Exception e)
            {
                sendFailed = true;
                Debug.LogError("[Api] Submit 发送异常 " + e);
                request.Dispose();
                if (onComplete != null) onComplete(Fail(0, "请求异常: " + e.GetType().Name + " " + e.Message, null));
            }
            if (sendFailed)
            {
                yield break;
            }
            if (op == null)
            {
                request.Dispose();
                Debug.LogError("[Api] Submit op=null");
                if (onComplete != null) onComplete(Fail(0, "请求异常: SendWebRequest 返回 null", null));
                yield break;
            }
            // 不用 yield return op：部分安卓设备/Unity 版本下该回调可能永不到达。
            // 改为手写硬超时轮询，超时主动 Abort，宁可报「无法连接后端」也不能永久卡住。
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!op.isDone && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            if (!op.isDone)
            {
                request.Abort();
                request.Dispose();
                Debug.LogError("[IssueApi] 提交硬超时 12s，已 Abort URL=" + ApiConfig.BaseUrl + "/api/issues");
                if (onComplete != null) onComplete(Fail(0, UnreachableMessage, null));
                yield break;
            }
            SubmitResult result = Parse(request);
            request.Dispose();
            if (onComplete != null) onComplete(result);
        }

        // 提交与自动化验证共用同一份请求构建逻辑，保证发出去的 JSON 完全一致。
        private static UnityWebRequest BuildCreateRequest(CreateIssueRequest data)
        {
            string json = JsonUtility.ToJson(data);
            UnityWebRequest request = new UnityWebRequest(ApiConfig.BaseUrl + "/api/issues", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 8;
            return request;
        }

        private static SubmitResult Parse(UnityWebRequest request)
        {
            long statusCode = request.responseCode;
            string body = request.downloadHandler != null ? request.downloadHandler.text : null;

            // 断网 / 拒绝连接 / 8 秒超时：统一「无法连接后端」。
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return Fail(statusCode, UnreachableMessage, body);
            }

            if (statusCode >= 200 && statusCode <= 299)
            {
                IssueResponse issue = ParseJson<IssueResponse>(body);
                if (issue == null || string.IsNullOrEmpty(issue.id))
                {
                    // 2xx 但响应体缺失或无法解析（HTML/空）。
                    return Fail(statusCode, UnreachableMessage, body);
                }
                return new SubmitResult { success = true, statusCode = statusCode, issue = issue, rawBody = body };
            }

            // 非 2xx：优先取错误信封的 message 原文。
            ErrorEnvelope envelope = ParseJson<ErrorEnvelope>(body);
            if (envelope != null && envelope.error != null && !string.IsNullOrEmpty(envelope.error.message))
            {
                return Fail(statusCode, envelope.error.message, body);
            }
            // 响应不是 JSON 信封（HTML/空/代理错误页）。
            return Fail(statusCode, UnreachableMessage, body);
        }

        private static SubmitResult Fail(long statusCode, string message, string rawBody)
        {
            return new SubmitResult
            {
                success = false,
                statusCode = statusCode,
                issue = null,
                errorMessage = message,
                rawBody = rawBody,
            };
        }

        // JsonUtility 对非 JSON 输入会抛异常，这里兜住并返回 null，由调用方给出可读文案。
        private static T ParseJson<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
