using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace JinlingKnot
{
    public sealed class PerformanceCapture : MonoBehaviour
    {
        static PerformanceCapture active;
        KnotGame game;string output;
        readonly List<float> frames=new List<float>(20000);
        double ropeMs,reflectionMs;int ropeCalls,reflectionCalls;
        public static bool Sampling => active&&active.game.State==RunState.Playing&&active.game.RunTime>3&&active.game.RunTime<30;
        public static long Begin()=>Sampling?System.Diagnostics.Stopwatch.GetTimestamp():0;
        public static void End(long start,bool reflection){if(start==0||!active)return;double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;if(reflection){active.reflectionMs+=ms;active.reflectionCalls++;}else{active.ropeMs+=ms;active.ropeCalls++;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="--knot-benchmark"){var p=new GameObject("Performance capture").AddComponent<PerformanceCapture>();p.output=args[i+1];p.game=FindObjectOfType<KnotGame>();active=p;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;break;}}
        void Update(){if(Sampling)frames.Add(Time.unscaledDeltaTime*1000);}
        [Serializable]class Result{public int frames,ropeCalls,reflectionCalls;public float meanFrameMs,p95FrameMs,ropeCpuMs,reflectionSubmitCpuMs;public string device;}
        void OnApplicationQuit(){if(frames.Count==0)return;frames.Sort();double sum=0;foreach(float f in frames)sum+=f;var r=new Result{frames=frames.Count,meanFrameMs=(float)(sum/frames.Count),p95FrameMs=frames[(int)((frames.Count-1)*.95)],ropeCalls=ropeCalls,reflectionCalls=reflectionCalls,ropeCpuMs=(float)(ropeMs/Math.Max(1,ropeCalls)),reflectionSubmitCpuMs=(float)(reflectionMs/Math.Max(1,reflectionCalls)),device=SystemInfo.graphicsDeviceName};File.WriteAllText(output,JsonUtility.ToJson(r,true));}
    }
}
