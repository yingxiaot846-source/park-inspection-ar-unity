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
        private string backendState = "检测中…";

        private void Start()
        {
            session = Object.FindObjectOfType<ARSession>();
            planeManager = Object.FindObjectOfType<ARPlaneManager>();
            cameraManager = Object.FindObjectOfType<ARCameraManager>();
            Debug.Log("[ARDebugHud] session=" + (session != null) +
                      " planes=" + (planeManager != null) +
                      " camera=" + (cameraManager != null) +
                      " backend=" + backend);
            StartCoroutine(CheckBackendLoop());
        }

        private IEnumerator CheckBackendLoop()
        {
            var wait = new WaitForSeconds(5f);
            while (true)
            {
                using (UnityWebRequest req = UnityWebRequest.Get(backend + "/api/health"))
                {
                    req.timeout = 5;
                    yield return req.SendWebRequest();
                    backendState = req.result == UnityWebRequest.Result.Success
                        ? "已连通"
                        : "未连通(" + req.error + ")";
                }
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