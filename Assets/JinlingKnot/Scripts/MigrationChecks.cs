using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JinlingKnot
{
    // Runs the actual runtime rules with injected timestamps; no alternate gameplay model.
    public static class MigrationChecks
    {
        static int checks;
        static void Check(bool condition,string label)
        {
            if(!condition)throw new Exception("Migration check failed: "+label);
            checks++;Debug.Log("CHECK PASS: "+label);
        }
        public static void Run(KnotGame game,string directory)
        {
            checks=0;var route=game.Route;var d=route.data;
            Check(d.turns.Length==13&&d.crossings.Length==13&&d.optionalEvents.Length==28,"original action counts");
            Check(d.pullFrames==241&&d.pullNodes==1251,"full contact simulation retained");
            for(int i=0;i<d.events.Length;i++)Check(Mathf.Abs(route.DistanceAt(d.events[i].time)-d.events[i].distance)<.001f,"beat distance "+i);
            game.StartGame(true);game.StepTo(d.endTime+.1f);
            Check(game.State==RunState.Revealing,"automatic full run");
            Check(game.Bends==13&&game.CrossingsPassed==13&&game.OptionalHits==28,"all automatic actions");
            game.Present(d.pullDuration+.1f);Check(game.State==RunState.Complete,"tightening and finale");
            game.StartGame(false);Check(game.Progress==0&&game.Score==0&&game.CrossingsPassed==0,"restart clears prior run");
            game.StepTo(2);Check(game.State==RunState.Failed,"missed bend fails");
            Manual(game,0,false);Check(game.State==RunState.Revealing&&game.OptionalHits==0,"manual required inputs; optional misses unpenalized");
            Manual(game,-.29f,false);Check(game.State==RunState.Revealing,"early edge inside timing window");
            Manual(game,.29f,false);Check(game.State==RunState.Revealing,"late edge inside timing window");
            Manual(game,0,true);Check(game.State==RunState.Failed,"wrong crossing direction fails");
            game.StartGame(false);game.Tap(route.bendTimes[0]-.31f);game.StepTo(2);Check(game.State==RunState.Failed,"input outside timing window rejected");
            game.StartGame(false);game.Pause();Check(game.State==RunState.Paused,"pause state");game.Pause();Check(game.State==RunState.Playing,"resume state");
            Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"checks.json"),"{\"passed\":"+checks+",\"failed\":0,\"engine\":\""+Application.unityVersion+"\"}");
            game.ResetGame();
        }
        static void Manual(KnotGame game,float offset,bool wrongCrossing)
        {
            game.StartGame(false);var events=new List<Beat>(game.Route.data.events);events.Sort((a,b)=>a.time.CompareTo(b.time));
            foreach(var e in events)
            {
                float t=e.time+offset;game.StepTo(t);
                if(game.State!=RunState.Playing)return;
                if(e.action=="turn")game.Tap(t);
                else {float side=game.Route.data.crossings[e.index].side;game.Weave(wrongCrossing?-side:side,t);}
            }
            game.StepTo(game.Route.data.endTime+.1f);
        }
    }
}
