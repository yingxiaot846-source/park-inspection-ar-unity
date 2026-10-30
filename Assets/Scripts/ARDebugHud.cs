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
                UnityWebRequest req = UnityWebRequest.Get(url);
                req.timeout = 3;
                UnityWebRequestAsyncOperation op = req.SendWebRequest();
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

            GUI.Box(new Rect(10, 10, 480, 190), "");
            GUI.Label(new Rect(20, 14, 460, 20), "S5.3 诊断面板");
            GUI.Label(new Rect(20, 34, 460, 20), "AR 会话: " + sessionState);
            GUI.Label(new Rect(20, 54, 460, 20), "相机组件: " + cameraState);
            GUI.Label(new Rect(20, 74, 460, 20), "检测到的平面: " + planeCount + " 个");
            GUI.Label(new Rect(20, 94, 460, 20), "后端: " + backend);
            GUI.Label(new Rect(20, 114, 460, 20), "后端连通: " + backendState);
            GUI.Label(new Rect(20, 134, 460, 20), markerState);
            GUI.Label(new Rect(20, 154, 460, 20), lastTap);
        }
    }
}