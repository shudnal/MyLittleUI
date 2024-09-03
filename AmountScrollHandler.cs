using UnityEngine.EventSystems;
using UnityEngine;

namespace MyLittleUI
{
    public class AmountScrollHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static bool hovered;
        
        void Start()
        {
            hovered = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }
    }
}

