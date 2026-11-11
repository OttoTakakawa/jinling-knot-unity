using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace JinlingKnot
{
    public sealed class LevelSelection : MonoBehaviour
    {
        Transform layout;Font font;Sprite plate,primary;Text record;
        public Button enter,demo,back;
        readonly Color ink=new Color(.12f,.22f,.17f),muted=new Color(.36f,.46f,.37f);
        Image Art(string name,Sprite sprite,float x,float y,float w,float h,bool sliced=false)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(layout,false);Position(o,x,y,w,h);
            var image=o.GetComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
            if(sliced){image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;}
            return image;
        }
        static void Position(GameObject o,float x,float y,float w,float h){var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        Text Label(string value,int size,float x,float y,float w,float h,Color color)
        {
            var o=new GameObject(value,typeof(RectTransform),typeof(Text));o.transform.SetParent(layout,false);Position(o,x,y,w,h);var text=o.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        Button Action(string title,float x,float y,float w,float h,bool hot,UnityEngine.Events.UnityAction callback)
        {
            var image=Art(title,hot?primary:plate,x,y,w,h,!hot);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(callback);
            // Child label follows its button's hover/press motion.
            var text=Label(title,30,x,y,w,h,hot?new Color(1,.96f,.81f):ink);text.alignment=TextAnchor.MiddleCenter;text.transform.SetParent(image.transform,false);var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            image.gameObject.AddComponent<ButtonMotion>();return button;
        }
        public void Initialize(GameObject menu,GameObject hud,KnotUI ui,KnotGame game)
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=25;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1672,941);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;gameObject.AddComponent<GraphicRaycaster>();
            var area=new GameObject("Level layout",typeof(RectTransform));area.transform.SetParent(transform,false);var rect=area.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1672,941);layout=area.transform;
            var art=menu.GetComponentsInChildren<Image>(true);font=menu.GetComponentInChildren<Text>(true).font;
            plate=hud.GetComponentsInChildren<Image>(true).First(i=>i.name=="Plate_Secondary").sprite;
            primary=hud.GetComponentsInChildren<Image>(true).First(i=>i.name=="Plate_Primary").sprite;
            var bg=Art("Tiles",art.First(i=>i.name=="Background_Tiles").sprite,0,0,1672,941);bg.color=new Color(.55f,.68f,.57f,1);
            Art("Collection board",plate,65,35,1542,865,true);
            Label("绳 结 小 游 戏",44,125,75,1000,70,ink).fontStyle=FontStyle.Bold;
            Label("选择一个绳形，开始织结",25,128,145,1000,45,muted);
            back=Action("返回",1350,88,180,70,false,()=>ui.Transition(()=>ui.ShowLevels(false)));
            for(int i=0;i<8;i++){
                float x=130+(i%4)*362,y=218+(i/4)*285;
                var tile=Art("Knot slot "+(i+1),plate,x,y,324,252,true);
                tile.color=i==0?Color.white:new Color(.82f,.85f,.76f,1);
                if(i==0){
                    var icon=new GameObject("Actual knot silhouette",typeof(RectTransform),typeof(KnotSilhouette));icon.transform.SetParent(tile.transform,false);
                    var ir=icon.GetComponent<RectTransform>();ir.anchorMin=ir.anchorMax=ir.pivot=new Vector2(.5f,.5f);ir.anchoredPosition=new Vector2(162,-110);ir.sizeDelta=new Vector2(242,188);
                    // The tile uses a top-left pivot; center anchoring is relative to its rectangle.
                    ir.anchoredPosition=new Vector2(0,16);
                    var shape=icon.GetComponent<KnotSilhouette>();shape.color=ink;shape.raycastTarget=false;shape.SetRoute(game.Route);
                    var title=Label("01  秦淮初织",26,x+20,y+200,284,40,ink);title.alignment=TextAnchor.MiddleCenter;
                    title.transform.SetParent(tile.transform,true);
                    var button=tile.gameObject.AddComponent<Button>();tile.raycastTarget=true;button.targetGraphic=tile;
                    button.onClick.AddListener(()=>ui.Transition(()=>{ui.ShowLevels(false);game.StartGame(false);}));tile.gameObject.AddComponent<ButtonMotion>();enter=button;
                }else{
                    var lockLabel=Label("—",54,x+20,y+65,284,70,muted);lockLabel.alignment=TextAnchor.MiddleCenter;
                    var title=Label("尚未收录",25,x+20,y+200,284,40,muted);title.alignment=TextAnchor.MiddleCenter;
                }
            }
            record=Label("",23,135,820,970,48,ink);
            demo=Action("观织演示",1190,810,325,65,false,()=>ui.Transition(()=>{ui.ShowLevels(false);game.StartGame(true);}));
            RefreshRecord();
        }
        public void RefreshRecord(){if(record)record.text=PlayerPrefs.GetInt("Qinhuai01Complete",0)==1?"已完成   ·   最佳 "+PlayerPrefs.GetInt("Qinhuai01Best",0)+" 分":"尚未完成   ·   完成后记录最佳成绩";}
    }
}
