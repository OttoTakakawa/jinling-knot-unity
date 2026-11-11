using UnityEngine;
using UnityEngine.Rendering;
namespace JinlingKnot
{
    public sealed class ThreadHeadVFX : MonoBehaviour
    {
        KnotGame game;Transform head;Camera cameraRef;
        TrailRenderer trail;ParticleSystem motes,sparks;Transform star;
        Material starMaterial,trailMaterial;LineRenderer ring,arc,echo;
        float age=10,clock,side,weaveAge=10,weaveSide;Vector3 impact,weaveImpact;
        readonly Vector3[] ringPoints=new Vector3[48],echoPoints=new Vector3[48],arcPoints=new Vector3[32];
        readonly Color cream=new Color(1,.87f,.51f),orange=new Color(1,.42f,.12f),mint=new Color(.55f,1,.87f);
        public void Initialize(KnotGame owner,Transform target,Camera cam)
        {
            game=owner;head=target;cameraRef=cam;
            starMaterial=new Material(Shader.Find("Jinling/ThreadSpark"));trailMaterial=new Material(Shader.Find("Jinling/ThreadTrail"));
            star=GameObject.CreatePrimitive(PrimitiveType.Quad).transform;star.name="Thread head star";star.gameObject.layer=30;star.SetParent(transform,false);Destroy(star.GetComponent<Collider>());star.GetComponent<Renderer>().sharedMaterial=starMaterial;
            var o=new GameObject("Golden afterglow");o.layer=30;o.transform.SetParent(transform,false);trail=o.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMaterial;trail.time=.46f;trail.minVertexDistance=.045f;trail.widthCurve=AnimationCurve.EaseInOut(0,.32f,1,0);trail.numCapVertices=3;trail.shadowCastingMode=ShadowCastingMode.Off;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(cream,0),new GradientColorKey(orange,1)},new[]{new GradientAlphaKey(.75f,0),new GradientAlphaKey(0,1)});trail.colorGradient=gradient;
            motes=Particles("Floating thread sparks",14,.55f,.10f,.17f);sparks=Particles("Beat sparks",0,.65f,.13f,.26f);
            ring=Line("Beat ripple",48,.075f);echo=Line("Beat echo",48,.035f);arc=Line("Weave direction arc",32,.105f);
        }
        ParticleSystem Particles(string name,float rate,float life,float low,float high)
        {
            var o=new GameObject(name);o.layer=30;o.transform.SetParent(transform,false);var ps=o.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.startLifetime=life;main.startSize=new ParticleSystem.MinMaxCurve(low,high);main.startSpeed=.25f;main.startColor=cream;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=96;
            var emit=ps.emission;emit.rateOverTime=rate;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.18f;
            var colors=ps.colorOverLifetime;colors.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(cream,0),new GradientColorKey(orange,1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});colors.color=g;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=starMaterial;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
            ps.Play();return ps;
        }
        LineRenderer Line(string name,int points,float width)
        {
            var o=new GameObject(name);o.layer=30;o.transform.SetParent(transform,false);var line=o.AddComponent<LineRenderer>();line.sharedMaterial=trailMaterial;line.positionCount=points;line.widthMultiplier=width;line.useWorldSpace=true;line.shadowCastingMode=ShadowCastingMode.Off;line.enabled=false;return line;
        }
        public void Clear(){age=weaveAge=10;if(ring)ring.enabled=false;if(echo)echo.enabled=false;if(arc)arc.enabled=false;if(trail)trail.Clear();if(motes)motes.Clear();if(sparks)sparks.Clear();}
        public void Pulse(Vector3 position,float direction,bool optional)
        {
            if(!sparks)return;age=0;impact=position+Vector3.up*.1f;side=direction;
            if(Mathf.Abs(direction)>.5f){weaveAge=0;weaveSide=direction;weaveImpact=impact;}
            for(int i=0;i<(optional?7:14);i++)
            {
                float a=i*2.39996f;Vector3 velocity=new Vector3(Mathf.Cos(a),.4f+Mathf.Abs(direction)*.65f,Mathf.Sin(a))*(optional?.8f:1.4f);
                var p=new ParticleSystem.EmitParams{position=impact,velocity=velocity,startColor=direction<0?mint:cream,startLifetime=.35f+(i%4)*.07f};sparks.Emit(p,1);
            }
        }
        void LateUpdate()
        {
            if(!head)return;bool live=game.State==RunState.Playing;bool paused=game.State==RunState.Paused;
            if(paused){motes.Pause();sparks.Pause();return;}
            if(!motes.isPlaying)motes.Play();if(!sparks.isPlaying)sparks.Play();
            clock+=Time.deltaTime;age+=Time.deltaTime;weaveAge+=Time.deltaTime;
            Vector3 p=head.position+Vector3.up*.19f;
            trail.transform.position=p;trail.emitting=live;motes.transform.position=p;var e=motes.emission;e.enabled=live;
            star.gameObject.SetActive(live);star.position=p;star.rotation=cameraRef.transform.rotation;
            float pulse=Mathf.Exp(-age*8);star.localScale=Vector3.one*(1.12f+Mathf.Sin(clock*6)*.06f+pulse*.85f);
            starMaterial.SetColor("_Color",new Color(1,1,1,.9f));
            ring.enabled=live&&age<.68f;echo.enabled=live&&age>.10f&&age<.68f;
            arc.enabled=live&&weaveAge<.85f;
            float fade=Mathf.Clamp01(1-age/.68f);Color tint=side<0?mint:cream;tint.a=fade*.95f;
            ring.startColor=ring.endColor=tint;tint.a*=.6f;echo.startColor=echo.endColor=tint;
            if(ring.enabled){
                for(int i=0;i<48;i++){
                    float a=i*Mathf.PI*2/47;var direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    ringPoints[i]=impact+direction*(.36f+age*2.6f);
                    echoPoints[i]=impact+direction*(.24f+Mathf.Max(0,age-.1f)*2.2f);
                }
                ring.SetPositions(ringPoints);if(echo.enabled)echo.SetPositions(echoPoints);
            }
            if(arc.enabled){
                Color weaveTint=weaveSide<0?mint:cream;weaveTint.a=Mathf.Clamp01(1-weaveAge/.85f);arc.startColor=arc.endColor=weaveTint;
                Vector3 right=cameraRef.transform.right;
                for(int i=0;i<32;i++){
                    float a=i/31f*Mathf.PI;arcPoints[i]=weaveImpact+right*Mathf.Cos(a)*(.65f+weaveAge*.6f)+Vector3.up*(Mathf.Sin(a)*weaveSide*(.65f+weaveAge*.5f));
                }
                arc.SetPositions(arcPoints);
            }
        }
        void OnDestroy(){if(starMaterial)Destroy(starMaterial);if(trailMaterial)Destroy(trailMaterial);}
    }
}
