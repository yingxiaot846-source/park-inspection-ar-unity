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

        // —— FIX-1：诊断上屏数据（每次 RefreshMarkerStyle 之后刷新，ARDebugHud 每帧读取，只读不回写）。
        public string DiagMaterial = "材质: 未初始化";
        public string DiagColor = "颜色: -";
        public string DiagRender = "渲染: -";
        public string DiagShader = "Shader: -";
        public string DiagInstance = "实例: -";
        public string DiagFallback = "兜底: 无";

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

        /// <summary>
        /// 材质样式唯一应用入口：颜色 + Standard Shader 透明/不透明渲染状态。
        /// 只允许作用于 markerMaterialInstance（克隆实例），绝不改 Resources/Marker.mat 资产。
        /// 仅改 alpha 在 Opaque 模式下无效，必须同步切 Blend/ZWrite/Keyword/renderQueue。
        /// </summary>
        private void ApplyMarkerVisualStyle(Color color, bool resolved)
        {
            if (markerMaterialInstance == null)
            {
                return;
            }
            markerMaterialInstance.color = color;
            if (resolved)
            {
                // 透明模式：SrcAlpha / OneMinusSrcAlpha 混合，关 ZWrite，进透明队列。
                markerMaterialInstance.SetFloat("_Mode", 3f);
                markerMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);      // 5
                markerMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // 10
                markerMaterialInstance.SetInt("_ZWrite", 0);
                markerMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
                markerMaterialInstance.DisableKeyword("_ALPHATEST_ON");
                markerMaterialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                markerMaterialInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 3000
            }
            else
            {
                // 不透明模式：恢复 Marker.mat 默认（_Mode=0, Src=1, Dst=0, ZWrite=1, Geometry 2000）。
                markerMaterialInstance.SetFloat("_Mode", 0f);
                markerMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);  // 1
                markerMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero); // 0
                markerMaterialInstance.SetInt("_ZWrite", 1);
                markerMaterialInstance.DisableKeyword("_ALPHABLEND_ON");
                markerMaterialInstance.DisableKeyword("_ALPHATEST_ON");
                markerMaterialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                markerMaterialInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry; // 2000
            }
        }

        /// <summary>按当前 currentPriority/currentStatus 刷新实例样式（内部统一走 ApplyMarkerVisualStyle）。</summary>
        private void RefreshMarkerStyle()
        {
            ApplyMarkerVisualStyle(ColorForState(currentPriority, currentStatus), currentStatus == "resolved");
            DiagFallback = "兜底: 无";
            ApplyAntiBlackFallback();
            RefreshDiagnostics();
        }

        // FIX-1 防黑兜底：只操作运行时材质实例，绝不修改 Resources/Marker.mat 资产，绝不改共享材质本身。
        private void ApplyAntiBlackFallback()
        {
            if (marker == null)
            {
                return;
            }
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }
            // 兜底 1：已放置标记但实例丢失 → 从 renderer.sharedMaterial 克隆重建并挂回渲染器。
            if (markerMaterialInstance == null)
            {
                Material baseMat = renderer.sharedMaterial;
                if (baseMat == null)
                {
                    DiagFallback = "材质: NULL→无法重建";
                    return;
                }
                markerMaterialInstance = new Material(baseMat);
                renderer.material = markerMaterialInstance;
                ApplyMarkerVisualStyle(ColorForState(currentPriority, currentStatus), currentStatus == "resolved");
                DiagFallback = "材质: NULL→已重建";
                return;
            }
            // 非 resolved 才做颜色与渲染状态兜底；resolved 灰色半透明完全豁免。
            if (currentStatus == "resolved")
            {
                return;
            }
            // 兜底 2：非 resolved 且颜色最大通道 < 0.05（近黑）→ 改用 medium 黄。
            Color c = markerMaterialInstance.color;
            float maxChannel = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (maxChannel < 0.05f)
            {
                markerMaterialInstance.color = PriorityMediumColor;
                DiagFallback = "颜色回退:黄";
            }
            // 兜底 3：非 resolved 强制不透明渲染状态（与 ApplyMarkerVisualStyle else 分支一致，防被外部覆盖）。
            markerMaterialInstance.SetFloat("_Mode", 0f);
            markerMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            markerMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            markerMaterialInstance.SetInt("_ZWrite", 1);
            markerMaterialInstance.DisableKeyword("_ALPHABLEND_ON");
            markerMaterialInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }

        // FIX-1 颜色诊断：只读取渲染器当前共享材质（即实例），不修改任何材质。
        private void RefreshDiagnostics()
        {
            Renderer renderer = marker != null ? marker.GetComponent<Renderer>() : null;
            Material mat = renderer != null ? renderer.sharedMaterial : null;
            if (mat == null)
            {
                DiagMaterial = "材质: NULL";
                DiagColor = "颜色: -";
                DiagRender = "渲染: -";
                DiagShader = "Shader: -";
                DiagInstance = "实例: -";
                return;
            }
            DiagMaterial = "材质: OK";
            Color c = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.clear;
            DiagColor = "颜色: #" + ColorUtility.ToHtmlStringRGB(c);
            DiagRender = "渲染: Mode=" + (mat.HasProperty("_Mode") ? mat.GetFloat("_Mode").ToString() : "?") +
                         " Src=" + (mat.HasProperty("_SrcBlend") ? mat.GetFloat("_SrcBlend").ToString() : "?") +
                         " Dst=" + (mat.HasProperty("_DstBlend") ? mat.GetFloat("_DstBlend").ToString() : "?") +
                         " ZW=" + (mat.HasProperty("_ZWrite") ? mat.GetFloat("_ZWrite").ToString() : "?") +
                         " Q=" + mat.renderQueue;
            DiagShader = "Shader: " + (mat.shader != null ? mat.shader.name : "null") +
                         " supported=" + (mat.shader != null && mat.shader.isSupported);
            DiagInstance = "实例: " + mat.name + " id=" + mat.GetInstanceID();
        }

        /// <summary>记录当前选择的优先级，供随后放置的标记上色（由 SubmitPanel.SetPriority 调用）。</summary>
        public void SetCurrentPriority(string priority)
        {
            currentPriority = priority;
            if (marker != null && markerMaterialInstance != null)
            {
                RefreshMarkerStyle();
                Debug.Log("[PlaneTapPlacer] 标记样式更新 priority=" + currentPriority +
                          " status=" + currentStatus);
            }
        }

        /// <summary>提交成功后按后端返回的 priority/status 刷新标记样式（由 SubmitPanel 调用）。</summary>
        public void ApplyIssueStyle(string priority, string status)
        {
            currentPriority = priority;
            currentStatus = status;
            if (markerMaterialInstance != null)
            {
                RefreshMarkerStyle();
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
            // FIX-1：面板扩到 15 行（Box 330 高），拦截下限从 height-200 同步扩到 height-350。
            if (screenPos.x < 500f && screenPos.y > Screen.height - 350f)
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

            // 新一轮上报：真实命中平面才重置状态为 open（保留用户当前选择的优先级），
            // 防止旧问题的 resolved 灰色半透明样式污染新放置的标记；提交失败时不动。
            if (currentStatus != "open")
            {
                currentStatus = "open";
                Debug.Log("[PlaneTapPlacer] 命中新平面，样式状态重置为 open（priority 保持 " + currentPriority + "）");
            }

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
                    RefreshMarkerStyle();
                    renderer.sharedMaterial = markerMaterialInstance;
                    RefreshDiagnostics(); // FIX-1：赋值后再刷新一次诊断（否则读到的是赋值前的默认材质）
                    Debug.Log("[PlaneTapPlacer] 标记创建，样式 priority=" + currentPriority +
                              " status=" + currentStatus);
                }
                else
                {
                    Debug.LogError("[PlaneTapPlacer] Resources/Marker.mat 未找到");
                }
            }
            else
            {
                // 标记已存在（单标记复用，位置即将更新）：按重置后的状态重新应用样式。
                RefreshMarkerStyle();
            }

            marker.transform.position = position;
            LastTapInfo = "放置";
            Debug.Log("[Tap] 放置标记 " + position);
        }
    }
}
