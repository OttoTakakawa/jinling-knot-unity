using System;
using UnityEngine;

namespace JinlingKnot
{
    [Serializable] public class Crossing { public float first, distance, side; public bool automatic; }
    [Serializable] public class Beat { public string action; public int index; public float time, distance; }
    [Serializable] public class ScoreNote { public float time, pitch; }
    [Serializable] public class RouteData
    {
        public Vector3[] points;
        public float[] distances, turns, times, beatDistances, velocities, pullDistances;
        public Crossing[] crossings;
        public Beat[] events, optionalEvents;
        public ScoreNote[] notes;
        public float wrapStart, endTime, pullDuration;
        public int pullFrames, pullNodes;
    }
    // Coordinates are converted once from Godot RH to Unity LH by reflecting Z.
    public sealed class KnotRoute
    {
        public readonly RouteData data;
        public readonly float[] bendTimes, weaveTimes;
        readonly float[] pull;
        public float Total => data.distances[data.distances.Length - 1];
        public KnotRoute()
        {
            data = JsonUtility.FromJson<RouteData>(Resources.Load<TextAsset>("Route").text);
            var bytes = Resources.Load<TextAsset>("KnotPull").bytes;
            if (bytes.Length != data.pullFrames * data.pullNodes * 16) throw new InvalidOperationException("Incomplete knot pull cache");
            pull = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, pull, 0, bytes.Length);
            bendTimes = new float[data.turns.Length]; weaveTimes = new float[data.crossings.Length];
            foreach (var e in data.events)
                if (e.action == "turn") bendTimes[e.index] = e.time; else weaveTimes[e.index] = e.time;
        }
        public static int Index(float[] ds, float d)
        {
            int low = 0, high = ds.Length - 2;
            while (low < high) { int mid = (low + high + 1) / 2; if (ds[mid] <= d) low = mid; else high = mid - 1; }
            return low;
        }
        public Vector3 Position(float d)
        {
            int i = Index(data.distances, d);
            return Vector3.Lerp(data.points[i], data.points[i + 1], Mathf.InverseLerp(data.distances[i], data.distances[i + 1], d));
        }
        Vector3 PullNode(int n, int f)
        {
            int i = (f * data.pullNodes + n) * 4;
            return new Vector3(pull[i], pull[i + 1], -pull[i + 2]);
        }
        public Vector3 Pull(float d, float amount)
        {
            int i = Index(data.pullDistances, d);
            float u = Mathf.InverseLerp(data.pullDistances[i], data.pullDistances[i + 1], d);
            float frame = Mathf.Clamp01(amount) * (data.pullFrames - 1);
            int f = Mathf.Min((int)frame, data.pullFrames - 2);
            return Vector3.Lerp(Vector3.Lerp(PullNode(i, f), PullNode(i + 1, f), u), Vector3.Lerp(PullNode(i, f + 1), PullNode(i + 1, f + 1), u), frame - f);
        }
        public Vector3 Display(float d, float amount) => amount < 0 ? Position(d) : Pull(d, amount);
        public Vector3 Tangent(float d, float amount = -1) => (Display(Mathf.Min(Total, d + .12f), amount) - Display(Mathf.Max(0, d - .12f), amount)).normalized;
        public float DistanceAt(float time)
        {
            if (time >= data.endTime) return Total;
            int i = Index(data.times, time);
            float dt = data.times[i + 1] - data.times[i], t = Mathf.Clamp01((time - data.times[i]) / dt), t2 = t*t, t3 = t2*t;
            return (2*t3-3*t2+1)*data.beatDistances[i] + (t3-2*t2+t)*dt*data.velocities[i] + (-2*t3+3*t2)*data.beatDistances[i+1] + (t3-t2)*dt*data.velocities[i+1];
        }
        public float TimeAt(float distance)
        {
            float a = 0, b = data.endTime;
            for (int i = 0; i < 22; i++) { float m = (a+b)*.5f; if (DistanceAt(m) < distance) a=m; else b=m; }
            return (a+b)*.5f;
        }
        public float PitchAt(float time)
        {
            float best = float.MaxValue, pitch = 74;
            foreach (var n in data.notes) if (Mathf.Abs(n.time-time)<best) { best=Mathf.Abs(n.time-time); pitch=n.pitch; }
            return 440*Mathf.Pow(2,(pitch-69)/12);
        }
    }
}
