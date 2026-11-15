using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace JinlingKnot
{
    // Vector playback symbol; independent of font glyph availability.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AutoPlayIcon : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler
    {
        public bool active;
        Text hint;
        public static AutoPlayIcon Attach(Button button, Font font)
        {
            foreach (var text in button.GetComponentsInChildren<Text>(true)) text.gameObject.SetActive(false);
            button.GetComponent<RectTransform>().sizeDelta=new Vector2(76,76);
            button.image.enabled=false;
            var obj=new GameObject("Auto play icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(AutoPlayIcon));obj.transform.SetParent(button.transform,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var icon=obj.GetComponent<AutoPlayIcon>();icon.raycastTarget=true;
            var tip=new GameObject("Auto play hint",typeof(RectTransform),typeof(Text));tip.transform.SetParent(obj.transform,false);
            icon.hint=tip.GetComponent<Text>();icon.hint.font=font;icon.hint.fontSize=20;icon.hint.alignment=TextAnchor.MiddleCenter;icon.hint.color=new Color(.12f,.23f,.18f);icon.hint.raycastTarget=false;
            var tr=tip.GetComponent<RectTransform>();tr.anchorMin=tr.anchorMax=new Vector2(1,1);tr.anchoredPosition=new Vector2(-95,22);tr.sizeDelta=new Vector2(220,38);tip.SetActive(false);
            icon.SetActive(false);return icon;
        }
        public void SetActive(bool value)
        {
            if(active!=value){active=value;SetVerticesDirty();}
            color=value?new Color(.96f,.68f,.25f):new Color(.13f,.27f,.21f);
            if(hint)hint.text=value?"自动播放：开":"自动播放：关";
        }
        public void OnPointerEnter(PointerEventData e){hint.gameObject.SetActive(true);}
        public void OnPointerExit(PointerEventData e){hint.gameObject.SetActive(false);}
        static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            int i=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);
        }
        static void Disc(VertexHelper vh,Vector2 center,float radius,Color tint)
        {
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;Triangle(vh,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,tint);}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var c=rectTransform.rect.center;float r=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.47f;
            var ink=new Color(.075f,.25f,.19f);var cream=new Color(1,.94f,.76f);var gold=new Color(.85f,.48f,.12f);
            Disc(vh,c,r,ink);Disc(vh,c,r-2.5f,cream);Disc(vh,c,r-5,new Color(.91f,.72f,.39f));Disc(vh,c,r-6,cream);
            Color symbol=active?gold:ink;
            Triangle(vh,c+new Vector2(-.16f,-.27f)*r,c+new Vector2(-.16f,.27f)*r,c+new Vector2(.31f,0)*r,symbol);
            float radius=r*.56f,halfWidth=r*.045f;
            for(int i=0;i<40;i++){
                float a=(55+i*270f/40)*Mathf.Deg2Rad,b=(55+(i+1)*270f/40)*Mathf.Deg2Rad;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                Triangle(vh,c+u*(radius-halfWidth),c+u*(radius+halfWidth),c+v*(radius+halfWidth),symbol);
                Triangle(vh,c+u*(radius-halfWidth),c+v*(radius+halfWidth),c+v*(radius-halfWidth),symbol);
            }
            var end=c+new Vector2(Mathf.Cos(325*Mathf.Deg2Rad),Mathf.Sin(325*Mathf.Deg2Rad))*radius;
            Triangle(vh,end+new Vector2(-.16f,-.02f)*r,end+new Vector2(.16f,-.08f)*r,end+new Vector2(.10f,.20f)*r,symbol);
            if(active){Disc(vh,c+new Vector2(.64f,-.64f)*r,r*.17f,cream);Disc(vh,c+new Vector2(.64f,-.64f)*r,r*.12f,gold);}
        }
    }
}
