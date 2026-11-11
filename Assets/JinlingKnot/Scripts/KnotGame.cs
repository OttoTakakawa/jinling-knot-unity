using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JinlingKnot
{
    public enum RunState { Ready, Playing, Paused, Failed, Revealing, Complete }
    public sealed class KnotGame : MonoBehaviour
    {
        public CityRig city;
        public GameObject menuPrefab,hudPrefab,pausePrefab,completePrefab,retryPrefab;
        public KnotRoute Route {get;private set;}
        public RunState State {get;private set;}
        public float Progress {get;private set;}
        public float RunTime {get;private set;}
        public int Bends {get;private set;}
        public int CrossingsPassed {get;private set;}
        public int OptionalHits {get;private set;}
        public int Score {get;private set;}
        public string Status {get;private set;} = "一线九穿 · 秦淮入画";
        public bool Demo {get;private set;}
        public bool Sounds {get;private set;}=true;
        public float Presentation {get;private set;}
        public string Feedback {get;private set;}="";
        public float FeedbackOpacity => Mathf.Clamp01(1-hitAge/.55f);
        public int NextCrossing { get { for(int i=0;i<passed.Length;i++)if(!passed[i]&&!Route.data.crossings[i].automatic)return i; return -1; } }
        bool[] passed, optional;
        bool bendArmed;
        int weaveArmed=-1;
        double dspOrigin;
        AudioSource music, effects;
        Camera cam;
        Transform player, tail, hitRing, bendRing;
        Transform[] optionalRings;
        GameObject[] guides;
        float[] guideDs;
        Material ropeMaterial;
        RopeMesh rope;
        ThreadHeadVFX headVfx;
        Vector3 heading, focus, revealOrigin, revealFocus;
        float hitAge=1,hitSide,headAge,pausedTime;
        string captureDirectory;
        bool capture, capturedMid,capturedEnd,checkingUI,capturedVfx,capturedWeave;
        Dictionary<string,AudioClip> tones=new Dictionary<string,AudioClip>();
        readonly Color jade=new Color(.58f,.89f,.87f), gold=new Color(.94f,.68f,.31f);
        public bool IsPanorama => State==RunState.Revealing||State==RunState.Complete;
        void Awake()
        {
            Route=new KnotRoute();
            passed=new bool[Route.data.crossings.Length]; optional=new bool[Route.data.optionalEvents.Length];
            BuildWorld();
            gameObject.AddComponent<KnotUI>().Initialize(this);
            foreach(var e in Route.data.events) MakeTone(e.time,e.action=="turn"?0:Route.data.crossings[e.index].side);
            foreach(var e in Route.data.optionalEvents) MakeTone(e.time,0);
            ResetGame();
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length;i++)if(args[i]=="--knot-capture"&&i+1<args.Length){capture=true;captureDirectory=args[i+1];Directory.CreateDirectory(captureDirectory);}
            if(capture)
            {
                try { MigrationChecks.Run(this,captureDirectory); }
                catch(Exception ex){Debug.LogException(ex);File.WriteAllText(Path.Combine(captureDirectory,"check-failure.txt"),ex.ToString());Application.Quit(1);return;}
                checkingUI=true;StartCoroutine(CaptureUIThenPlay());
            }
        }
        Material Flat(Color c, float glow=0)
        {
            var m=new Material(Shader.Find("Jinling/City"));m.SetColor("_Color",c);m.SetFloat("_Glow",glow);return m;
        }
        GameObject Ball(string name,Vector3 scale,Material mat,Transform parent=null)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=name;o.transform.SetParent(parent,false);o.transform.localScale=scale;
            Destroy(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=mat;return o;
        }
        Transform Ring(string name,float radius,Material material)
        {
            var o=new GameObject(name);o.layer=30;var lr=o.AddComponent<LineRenderer>();lr.sharedMaterial=material;lr.useWorldSpace=false;lr.loop=true;lr.positionCount=48;lr.widthMultiplier=.045f;lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;lr.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
            o.transform.SetParent(transform,false);return o.transform;
        }
        void BuildWorld()
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.85f,.88f,.72f);RenderSettings.ambientIntensity=1;
            RenderSettings.fog=false;RenderSettings.skybox=null;
            var sun=new GameObject("Lantern sunset").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.98f,.77f,.53f);sun.intensity=.65f;sun.transform.rotation=Quaternion.Euler(60,25,0);sun.shadows=LightShadows.None;
            cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.36f,.27f);cam.fieldOfView=48;cam.nearClipPlane=.1f;cam.farClipPlane=250;cam.gameObject.AddComponent<AudioListener>();cam.depthTextureMode=DepthTextureMode.Depth;
            foreach(var renderer in city.GetComponentsInChildren<Renderer>(true))if(renderer.sharedMaterial&&renderer.sharedMaterial.shader.name=="Jinling/Water")renderer.gameObject.AddComponent<RiverReflection>();
            player=Ball("Golden thread head",Vector3.one*.46f,Flat(gold,.45f)).transform;player.SetParent(transform,false);
            var bead=Ball("Direction bead",Vector3.one*.11f,Flat(new Color(1,.94f,.77f)),player);bead.transform.localPosition=Vector3.forward*.5f;bead.transform.localScale=Vector3.one*(.11f/.46f);
            var band=Ring("Head equatorial band",.52f,Flat(new Color(.55f,.19f,.16f)));band.SetParent(player,false);band.localRotation=Quaternion.Euler(90,0,0);band.GetComponent<LineRenderer>().widthMultiplier=.035f;
            tail=Ball("Stationary tail cap",Vector3.one*.33f,Flat(gold)).transform;tail.SetParent(transform,false);
            ropeMaterial=new Material(Shader.Find("Jinling/Silk"));
            rope=new GameObject("Permanent rope").AddComponent<RopeMesh>();rope.transform.SetParent(transform,false);rope.Initialize(Route,ropeMaterial);
            hitRing=Ring("Touch pulse",.4f,Flat(gold,.7f));hitRing.gameObject.SetActive(false);
            bendRing=Ring("Next bend",.48f,Flat(gold,.4f));
            optionalRings=new Transform[optional.Length];
            var optionalMat=Flat(jade,.5f);
            for(int i=0;i<optional.Length;i++){optionalRings[i]=Ring("Optional touch "+i,.3f,optionalMat);optionalRings[i].position=Route.Position(Route.data.optionalEvents[i].distance)+Vector3.up*.21f;}
            int count=Mathf.CeilToInt(Route.data.wrapStart/.72f);guides=new GameObject[count];guideDs=new float[count];var guideMat=Flat(new Color(.55f,.70f,.66f),.3f);
            for(int i=0;i<count;i++)
            {
                float d=i*.72f;guideDs[i]=d;
                var a=Route.Position(d)+Vector3.up*.15f;var b=Route.Position(Mathf.Min(d+.36f,Route.data.wrapStart))+Vector3.up*.15f;
                var o=new GameObject("Dashed guide "+i);o.layer=30;o.transform.SetParent(transform,false);var lr=o.AddComponent<LineRenderer>();lr.sharedMaterial=guideMat;lr.positionCount=2;lr.SetPosition(0,a);lr.SetPosition(1,b);lr.widthMultiplier=.045f;lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;guides[i]=o;
            }
            city.gameObject.AddComponent<RiverContactEffects>();
            headVfx=new GameObject("Thread head effects").AddComponent<ThreadHeadVFX>();headVfx.Initialize(this,player,cam);
            music=gameObject.AddComponent<AudioSource>();music.clip=Resources.Load<AudioClip>("AuthoredWeave");music.volume=.7f;music.playOnAwake=false;
            effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.volume=.22f;
        }
        public void ResetGame()
        {
            if(headVfx)headVfx.Clear();
            music.Stop();effects.Stop();Array.Clear(passed,0,passed.Length);Array.Clear(optional,0,optional.Length);
            Progress=RunTime=Presentation=0;Bends=CrossingsPassed=OptionalHits=Score=0;weaveArmed=-1;bendArmed=false;headAge=0;hitAge=1;
            State=RunState.Ready;Status="一线九穿 · 秦淮入画";Feedback="";
            city.Darkness(0);city.Reveal(0);rope.UpdateRope(0,-1);ropeMaterial.SetFloat("_Clock",0);
            player.position=Route.Position(0)+Vector3.up*.14f;player.rotation=Quaternion.LookRotation(Route.Tangent(0));player.localScale=Vector3.one*.001f;
            tail.position=player.position;tail.gameObject.SetActive(false);heading=Route.Tangent(0);focus=player.position+heading*3.3f;
            cam.fieldOfView=48;cam.backgroundColor=new Color(.16f,.36f,.27f);Follow(0,true);UpdateMarkers();
        }
        bool transitionLocked;
        public void SetTransitionLock(bool locked)
        {
            transitionLocked=locked;
            if(locked){if(State==RunState.Playing)music.Pause();}
            else if(State==RunState.Playing){dspOrigin=AudioSettings.dspTime-RunTime;music.time=Mathf.Min(RunTime,music.clip.length-.01f);music.Play();}
        }
        public void StartGame(bool demo)
        {
            ResetGame();Demo=demo;State=RunState.Playing;Status=demo?"循音观织 · 自动演示":"跟随拨弦 · 到拍续弯";
            dspOrigin=AudioSettings.dspTime+.15;music.time=0;if(!transitionLocked)music.PlayScheduled(dspOrigin);
        }
        public void ToggleSound(){Sounds=!Sounds;music.mute=effects.mute=!Sounds;}
        public void Pause()
        {
            if(State==RunState.Playing){pausedTime=RunTime;State=RunState.Paused;music.Pause();}
            else if(State==RunState.Paused){dspOrigin=AudioSettings.dspTime-pausedTime;if(!transitionLocked)music.UnPause();State=RunState.Playing;}
        }
        public void Tap(float at)
        {
            if(State!=RunState.Playing)return;
            if(Bends<Route.bendTimes.Length&&Mathf.Abs(at-Route.bendTimes[Bends])<=.3f)
            {
                if(!bendArmed){bendArmed=true;Hit(Route.data.turns[Bends],Mathf.Abs(at-Route.bendTimes[Bends]),0,false,Route.bendTimes[Bends]);}return;
            }
            int best=-1;float err=.30001f;
            for(int i=0;i<optional.Length;i++){float e=Mathf.Abs(at-Route.data.optionalEvents[i].time);if(!optional[i]&&e<err){err=e;best=i;}}
            if(best>=0){optional[best]=true;OptionalHits++;var e=Route.data.optionalEvents[best];Hit(e.distance,err,0,true,e.time);}
        }
        public void Weave(float side,float at)
        {
            if(State!=RunState.Playing)return;
            int i=NextCrossing;if(i<0||Mathf.Abs(at-Route.weaveTimes[i])>.3f)return;
            if(side!=Route.data.crossings[i].side){Fail(side>0?"此处需要下穿 ↓":"此处需要上穿 ↑");return;}
            if(weaveArmed==i)return;weaveArmed=i;Hit(Route.data.crossings[i].distance,Mathf.Abs(at-Route.weaveTimes[i]),side,false,Route.weaveTimes[i]);
        }
        public void StepTo(float time)
        {
            if(State!=RunState.Playing)return;
            // Substeps preserve action order even during a slow rendered frame.
            while(RunTime<time&&State==RunState.Playing)
            {
                RunTime=Mathf.Min(time,RunTime+1f/120);Progress=Route.DistanceAt(RunTime);
                if(Demo)
                {
                    if(Bends<Route.bendTimes.Length&&RunTime>=Route.bendTimes[Bends])Tap(Route.bendTimes[Bends]);
                    int w=NextCrossing;if(w>=0&&RunTime>=Route.weaveTimes[w])Weave(Route.data.crossings[w].side,Route.weaveTimes[w]);
                    for(int j=0;j<optional.Length;j++)if(!optional[j]&&RunTime>=Route.data.optionalEvents[j].time){optional[j]=true;OptionalHits++;var e=Route.data.optionalEvents[j];Hit(e.distance,0,0,true,e.time);}
                }
                if(Bends<Route.data.turns.Length&&Progress>=Route.data.turns[Bends])
                {
                    if(bendArmed){Bends++;bendArmed=false;}
                    else if(RunTime>Route.bendTimes[Bends]+.3f)Fail("错过节拍 · 拨弦重音时点击续弯");
                }
                for(int i=0;i<passed.Length;i++)if(!passed[i]&&Progress>=Route.data.crossings[i].distance)
                {
                    if(Route.data.crossings[i].automatic||weaveArmed==i){passed[i]=true;CrossingsPassed++;if(weaveArmed==i)weaveArmed=-1;}
                    else if(RunTime>Route.weaveTimes[i]+.3f)Fail(Route.data.crossings[i].side>0?"错过上穿 ↑":"错过下穿 ↓");
                }
                if(State==RunState.Playing&&Progress>=Route.Total-.0001f)Finish();
            }
        }
        void Fail(string reason){State=RunState.Failed;Status=reason;music.Stop();}
        void Finish(){State=RunState.Revealing;Presentation=0;revealOrigin=cam.transform.position;revealFocus=focus;Status="一线成结 · 回环收拢";}
        public void Present(float dt)
        {
            Presentation+=dt;float amount=Mathf.Clamp01(Presentation/Route.data.pullDuration);
            rope.UpdateRope(Route.Total,amount);
            player.position=Route.Pull(Route.Total,amount)+Vector3.up*.14f;player.rotation=Quaternion.LookRotation(Route.Tangent(Route.Total,amount));player.localScale=Vector3.one*.46f;
            tail.gameObject.SetActive(true);tail.position=Route.Pull(0,amount)+Vector3.up*.14f;tail.localScale=Vector3.one*.33f;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(Presentation/4.8f));
            cam.transform.position=Vector3.Lerp(revealOrigin,new Vector3(0,62,-8),t);cam.transform.LookAt(Vector3.Lerp(revealFocus,new Vector3(0,0,-2),t));cam.fieldOfView=Mathf.Lerp(48,43,t);
            float dark=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,4.5f,Presentation));city.Darkness(dark);cam.backgroundColor=Color.Lerp(new Color(.16f,.36f,.27f),new Color(.055f,.19f,.135f),dark);
            if(amount>=1){State=RunState.Complete;Status="绳结收紧 · 一线成结";}
        }
        float InputTime => Mathf.Max(RunTime,(float)(AudioSettings.dspTime-dspOrigin));
        void Update()
        {
            if(transitionLocked)return;
            float dt=Time.deltaTime;
            if(!capture)
            {
                if(Input.GetKeyDown(KeyCode.R)){GetComponent<KnotUI>().Transition(()=>StartGame(false));return;}
                if(Input.GetKeyDown(KeyCode.D)){GetComponent<KnotUI>().Transition(()=>StartGame(true));return;}
                if(Input.GetKeyDown(KeyCode.Escape)){GetComponent<KnotUI>().Escape();return;}
                if(State==RunState.Playing&&!Demo)
                {
                    bool ui=EventSystem.current&&EventSystem.current.IsPointerOverGameObject();
                    if((Input.GetMouseButtonDown(0)&&!ui)||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.RightArrow))Tap(InputTime);
                    if(Input.GetMouseButtonDown(1)&&!ui){int i=NextCrossing;if(i>=0)Weave(Route.data.crossings[i].side,InputTime);}
                    if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))Weave(1,InputTime);
                    if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))Weave(-1,InputTime);
                }
            }
            if(State==RunState.Playing)
            {
                StepTo(Mathf.Max(0,(float)(AudioSettings.dspTime-dspOrigin)));
                rope.UpdateRope(Progress,-1);ropeMaterial.SetFloat("_Clock",RunTime);
                player.position=Route.Position(Progress)+Vector3.up*.14f;player.rotation=Quaternion.LookRotation(Route.Tangent(Progress));
                float cap=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,.9f,Progress));tail.gameObject.SetActive(cap>0);tail.localScale=Vector3.one*.33f*cap;
                if(Progress<Route.data.wrapStart)Follow(dt);
            }
            else if(IsPanorama)Present(dt);
            else if(State==RunState.Failed){var p=player.position;p.y=Mathf.Max(-.6f,p.y-dt*2);player.position=p;}
            if(State!=RunState.Paused)
            {
                headAge+=dt;hitAge+=dt;
                float entry=Mathf.SmoothStep(0,1,headAge/.28f);float pulse=Mathf.Exp(-hitAge*9)*Mathf.Sin(Mathf.Clamp01(hitAge/.55f)*Mathf.PI);
                player.localScale=new Vector3(.46f*(1-pulse*.12f),.46f*(1+pulse*hitSide*.42f),.46f*(1+pulse*.16f))*entry;
                hitRing.gameObject.SetActive(false);hitRing.localScale=Vector3.one*(1+hitAge*3);
            }
            city.Reveal(Progress);UpdateMarkers();
            if(capture&&!checkingUI&&!capturedVfx&&RunTime>10&&hitAge<.08f){capturedVfx=true;StartCoroutine(Capture("HeadVFX.png"));}
            if(capture&&!checkingUI&&!capturedWeave&&RunTime>15&&Mathf.Abs(hitSide)>.5f&&hitAge>.12f&&hitAge<.3f){capturedWeave=true;StartCoroutine(Capture("WeaveVFX.png"));}
            if(capture&&!checkingUI&&!capturedMid&&RunTime>=19){capturedMid=true;StartCoroutine(Capture("Gameplay.png"));}
            if(capture&&!checkingUI&&!capturedEnd&&State==RunState.Complete){capturedEnd=true;StartCoroutine(CaptureFinal());}
            if(capture&&!checkingUI&&State==RunState.Failed){File.WriteAllText(Path.Combine(captureDirectory,"runtime-failure.txt"),Status);Application.Quit(1);}
        }
        void Follow(float dt,bool instant=false)
        {
            float blend=instant?1:1-Mathf.Exp(-dt*2.5f);Vector3 flat=Route.Tangent(Progress);flat.y=0;
            heading=Vector3.Slerp(heading,flat.normalized,blend).normalized;
            focus=Vector3.Lerp(focus,player.position+heading*3.3f,instant?1:1-Mathf.Exp(-dt*6));
            cam.transform.position=focus-heading*16-Vector3.Cross(heading,Vector3.up)*9+Vector3.up*11;cam.transform.LookAt(focus);
        }
        void UpdateMarkers()
        {
            bool play=State==RunState.Playing||State==RunState.Ready;
            for(int i=0;i<guides.Length;i++)guides[i].SetActive(play&&Bends<Route.data.turns.Length&&guideDs[i]>Progress+.35f&&guideDs[i]<Progress+23);
            for(int i=0;i<optional.Length;i++){float until=Route.data.optionalEvents[i].time-RunTime;optionalRings[i].gameObject.SetActive(play&&!optional[i]&&until>=-.3f&&until<2);optionalRings[i].localScale=Vector3.one*Mathf.Lerp(1.25f,.75f,Mathf.Clamp01(1-until/.8f));}
            bool bend=play&&Bends<Route.data.turns.Length&&Route.data.turns[Bends]-Progress<14;
            bendRing.gameObject.SetActive(bend);
            if(bend){bendRing.position=Route.Position(Route.data.turns[Bends])+Vector3.up*.2f;bendRing.localScale=Vector3.one*Mathf.Lerp(1.45f,.85f,Mathf.Clamp01(1-(Route.bendTimes[Bends]-RunTime)/.75f));}
        }
        void Hit(float distance,float error,float side,bool optionalHit,float noteTime)
        {
            hitAge=0;hitSide=side;hitRing.position=Route.Position(distance)+Vector3.up*.2f;
            if(headVfx)headVfx.Pulse(hitRing.position,side,optionalHit);
            Feedback=optionalHit?"轻点 · 合鸣":side>0?"上穿 · 合拍":side<0?"下穿 · 合拍":"续弯 · 合拍";
            Score+=error<.12f?100:60;
            if(Application.isPlaying&&Sounds)effects.PlayOneShot(MakeTone(noteTime,side));
        }
        AudioClip MakeTone(float time,float side)
        {
            string key=time.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+":"+side;
            if(tones.TryGetValue(key,out var clip))return clip;
            const int rate=44100;int n=(int)(rate*.32f);var data=new float[n];float f=Route.PitchAt(time)*(side<0?.5f:1);
            for(int i=0;i<n;i++){float t=(float)i/rate;data[i]=(Mathf.Sin(2*Mathf.PI*f*t)+.25f*Mathf.Sin(4*Mathf.PI*f*t))*Mathf.Exp(-t*17)*Mathf.Min(1,t*400)*.3f;}
            clip=AudioClip.Create("Touch "+key,n,1,rate,false);clip.SetData(data,0);tones[key]=clip;return clip;
        }
        IEnumerator Capture(string file){if(file=="HeadVFX.png")yield return new WaitForSeconds(.14f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory,file));}
        IEnumerator CaptureUIThenPlay()
        {
            yield return GetComponent<KnotUI>().VerifyFlow(captureDirectory);
            checkingUI=false;
            StartGame(true);
        }
        IEnumerator CaptureFinal()
        {
            yield return new WaitForSeconds(.6f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory,"Finale.png"));
            File.WriteAllText(Path.Combine(captureDirectory,"runtime.json"),"{\"state\":\""+State+"\",\"bends\":"+Bends+",\"crossings\":"+CrossingsPassed+",\"optional\":"+OptionalHits+",\"score\":"+Score+",\"graphics\":\""+SystemInfo.graphicsDeviceName+"\"}");
            yield return new WaitForSeconds(3);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory,"Completed.png"));
            yield return new WaitForSeconds(1);Application.Quit();
        }
    }
}
