using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace JinlingKnot
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonMotion : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        Button button;Vector3 rest;Color color;bool hover,down,selected;
        void Awake(){button=GetComponent<Button>();rest=transform.localScale;color=button.targetGraphic.color;button.transition=Selectable.Transition.None;}
        void Update()
        {
            bool enabled=button.IsInteractable();float target=enabled?(down?.97f:hover?1.035f:1):1;
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*18);
            transform.localScale=Vector3.Lerp(transform.localScale,rest*target,blend);
            Color tint=!enabled?new Color(.62f,.66f,.61f,.75f):down?new Color(.85f,.9f,.8f,1):hover||selected?new Color(1,.96f,.82f,1):Color.white;
            button.targetGraphic.color=Color.Lerp(button.targetGraphic.color,color*tint,blend);
        }
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=down=false;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)down=true;}
        public void OnPointerUp(PointerEventData e){down=false;}
        public void OnSelect(BaseEventData e){selected=true;}
        public void OnDeselect(BaseEventData e){selected=false;}
        void OnDisable(){hover=down=selected=false;if(button){transform.localScale=rest;button.targetGraphic.color=color;}}
    }
}
