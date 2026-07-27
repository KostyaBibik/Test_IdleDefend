using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Views.Panels
{
    /// <summary>
    /// Полноэкранный невидимый перехватчик клика "мимо" попапа. Закрывает попап (событие Closed)
    /// и тем же кликом перерейкастит сквозь себя — если под попапом оказалась кликабельная кнопка
    /// (например, апгрейд башни), клик долетает и до неё, без необходимости кликать дважды.
    /// </summary>
    public class ClickThroughOverlay : MonoBehaviour, IPointerClickHandler
    {
        public event Action Closed;

        public void OnPointerClick(PointerEventData eventData)
        {
            Closed?.Invoke();

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(result.gameObject);
                if (handler != null)
                {
                    ExecuteEvents.Execute(handler, eventData, ExecuteEvents.pointerClickHandler);
                    break;
                }
            }
        }
    }
}
