using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MvpArSubmitter
{
    /// <summary>
    /// 点击平面放置标记：手指（或编辑器鼠标）点 AR 平面 → 在命中点放/移动一个立方体标记。
    /// 两重拦截：EventSystem UI 射线（RaycastAll）/ 左上角诊断 HUD 区域。
    /// 单标记策略：全局只保留一个 PlacementMarker。
    /// </summary>
    public class PlaneTapPlacer : MonoBehaviour
    {
        public static PlaneTapPlacer Instance;

        public bool HasMarker { get { return marker != null; } }

        public Vector3 MarkerPosition { get { return marker != null ? marker.transform.position : Vector3.zero; } }

        /// <summary>最近一次点击的结果，供 ARDebugHud 显示："放置"/"UI拦截"/"未命中"。</summary>
        public string LastTapInfo = "无";

        // —— 加分项 1：标记颜色按优先级/状态切换 ——
        // 颜色规则（固定）：high=红 medium=黄 low=绿；status=resolved → 灰色半透明（优先于优先级）。
        private static readonly Color PriorityHighColor = Color.red;
        private static readonly Color PriorityMediumColor = Color.yellow;
        private static readonly Color PriorityLowColor = Color.green;
        // 已解决：灰色 + 降低透明度。
        private static readonly Color ResolvedColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);

        // 当前样式状态：新放置的标记按此上色；null/空/未知一律回退黄色，不抛异常。
        private string currentPriority = "medium";
        private string currentStatus = "open";

        // 标记专属材质实例：绝不改 Resources/Marker.mat 共享资产。
        private Material markerMaterialInstance;

        /// <summary>优先级 → 颜色（high红/medium黄/low绿/未知黄）。</summary>
        public static Color ColorForPriority(string priority)
        {
            switch (priority)
            {
                case "high": return PriorityHighColor;
                case "low": return PriorityLowColor;
                default: return PriorityMediumColor; // medium、null、空、未知 → 黄
            }
        }

        /// <summary>颜色决策唯一入口：resolved 灰色半透明优先，否则按优先级。</summary>
        public static Color ColorForState(string priority, string status)
        {
            if (status == "resolved")
            {
                return ResolvedColor;
            }
            return ColorForPriority(priority);
        }

        /// <summary>记录当前选择的优先级，供随后放置的标记上色（由 SubmitPanel.SetPriority 调用）。</summary>
        public void SetCurrentPriority(string priority)
        {
            currentPriority = priority;
            if (marker != null && markerMaterialInstance != null)
            {
                markerMaterialInstance.color = ColorForState(currentPriority, currentStatus);
                Debug.Log("[PlaneTapPlacer] 标记样式更新 priority=" + currentPriority +
                          " status=" + currentStatus);
            }
        }

        /// <summary>提交成功后按后端返回的 priority/status 刷新标记颜色（由 SubmitPanel 调用）。</summary>
        public void ApplyIssueStyle(string priority, string status)
        {
            currentPriority = priority;
            currentStatus = status;
            if (markerMaterialInstance != null)
            {
                markerMaterialInstance.color = ColorForState(currentPriority, currentStatus);
                Debug.Log("[PlaneTapPlacer] 标记样式更新 priority=" + currentPriority +
                          " status=" + currentStatus);
            }
        }

        private ARRaycastManager raycastManager;
        private GameObject marker;

        private void Awake()
        {
            Instance = this;
            raycastManager = FindObjectOfType<ARRaycastManager>();
            if (raycastManager == null)
            {
                Debug.LogError("[PlaneTapPlacer] 场景里找不到 ARRaycastManager，已禁用自身（点击放置不可用）");
                enabled = false;
            }
        }

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                // 遍历全部手指：单手持机时最先按下的那根（握持手）会一直停在 Moved，
                // 只取 GetTouch(0) 会把真正的点击丢掉。任意一根进入 Began 即处理，一帧只处理一根。
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began)
                    {
                        HandleTap(touch.position, touch.fingerId);
                        break;
                    }
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // 编辑器里用鼠标调试。
                HandleTap(Input.mousePosition, -1);
            }
        }

        /// <summary>拦截 UI 射线与 HUD 区 → 平面射线 → 放置/移动标记。</summary>
        private void HandleTap(Vector2 screenPos, int pointerId)
        {
            // 拦截 a：点在任意 UI 元素上（RaycastAll，比 IsPointerOverGameObject 对 uGUI 更可靠）。
            if (EventSystem.current != null)
            {
                PointerEventData ped = new PointerEventData(EventSystem.current);
                ped.position = screenPos;
                List<RaycastResult> uiHits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(ped, uiHits);
                if (uiHits.Count > 0)
                {
                    LastTapInfo = "UI拦截(raycast)";
                    Debug.Log("[Tap] 拦截:UI 射线命中 " + uiHits.Count + " 个");
                    return;
                }
            }

            // 拦截 c：左上角诊断 HUD 区域（避免点 HUD 时误放标记）。
            if (screenPos.x < 500f && screenPos.y > Screen.height - 200f)
            {
                LastTapInfo = "UI拦截";
                Debug.Log("[Tap] 拦截:左上角诊断面板区域");
                return;
            }

            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            bool ok = raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon);
            if (!ok || hits.Count == 0)
            {
                LastTapInfo = "未命中";
                Debug.Log("[Tap] 未命中平面");
                return;
            }

            Pose pose = hits[0].pose;
            Vector3 position = pose.position + Vector3.up * 0.04f; // 让方块"坐在"平面上，不半埋

            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "PlacementMarker";
                marker.transform.localScale = Vector3.one * 0.08f;
                Renderer renderer = marker.GetComponent<Renderer>();
                Material markerMat = Resources.Load<Material>("Marker");
                if (markerMat != null)
                {
                    // 只取共享材质做模板，克隆出标记专属实例再改色——共享资产颜色永不变。
                    markerMaterialInstance = new Material(markerMat);
                    markerMaterialInstance.color = ColorForState(currentPriority, currentStatus);
                    renderer.sharedMaterial = markerMaterialInstance;
                    Debug.Log("[PlaneTapPlacer] 标记创建，样式 priority=" + currentPriority +
                              " status=" + currentStatus);
                }
                else
                {
                    Debug.LogError("[PlaneTapPlacer] Resources/Marker.mat 未找到");
                }
            }

            marker.transform.position = position;
            LastTapInfo = "放置";
            Debug.Log("[Tap] 放置标记 " + position);
        }
    }
}
