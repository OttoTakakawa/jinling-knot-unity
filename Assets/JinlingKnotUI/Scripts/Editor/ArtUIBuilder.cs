using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace JinlingKnot.ArtUI
{
    public static class ArtUIBuilder
    {
        const string Root="Assets/JinlingKnotUI";
        [Serializable] class Entry { public string name,file; public int width,height; public int[] bounds; }
        [Serializable] class Entries { public Entry[] items; }
        static Transform parent;
        static Font font;
        static RectTransform Position(GameObject o,float x,float y,float w,float h)
        {
            var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        static Image Art(string name,float x,float y,float w,float h)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);
            var im=o.GetComponent<Image>();im.sprite=AssetDatabase.LoadAllAssetsAtPath(Root+"/Components/"+name+".png").OfType<Sprite>().First();im.raycastTarget=false;Position(o,x,y,w,h);return im;
        }
        static Button Button(string name,float x,float y,float w,float h)
        {
            var im=Art(name,x,y,w,h);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;
            var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,.96f,.84f);c.pressedColor=new Color(.79f,.84f,.72f);c.disabledColor=new Color(.6f,.6f,.6f,.5f);c.fadeDuration=.12f;b.colors=c;return b;
        }
        static Text Text(string name,string value,int size,float x,float y,float w,float h)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);Position(o,x,y,w,h);
            var t=o.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.12f,.16f,.12f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        [MenuItem("金陵绳结/UI/生成拆件主菜单")]
        public static void Build()
        {
            var entries=JsonUtility.FromJson<Entries>("{\"items\":"+File.ReadAllText(Root+"/manifest.json")+"}");
            foreach(var a in entries.items)
            {
                string path=Root+"/"+a.file;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
                importer.spritesheet=new[]{new SpriteMetaData{name=a.name,alignment=9,pivot=new Vector2(.5f,.5f),border=a.name=="Plate_Secondary"?new Vector4(300,180,300,180):Vector4.zero,rect=new Rect(a.bounds[0],a.height-a.bounds[3],a.bounds[2]-a.bounds[0],a.bounds[3]-a.bounds[1])}};
                importer.SaveAndReimport();
            }
            font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKsc-Regular.otf");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("UI Camera",typeof(Camera)).GetComponent<Camera>();cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.1f,.23f,.16f);cam.transform.position=new Vector3(0,0,-100);
            var canvas=new GameObject("Jinling Main Menu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=10;
            var s=canvas.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1672,941);s.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var area=new GameObject("16x9 Layout",typeof(RectTransform),typeof(AspectRatioFitter));area.transform.SetParent(canvas.transform,false);
            UnityEngine.Object.DestroyImmediate(area.GetComponent<AspectRatioFitter>());
            var areaRect=area.GetComponent<RectTransform>();areaRect.anchorMin=areaRect.anchorMax=new Vector2(.5f,.5f);areaRect.pivot=new Vector2(.5f,.5f);areaRect.sizeDelta=new Vector2(1672,941);
            parent=area.transform;
            Art("Background_Tiles",0,0,1672,941);
            Art("Panel_Menu",16,22,802,833);
            Art("Stage_ArchFan",800,12,859,880);
            Art("Hero_Knot",928,-12,595,883);
            Art("Title_Wordmark",53,66,719,234);
            Text("Roman title","—   J I N L I N G   K N O T   —",23,115,280,600,43);
            Text("Subtitle","一线织秦淮 · 九穿结金陵",34,95,319,645,55);
            var ctl=canvas.AddComponent<MenuInteractions>();
            ctl.start=Button("Button_Start",88,382,681,177);
            ctl.demo=Button("Button_Demo",136,571,587,128);
            ctl.settings=Button("Button_Settings",145,714,568,115);
            ctl.sound=Button("Button_Sound",1461,18,87,87);
            ctl.fullscreen=Button("Button_Fullscreen",1566,18,87,87);
            ctl.exit=Button("Button_Exit",37,852,198,74);
            var footer=Art("Plate_Secondary",307,879,1117,53);footer.type=Image.Type.Sliced;footer.pixelsPerUnitMultiplier=10;
            Text("Footer","循 音 入 画， 一 线 成 结",29,456,879,800,51);
            var modal=new GameObject("Settings Dialog",typeof(RectTransform),typeof(Image));modal.transform.SetParent(parent,false);Position(modal,0,0,1672,941);modal.GetComponent<Image>().color=new Color(.02f,.12f,.07f,.8f);ctl.settingsPanel=modal;parent=modal.transform;
            Art("Panel_Menu",480,150,712,650);Text("Heading","设置",48,610,235,450,90);Text("Volume label","声音音量",28,610,377,450,50);
            var track=new GameObject("Volume",typeof(RectTransform),typeof(Image),typeof(Slider));track.transform.SetParent(parent,false);Position(track,620,462,432,22);track.GetComponent<Image>().color=new Color(.2f,.4f,.28f);
            var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image));handle.transform.SetParent(track.transform,false);handle.GetComponent<Image>().color=new Color(1,.4f,.08f);handle.GetComponent<RectTransform>().sizeDelta=new Vector2(30,42);
            ctl.volume=track.GetComponent<Slider>();ctl.volume.handleRect=handle.GetComponent<RectTransform>();ctl.volume.targetGraphic=handle.GetComponent<Image>();ctl.volume.value=1;
            ctl.closeSettings=Button("Plate_Secondary",640,620,392,86);Text("Close label","返回",32,640,620,392,86);modal.SetActive(false);
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Scenes");
            // The portable prefab uses overlay mode; the scene camera is only for rendering.
            c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;
            PrefabUtility.SaveAsPrefabAsset(canvas,Root+"/Prefabs/JinlingMainMenu.prefab");
            c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/UIReview.unity");AssetDatabase.SaveAssets();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../JinlingKnotUI"));
            Directory.CreateDirectory(output+"/Preview");
            Render(cam,output+"/Preview/MainMenu.png");
            modal.SetActive(true);Render(cam,output+"/Preview/Settings.png");modal.SetActive(false);
            canvas.SetActive(false);
            BuildScreen(cam,output,"HUD","织结中 · 循音续弯",0);
            BuildScreen(cam,output,"Paused","稍作歇息",1);
            BuildScreen(cam,output,"Completed","一线成结",2);
            BuildScreen(cam,output,"Retry","再试一次",3);
            canvas.SetActive(true);
            AssetDatabase.ExportPackage(Root,output+"/JinlingKnotUI.unitypackage",ExportPackageOptions.Recurse);
            Debug.Log("ART_UI_BUILD_OK: 13 PNG assets, prefab, scene, package, preview");
        }
        static void BuildScreen(Camera cam,string output,string name,string heading,int kind)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=10;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1672,941);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var layout=new GameObject("Layout",typeof(RectTransform));layout.transform.SetParent(root.transform,false);var rect=layout.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1672,941);parent=layout.transform;
            var previewBackground=Art("Background_Tiles",0,0,1672,941);previewBackground.gameObject.name="Preview backdrop — remove over gameplay";
            if(kind==0)
            {
                var titleplate=Art("Plate_Secondary",28,26,560,96);titleplate.type=Image.Type.Sliced;titleplate.pixelsPerUnitMultiplier=6;
                Text("Run status",heading,29,55,39,500,65);
                var scoreplate=Art("Plate_Secondary",1000,26,515,96);scoreplate.type=Image.Type.Sliced;scoreplate.pixelsPerUnitMultiplier=6;
                Text("Score","02560 分   ·   连击 08",30,1025,39,465,65);
                var pause=Button("Plate_Secondary",1530,26,114,96);Text("Pause label","Ⅱ",38,1530,26,114,96);
                // The game supplies the rope view below this transparent HUD.
                var cue=Art("Plate_Primary",620,191,430,111);Text("Crossing cue","↑ 上穿 · 合拍",33,640,214,390,68).color=new Color(1,.95f,.8f);
                var bottom=Art("Plate_Secondary",262,782,1148,126);bottom.type=Image.Type.Sliced;bottom.pixelsPerUnitMultiplier=5;
                Text("Counters","续弯  07 / 13     穿织  06 / 13     轻点  16",30,342,794,988,54);
                Text("Controls","左键 / Enter 续弯     ·     ↑ ↓ 手动穿线     ·     右键自动穿织",22,342,847,988,39);
                var bar=new GameObject("Progress track",typeof(RectTransform),typeof(Image));bar.transform.SetParent(parent,false);Position(bar,404,756,864,10);bar.GetComponent<Image>().color=new Color(.1f,.28f,.18f);
                var fill=new GameObject("Progress fill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(bar.transform,false);Position(fill,0,0,472,10);fill.GetComponent<Image>().color=new Color(1,.37f,.06f);
            }
            else
            {
                var shade=new GameObject("Dimmer",typeof(RectTransform),typeof(Image));shade.transform.SetParent(parent,false);Position(shade,0,0,1672,941);shade.GetComponent<Image>().color=new Color(.01f,.10f,.06f,.62f);
                Art("Panel_Menu",462,44,748,851);
                Art("Title_Wordmark",580,95,512,167);
                Text("State heading",heading,48,562,290,548,85);
                if(kind==1)
                {
                    Text("Description","让秦淮稍候片刻",28,582,391,508,50);
                    var resume=Button("Plate_Primary",573,500,526,137);Text("Resume label","继续织结",36,603,531,466,68).color=new Color(1,.95f,.8f);
                    Button("Plate_Secondary",591,664,490,106);Text("Menu label","返回主菜单",30,591,664,490,106);
                }
                else
                {
                    Text("Description",kind==2?"循音入画，秦淮留结":"松开一线，再循音起结",28,542,382,588,54);
                    Text("Score",kind==2?"5000 分":"2560 分",55,562,456,548,82);
                    Text("Summary",kind==2?"续弯 13 / 13   ·   穿织 13 / 13":"续弯 07 / 13   ·   穿织 06 / 13",26,522,546,628,52);
                    Button("Plate_Primary",573,628,526,137);Text("Retry label","再织一次",36,603,659,466,68).color=new Color(1,.95f,.8f);
                    Button("Plate_Secondary",638,785,396,72);Text("Menu label","返回主菜单",26,638,785,396,72);
                }
            }
            Render(cam,output+"/Preview/"+name+".png");
            previewBackground.gameObject.SetActive(false);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }
        static void Render(Camera cam,string path)
        {
            var rt=new RenderTexture(1672,941,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1672,941,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1672,941),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
