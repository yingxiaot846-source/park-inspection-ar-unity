using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MvpArSubmitter
{
    /// <summary>S5.1 诊断用：屏幕左上角显示 AR 会话状态与平面数量。</summary>
    public class ARDebugHud : MonoBehaviour
    {
        private ARSession session;
        private ARPlaneManager planeManager;
        private ARCameraManager cameraManager;
        private string backend = ApiConfig.BaseUrl;

        private void Start()
        {
            session = Object.FindObjectOfType<ARSession>();
            planeManager = Object.FindObjectOfType<ARPlaneManager>();
            cameraManager = Object.FindObjectOfType<ARCameraManager>();
            Debug.Log("[ARDebugHud] session=" + (session != null) +
                      " planes=" + (planeManager != null) +
                      " camera=" + (cameraManager != null) +
                      " backend=" + backend);
        }

        private void OnGUI()
        {
            int planeCount = planeManager != null ? planeManager.trackables.count : -1;
            string sessionState = session != null ? ARSession.state.ToString() : "无ARSession";
            string cameraState = cameraManager != null ? "组件已存在" : "组件缺失";

            GUI.Box(new Rect(10, 10, 480, 132), "");
            GUI.Label(new Rect(20, 18, 460, 24), "S5.1 诊断面板");
            GUI.Label(new Rect(20, 44, 460, 24), "AR 会话: " + sessionState);
            GUI.Label(new Rect(20, 70, 460, 24), "相机组件: " + cameraState);
            GUI.Label(new Rect(20, 96, 460, 24), "检测到的平面: " + planeCount + " 个");
            GUI.Label(new Rect(20, 118, 460, 24), "后端: " + backend);
        }
    }
}