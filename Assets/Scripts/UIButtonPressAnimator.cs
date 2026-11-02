using UnityEngine;
using UnityEngine.EventSystems;

namespace MvpArSubmitter
{
    /// <summary>
    /// 按钮按压缩放动效：
    /// 按下（OnPointerDown）目标缩放 0.97，过渡 90ms；
    /// 松开（OnPointerUp）或指针移出（OnPointerExit）目标缩放 1.0，过渡 140ms。
    /// Update 内用 Time.unscaledDeltaTime 按固定速度线性逼近目标值——不用协程、不用第三方库。
    /// 生命周期复位：OnDisable / OnDestroy 直接归位 Vector3.one 并把插值目标重置为 1.0。
    /// 只操作 localScale，不碰任何布局属性。
    /// </summary>
    public class UIButtonPressAnimator : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>按下目标缩放。</summary>
        private const float PressedScale = 0.97f;

        /// <summary>常态目标缩放。</summary>
        private const float NormalScale = 1f;

        /// <summary>按下过渡时长（秒）。</summary>
        private const float PressDuration = 0.09f;

        /// <summary>回弹过渡时长（秒）。</summary>
        private const float ReleaseDuration = 0.14f;

        /// <summary>当前缩放标量。</summary>
        private float currentScale = NormalScale;

        /// <summary>目标缩放标量。</summary>
        private float targetScale = NormalScale;

        /// <summary>每秒逼近速度（切换目标时按剩余距离 / 时长计算，保证时长精确）。</summary>
        private float approachSpeed;

        public void OnPointerDown(PointerEventData eventData)
        {
            BeginTransition(PressedScale, PressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            BeginTransition(NormalScale, ReleaseDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 指针移出走 140ms 插值回弹；若在此直接归位会吞掉手机端松手的回弹动画。
            BeginTransition(NormalScale, ReleaseDuration);
        }

        /// <summary>记录目标与匀速速度；当前已在目标上则保持静止。</summary>
        private void BeginTransition(float scale, float duration)
        {
            targetScale = scale;
            float distance = Mathf.Abs(scale - currentScale);
            approachSpeed = distance > 0f ? distance / Mathf.Max(duration, 0.0001f) : 0f;
        }

        private void Update()
        {
            if (approachSpeed <= 0f || Mathf.Approximately(currentScale, targetScale))
            {
                return;
            }
            currentScale = Mathf.MoveTowards(currentScale, targetScale, approachSpeed * Time.unscaledDeltaTime);
            transform.localScale = new Vector3(currentScale, currentScale, currentScale);
        }

        private void OnDisable()
        {
            ResetToNormal();
        }

        private void OnDestroy()
        {
            ResetToNormal();
        }

        /// <summary>直接归位（不插值）：禁用/销毁时避免残留缩小状态。</summary>
        private void ResetToNormal()
        {
            currentScale = NormalScale;
            targetScale = NormalScale;
            approachSpeed = 0f;
            transform.localScale = Vector3.one;
        }
    }
}
