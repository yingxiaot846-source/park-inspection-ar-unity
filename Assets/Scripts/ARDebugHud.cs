using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;

namespace MvpArSubmitter
{
    /// <summary>S5.3 诊断用：屏幕左上角显示 AR 会话状态、平面数量与后端连通性。</summary>
    public class ARDebugHud : MonoBehaviour
    {
        private ARSession session;
        private ARPlaneManager planeManager;
        private ARCameraManager cameraManager;
        private string backend = ApiConfig.BaseUrl;
        // 后端连通状态：每 5 秒探测一次 /api/health。
        private string backendState = "探测#0";

        // FIX-1 必做1：提交结果双通道——SubmitPanel.SetResult 同步写这里，面板新增「最近结果:」行显示。
        public static string LastResult = "无";

        // FIX-1 必做3：启动 1 秒后计算 resultText 屏幕矩形并上屏（初值：未计算）。
        private string resultRectLine = "结果行: 未计算(启动1秒后)";

        private void Start()
        {
            session = Object.FindObjectOfType<ARSession>();
            planeManager = Object.FindObjectOfType<ARPlaneManager>();
            cameraManager = Object.FindObjectOfType<ARCameraManager>();
            Debug.Log("[ARDebugHud] session=" + (session != null) +
                      " planes=" + (planeManager != null) +
                      " camera=" + (cameraManager != null) +
                      " backend=" + backend);
            backendState = "探测已启动";
            StartCoroutine(CheckBackendLoop());
            StartCoroutine(ResultRectCheck());
        }

        // FIX-1 必做3：启动 1 秒后读 resultText 四角，换算屏幕矩形并判定在屏内/越界。
        // ScreenSpaceOverlay 画布的世界坐标即屏幕像素，GetWorldCorners 可直接当屏幕坐标用。
        private IEnumerator ResultRectCheck()
        {
            yield return new WaitForSecondsRealtime(1f);
            SubmitPanel panel = Object.FindObjectOfType<SubmitPanel>();
            if (panel == null || panel.resultText == null)
            {
                resultRectLine = "结果行: resultText=null 判定=无法计算";
                Debug.Log("[ARDebugHud] " + resultRectLine);
                yield break;
            }
            RectTransform rt = panel.resultText.rectTransform;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // GetWorldCorners 顺序：0=左下 1=左上 2=右上 3=右下。
            float left = corners[0].x;
            float bottom = corners[0].y;
            float top = corners[1].y;
            float right = corners[2].x;
            bool inScreen = left >= 0f && right <= Screen.width && top <= Screen.height && bottom >= 0f;
            resultRectLine = "结果行: L=" + Mathf.RoundToInt(left) + " R=" + Mathf.RoundToInt(right) +
                             " T=" + Mathf.RoundToInt(top) + " B=" + Mathf.RoundToInt(bottom) +
                             " 屏W=" + Screen.width + " 屏H=" + Screen.height +
                             " 判定=" + (inScreen ? "在屏内" : "越界");
            Debug.Log("[ARDebugHud] " + resultRectLine);
        }

        private const float ProbeTimeout = 3f;

