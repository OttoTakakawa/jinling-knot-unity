using System.Linq;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using JinlingKnot.ArtUI;

namespace JinlingKnot
{
    public sealed class KnotUI : MonoBehaviour
    {
        KnotGame game;
        CanvasGroup blackout,countdown;
        Text countdownNumber;
        public bool IsCountingDown { get; private set; }
        public string CountdownValue => countdownNumber?countdownNumber.text:"";
        IEnumerator CountIn()
        {
            IsCountingDown=true;countdown.gameObject.SetActive(true);
            for(int n=3;n>=1;n--){
                countdownNumber.text=n.ToString();float t=0;
                while(t<1){t+=Time.unscaledDeltaTime;countdownNumber.transform.localScale=Vector3.one*Mathf.Lerp(1.18f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.3f)));yield return null;}
            }
            countdownNumber.text="起";yield return new WaitForSecondsRealtime(.25f);
            countdown.gameObject.SetActive(false);IsCountingDown=false;
        }
        void CreateCountdown(Font font)
        {
            var root=new GameObject("Start countdown",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=32759;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1672,941);
            countdown=root.GetComponent<CanvasGroup>();
            var shade=new GameObject("Shade",typeof(RectTransform),typeof(Image));shade.transform.SetParent(root.transform,false);var sr=shade.GetComponent<RectTransform>();sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=sr.offsetMax=Vector2.zero;shade.GetComponent<Image>().color=new Color(.015f,.06f,.04f,.45f);
            var number=new GameObject("Count",typeof(RectTransform),typeof(Text));number.transform.SetParent(root.transform,false);var nr=number.GetComponent<RectTransform>();nr.sizeDelta=new Vector2(400,300);countdownNumber=number.GetComponent<Text>();countdownNumber.font=font;countdownNumber.fontSize=164;countdownNumber.fontStyle=FontStyle.Bold;countdownNumber.alignment=TextAnchor.MiddleCenter;countdownNumber.color=new Color(1,.91f,.68f);
            var hint=new GameObject("Ready hint",typeof(RectTransform),typeof(Text));hint.transform.SetParent(root.transform,false);var hr=hint.GetComponent<RectTransform>();hr.sizeDelta=new Vector2(700,70);hr.anchoredPosition=new Vector2(0,-165);var ht=hint.GetComponent<Text>();ht.font=font;ht.fontSize=30;ht.text="准备织结 · 跟随节拍";ht.alignment=TextAnchor.MiddleCenter;ht.color=new Color(1,.95f,.81f);
            root.SetActive(false);
        }
        LevelSelection levels;bool levelOpen,recorded;
        public void ShowLevels(bool show){levelOpen=show;if(levels)levels.RefreshRecord();}
        public void Escape(){if(IsTransitioning)return;Transition(()=>{if(levelOpen)ShowLevels(false);else if(menuActions.settingsPanel.activeSelf)menuActions.settingsPanel.SetActive(false);else game.Pause();},false);}
        public bool IsTransitioning { get; private set; }
        public void Transition(System.Action action,bool useBlack=true)
        {
            if(IsTransitioning)return;
            StartCoroutine(BlackTransition(action,useBlack));
        }
        IEnumerator BlackTransition(System.Action action,bool useBlack)
        {
            IsTransitioning=true;game.SetTransitionLock(true);blackout.blocksRaycasts=true;
            if(useBlack){
                yield return FadeBlack(0,1,.4f);action();Synchronize();
                yield return new WaitForSecondsRealtime(.06f);yield return FadeBlack(1,0,.5f);
                if(game.State==RunState.Playing&&game.RunTime<.001f)yield return CountIn();
            }else{
                action();Synchronize();
                GameObject visible=paused.activeSelf?paused:levels.gameObject.activeSelf?levels.gameObject:menu.activeSelf?(menuActions.settingsPanel.activeSelf?menuActions.settingsPanel:menu):hud;
                var group=visible.GetComponent<CanvasGroup>();if(!group)group=visible.AddComponent<CanvasGroup>();
                float t=0;group.alpha=0;while(t<.18f){t+=Time.unscaledDeltaTime;group.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.18f));yield return null;}group.alpha=1;
            }
            blackout.blocksRaycasts=false;game.SetTransitionLock(false);IsTransitioning=false;
        }
        IEnumerator FadeBlack(float from,float to,float duration)
        {
            float elapsed=0;
            while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;blackout.alpha=Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration)));yield return null;}
            blackout.alpha=to;
        }
        void CreateBlackout()
        {
            var root=new GameObject("Black transition",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            blackout=root.GetComponent<CanvasGroup>();blackout.alpha=0;blackout.blocksRaycasts=false;
            var panel=new GameObject("Black",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color=Color.black;
        }
        GameObject menu,hud,paused,completed,retry;
        Text score,counters,runStatus,cue,feedback,controls;
        RectTransform progressFill;
        GameObject cueRoot;
        MenuInteractions menuActions;
        static Text Label(GameObject root,string name) => root.GetComponentsInChildren<Text>(true).First(t=>t.name==name);
        GameObject Spawn(GameObject prefab)
        {
            var instance=Instantiate(prefab);instance.name=prefab.name;
            var canvas=instance.GetComponent<Canvas>();canvas.sortingOrder=20;
            return instance;
        }
        static void Bind(Button button,UnityEngine.Events.UnityAction action)
        {button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
        void BindModal(GameObject root,UnityEngine.Events.UnityAction primary,bool useBlack=true)
        {
            var buttons=root.GetComponentsInChildren<Button>(true);
            Bind(buttons[0],()=>Transition(()=>primary(),useBlack));Bind(buttons[1],()=>Transition(()=>{ShowLevels(false);game.ResetGame();}));
        }
        public void Initialize(KnotGame value)
        {
            game=value;CreateBlackout();
            if(!EventSystem.current)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            menu=Spawn(game.menuPrefab);hud=Spawn(game.hudPrefab);paused=Spawn(game.pausePrefab);completed=Spawn(game.completePrefab);retry=Spawn(game.retryPrefab);
            menuActions=menu.GetComponent<MenuInteractions>();
            menuActions.onStart.AddListener(()=>Transition(()=>ShowLevels(true),false));
            menuActions.onDemo.AddListener(()=>Transition(()=>game.StartGame(true)));
            menuActions.onExit.AddListener(()=>Transition(()=>Application.Quit()));
            BindModal(paused,()=>game.Pause(),false);BindModal(completed,()=>game.StartGame(false));BindModal(retry,()=>game.StartGame(false));
            Bind(hud.GetComponentInChildren<Button>(true),()=>Transition(()=>game.Pause(),false));
            Bind(menuActions.settings,()=>Transition(()=>menuActions.settingsPanel.SetActive(true),false));
            Bind(menuActions.closeSettings,()=>Transition(()=>menuActions.settingsPanel.SetActive(false),false));
            score=Label(hud,"Score");counters=Label(hud,"Counters");runStatus=Label(hud,"Run status");cue=Label(hud,"Crossing cue");controls=Label(hud,"Controls");
            cueRoot=cue.transform.parent.Find("Plate_Primary").gameObject;
            progressFill=hud.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="Progress fill");
            feedback=Instantiate(controls,controls.transform.parent);feedback.name="Live feedback";feedback.fontSize=29;feedback.color=new Color(1,.94f,.74f);feedback.rectTransform.anchoredPosition=new Vector2(342,-693);feedback.rectTransform.sizeDelta=new Vector2(988,52);
            CreateCountdown(controls.font);
            levels=new GameObject("Level selection",typeof(RectTransform)).AddComponent<LevelSelection>();levels.Initialize(menu,hud,this,game);
            foreach(var root in new[]{menu,hud,paused,completed,retry})foreach(var button in root.GetComponentsInChildren<Button>(true))if(!button.GetComponent<ButtonMotion>())button.gameObject.AddComponent<ButtonMotion>();
            // Give game view more room than the static review board.
            Synchronize();
        }
        void Update(){if(game!=null)Synchronize();}
        void Synchronize()
        {
            bool ready=game.State==RunState.Ready;
            menu.SetActive(ready&&!levelOpen);levels.gameObject.SetActive(ready&&levelOpen);hud.SetActive(game.State==RunState.Playing||game.State==RunState.Revealing||game.State==RunState.Complete&&game.Presentation<11);
            paused.SetActive(game.State==RunState.Paused);retry.SetActive(game.State==RunState.Failed);completed.SetActive(game.State==RunState.Complete&&game.Presentation>=11);
            if(!ready)menuActions.settingsPanel.SetActive(false);
            if(game.State==RunState.Playing)recorded=false;
            if(game.State==RunState.Complete&&!game.Demo&&!recorded){recorded=true;PlayerPrefs.SetInt("Qinhuai01Complete",1);PlayerPrefs.SetInt("Qinhuai01Best",Mathf.Max(game.Score,PlayerPrefs.GetInt("Qinhuai01Best",0)));PlayerPrefs.Save();}
            score.text=game.Score.ToString("00000")+" 分";
            runStatus.text=game.IsPanorama?"一线成结 · 缓缓收拢":game.Demo?"循音观织 · 自动演示":"织结中 · 循音续弯";
            counters.text=$"续弯 {game.Bends:00} / {game.Route.data.turns.Length:00}     穿织 {game.CrossingsPassed:00} / {game.Route.data.crossings.Length:00}     轻点 {game.OptionalHits:00}";
            controls.text=game.IsPanorama?"秦淮入画 · 一线盘长":game.Demo?"正在自动演示    ·    Esc 暂停    ·    R 手动重开":"左键 / Enter 续弯    ·    ↑ ↓ 手动穿线    ·    右键自动穿织";
            progressFill.sizeDelta=new Vector2(864*Mathf.Clamp01(game.Progress/game.Route.Total),10);
            int i=game.NextCrossing;bool approaching=game.State==RunState.Playing&&i>=0&&game.Route.weaveTimes[i]-game.RunTime<2;
            cueRoot.SetActive(approaching);cue.gameObject.SetActive(approaching);
            if(approaching)cue.text=(game.Route.data.crossings[i].side>0?"↑ 上穿":"↓ 下穿")+(Mathf.Abs(game.Route.weaveTimes[i]-game.RunTime)<=.3f?" · 合拍":" · 前方");
            feedback.text=game.State==RunState.Playing?game.Feedback:"";var c=feedback.color;c.a=game.FeedbackOpacity;feedback.color=c;
            if(retry.activeSelf){Label(retry,"Description").text=game.Status;SetResults(retry);}
            if(completed.activeSelf)SetResults(completed);
        }
        void SetResults(GameObject root)
        {
            Label(root,"Score").text=game.Score+" 分";
            Label(root,"Summary").text=$"续弯 {game.Bends} / {game.Route.data.turns.Length}   ·   穿织 {game.CrossingsPassed} / {game.Route.data.crossings.Length}";
        }
        public IEnumerator VerifyFlow(string directory)
        {
            game.ResetGame();Synchronize();yield return new WaitForSeconds(2);yield return Snap(directory,"MainMenu.png");
            var pointer=new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(menuActions.start.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.2f);bool hoverOK=menuActions.start.transform.localScale.x>1.02f;
            yield return Snap(directory,"ButtonHover.png");
            ExecuteEvents.Execute(menuActions.start.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.2f);
            hoverOK=hoverOK&&menuActions.start.transform.localScale.x<.99f;
            ExecuteEvents.Execute(menuActions.start.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(menuActions.start.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            menuActions.settings.onClick.Invoke();yield return new WaitUntil(()=>!IsTransitioning);yield return Snap(directory,"Settings.png");
            bool settingsOK=menuActions.settingsPanel.activeSelf;
            menuActions.closeSettings.onClick.Invoke();yield return new WaitUntil(()=>!IsTransitioning);
            menuActions.onStart.Invoke();menuActions.onDemo.Invoke();
            yield return new WaitForSecondsRealtime(.06f);
            bool transitionOK=IsTransitioning&&blackout.alpha==0&&game.State==RunState.Ready;
            yield return Snap(directory,"Transition.png");
            yield return new WaitUntil(()=>!IsTransitioning);
            transitionOK=transitionOK&&blackout.alpha==0&&!blackout.blocksRaycasts&&game.RunTime<.1f;
            File.WriteAllText(Path.Combine(directory,"transition-checks.json"),"{\"passed\":"+transitionOK.ToString().ToLower()+"}");
            bool selectOK=levelOpen&&levels.gameObject.activeSelf&&game.State==RunState.Ready;
            yield return Snap(directory,"LevelSelection.png");
            levels.back.onClick.Invoke();yield return new WaitUntil(()=>!IsTransitioning);selectOK=selectOK&&!levelOpen&&menu.activeSelf;
            menuActions.onStart.Invoke();yield return new WaitUntil(()=>!IsTransitioning);
            levels.enter.onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
            transitionOK=transitionOK&&blackout.alpha>0&&blackout.alpha<1;
            yield return Snap(directory,"BlackTransition.png");yield return new WaitUntil(()=>IsCountingDown);
            bool countdownOK=true;
            for(int n=3;n>=1;n--){yield return new WaitUntil(()=>CountdownValue==n.ToString());countdownOK=countdownOK&&game.RunTime==0&&game.Progress==0;yield return Snap(directory,"Countdown"+n+".png");}
            yield return new WaitUntil(()=>!IsTransitioning);
            File.WriteAllText(Path.Combine(directory,"countdown-checks.json"),"{\"sequence321\":"+countdownOK.ToString().ToLower()+",\"timerFrozen\":"+(game.RunTime<.1f).ToString().ToLower()+"}");
            File.WriteAllText(Path.Combine(directory,"selection-hover-checks.json"),"{\"selection\":"+selectOK.ToString().ToLower()+",\"hoverAndPress\":"+hoverOK.ToString().ToLower()+"}");
            bool startOK=countdownOK&&selectOK&&hoverOK&&game.State==RunState.Playing&&!game.Demo;
            hud.GetComponentInChildren<Button>(true).onClick.Invoke();yield return new WaitUntil(()=>!IsTransitioning);yield return Snap(directory,"Paused.png");
            bool pauseOK=game.State==RunState.Paused&&paused.activeSelf;
            float frozenTime=game.RunTime;
            paused.GetComponentsInChildren<Button>(true)[0].onClick.Invoke();yield return new WaitUntil(()=>!IsTransitioning);bool resumeOK=game.State==RunState.Playing&&game.RunTime-frozenTime<.1f;
            game.StepTo(game.Route.bendTimes[0]+.31f);yield return Snap(directory,"Retry.png");
            bool failOK=game.State==RunState.Failed&&retry.activeSelf;
            retry.GetComponentsInChildren<Button>(true)[0].onClick.Invoke();yield return new WaitUntil(()=>!IsTransitioning);bool retryOK=game.State==RunState.Playing;
            game.Pause();paused.GetComponentsInChildren<Button>(true)[1].onClick.Invoke();yield return new WaitUntil(()=>!IsTransitioning);bool menuOK=game.State==RunState.Ready;
            menuActions.onDemo.Invoke();yield return new WaitUntil(()=>!IsTransitioning);bool demoOK=game.State==RunState.Playing&&game.Demo;
            bool ok=transitionOK&&settingsOK&&startOK&&pauseOK&&resumeOK&&failOK&&retryOK&&menuOK&&demoOK;
            File.WriteAllText(Path.Combine(directory,"ui-checks.json"),"{\"passed\":"+ok.ToString().ToLower()+",\"settings\":"+settingsOK.ToString().ToLower()+",\"start\":"+startOK.ToString().ToLower()+",\"pause\":"+pauseOK.ToString().ToLower()+",\"resume\":"+resumeOK.ToString().ToLower()+",\"failure\":"+failOK.ToString().ToLower()+",\"retry\":"+retryOK.ToString().ToLower()+",\"menu\":"+menuOK.ToString().ToLower()+",\"demo\":"+demoOK.ToString().ToLower()+"}");
            if(!ok){Debug.LogError("UI FLOW CHECK FAILED");Application.Quit(1);}
        }
        IEnumerator Snap(string directory,string filename)
        {
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,filename));yield return new WaitForSeconds(.15f);
        }
    }
}
