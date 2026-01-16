using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public interface ILongPressInteractor: IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 是否开始长按
        /// </summary>
        public bool isLongPressing { get; }
        /// <summary>
        /// 是否在完成长按确认
        /// </summary>
        public bool isConfirmed { get; }
        /// <summary>
        /// 长按进度值
        /// </summary>
        public float processValue { get; }
        /// <summary>
        /// 长按确认后经过的时间
        /// </summary>
        public float pressDuration { get; }

        /// <summary>
        /// 长按开始或结束
        /// </summary>
        public UnityEvent<bool> onActive { get; }
    }
}