        private IEnumerator CheckBackendLoop()
        {
            var wait = new WaitForSecondsRealtime(5f);
            int round = 0;
            while (true)
            {
                round++;
                string url = backend + "/api/health";
                backendState = "探测#" + round + " 中…";
                UnityWebRequest req = null;
                UnityWebRequestAsyncOperation op = null;
                bool sendFailed = false;
                try
                {
                    req = UnityWebRequest.Get(url);
                    req.timeout = 3;
                    op = req.SendWebRequest();
                }
                catch (System.Exception e)
                {
                    sendFailed = true;
                    backendState = "异常#" + round + " " + e.GetType().Name + ": " + e.Message;
                    Debug.LogError("[Hud] 探测#" + round + " 抛异常 " + e);
                    if (req != null) req.Dispose();
                }
                if (sendFailed)
                {
                    yield return wait;
                    continue;
                }
                if (op == null)
                {
                    backendState = "异常#" + round + " SendWebRequest 返回 null";
                    Debug.LogError("[Hud] 探测#" + round + " op=null url=" + url);
                    req.Dispose();
                    yield return wait;
                    continue;
                }
                float t0 = Time.realtimeSinceStartup;
                while (!op.isDone && Time.realtimeSinceStartup - t0 < ProbeTimeout)
                {
                    yield return null;
                }
                float cost = Time.realtimeSinceStartup - t0;
                if (!op.isDone)
                {
                    req.Abort();
                    backendState = "未连通(硬超时 " + cost.ToString("F1") + "s) #" + round;
                    Debug.LogError("[Hud] 探测#" + round + " 硬超时 " + cost.ToString("F2") + "s url=" + url);
                }
                else if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
                {
                    backendState = "已连通 #" + round + " " + cost.ToString("F1") + "s";
                    Debug.Log("[Hud] 探测#" + round + " OK " + cost.ToString("F2") + "s");
                }
                else
                {
                    backendState = "未连通(HTTP" + req.responseCode + " " + req.result + ") #" + round;
                    Debug.LogError("[Hud] 探测#" + round + " 失败 HTTP=" + req.responseCode +
                                   " result=" + req.result + " err=" + req.error);
                }
                req.Dispose();
                yield return wait;
            }
        }

        private void OnGUI()
        {
            int planeCount = planeManager != null ? planeManager.trackables.count : -1;
            string sessionState = session != null ? ARSession.state.ToString() : "无ARSession";
            string cameraState = cameraManager != null ? "组件已存在" : "组件缺失";

            PlaneTapPlacer placer = PlaneTapPlacer.Instance;
            string markerState = "标记: 未放置";
            string lastTap = "最近一次点击: 无";
            if (placer != null)
            {
                if (placer.HasMarker)
                {
                    Vector3 p = placer.MarkerPosition;
                    markerState = "标记: (" + p.x.ToString("F3") + ", " +
                                  p.y.ToString("F3") + ", " + p.z.ToString("F3") + ")";
                }
                lastTap = "最近一次点击: " + placer.LastTapInfo;
            }

            // FIX-1：Box 高度 190→330（容纳追加行）；原有 8 行的位置与格式不变。
            GUI.Box(new Rect(10, 10, 480, 330), "");
            GUI.Label(new Rect(20, 14, 460, 20), "S5.3 诊断面板");
            GUI.Label(new Rect(20, 34, 460, 20), "AR 会话: " + sessionState);
            GUI.Label(new Rect(20, 54, 460, 20), "相机组件: " + cameraState);
            GUI.Label(new Rect(20, 74, 460, 20), "检测到的平面: " + planeCount + " 个");
            GUI.Label(new Rect(20, 94, 460, 20), "后端: " + backend);
            GUI.Label(new Rect(20, 114, 460, 20), "后端连通: " + backendState);
            GUI.Label(new Rect(20, 134, 460, 20), markerState);
            GUI.Label(new Rect(20, 154, 460, 20), lastTap);
            // FIX-1 追加行（逐行追加，不改动上方原有各行）。
            GUI.Label(new Rect(20, 174, 470, 20), "最近结果: " + LastResult);
            GUI.Label(new Rect(20, 194, 470, 20), resultRectLine);
            if (placer != null)
            {
                GUI.Label(new Rect(20, 214, 470, 20), placer.DiagMaterial);
                GUI.Label(new Rect(20, 234, 470, 20), placer.DiagColor);
                GUI.Label(new Rect(20, 254, 470, 20), placer.DiagRender);
                GUI.Label(new Rect(20, 274, 470, 20), placer.DiagShader);
                GUI.Label(new Rect(20, 294, 470, 20), placer.DiagInstance);
                GUI.Label(new Rect(20, 314, 470, 20), placer.DiagFallback);
            }
            else
            {
                GUI.Label(new Rect(20, 214, 470, 20), "标记诊断: 无 PlaneTapPlacer");
            }
        }
    }
}