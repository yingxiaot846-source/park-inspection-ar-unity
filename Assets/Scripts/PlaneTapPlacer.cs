using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MvpArSubmitter
{
    /// <summary>
    /// 点击平面放置标记：手指（或编辑器鼠标）点 AR 平面 → 在命中点放/移动一个立方体标记。
    /// 三重拦截：EventSystem UI / 提交面板矩形 / 左上角诊断 HUD 区域。
    /// 单标记策略：全局只保留一个 PlacementMarker。
    /// </summary>
    public class PlaneTapPlacer : MonoBehaviour
    {
        public static PlaneTapPlacer Instance;

        public bool HasMarker { get { return marker != null; } }

        public Vector3 MarkerPosition { get { return marker != null ? marker.transform.position : Vector3.zero; } }

        /// <summary>最近一次点击的结果，供 ARDebugHud 显示："放置"/"UI拦截"/"未命中"。</summary>
        public string LastTapInfo = "无";

        /// <summary>提交面板矩形，由 Bootstrap 注入，用于第二重拦截（Canvas 为 ScreenSpaceOverlay）。</summary>
        public RectTransform panelRect;

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
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    HandleTap(touch.position, touch.fingerId);
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // 编辑器里用鼠标调试。
                HandleTap(Input.mousePosition, -1);
            }
        }

        /// <summary>三重拦截 → 射线检测 → 放置/移动标记。pointerId&lt;0 表示鼠标（EventSystem 用无参重载）。</summary>
        private void HandleTap(Vector2 screenPos, int pointerId)
        {
            // 拦截 a：点在任意 UI 元素上。
            if (EventSystem.current != null)
            {
                bool overUi = pointerId < 0
                    ? EventSystem.current.IsPointerOverGameObject()
                    : EventSystem.current.IsPointerOverGameObject(pointerId);
                if (overUi)
                {
                    LastTapInfo = "UI拦截";
                    Debug.Log("[Tap] 拦截:UI 上（EventSystem）");
                    return;
                }
            }

            // 拦截 b：点在提交面板矩形内（Overlay 画布第三参传 null）。
            if (panelRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPos, null))
            {
                LastTapInfo = "UI拦截";
                Debug.Log("[Tap] 拦截:提交面板区域内");
                return;
            }

            // 拦截 c：左上角诊断 HUD 区域（避免点 HUD 时误放标记）。
            if (screenPos.x < 500f && screenPos.y > Screen.height - 160f)
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
                if (renderer != null)
                {
                    renderer.material.color = Color.yellow;
                }
            }

            marker.transform.position = position;
            LastTapInfo = "放置";
            Debug.Log("[Tap] 放置标记 " + position);
        }
    }
}
