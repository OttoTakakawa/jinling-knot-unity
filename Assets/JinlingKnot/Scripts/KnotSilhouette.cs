using UnityEngine;
using UnityEngine.UI;
namespace JinlingKnot
{
    // Vector silhouette from the actual final knot, not unrelated decorative art.
    public sealed class KnotSilhouette : MaskableGraphic
    {
        Vector2[] points;
        public void SetRoute(KnotRoute route)
        {
            points=new Vector2[route.data.pullDistances.Length];Vector2 lo=Vector2.one*10000,hi=-lo;
            for(int i=0;i<points.Length;i++){var p=route.Pull(route.data.pullDistances[i],1);points[i]=new Vector2(p.z,-p.x);lo=Vector2.Min(lo,points[i]);hi=Vector2.Max(hi,points[i]);}
            float span=Mathf.Max(hi.x-lo.x,hi.y-lo.y);for(int i=0;i<points.Length;i++)points[i]=(points[i]-(lo+hi)*.5f)/span;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(points==null)return;var rect=rectTransform.rect;float scale=Mathf.Min(rect.width,rect.height)*.88f;float width=scale*.018f;
            for(int i=0;i<points.Length-1;i++){
                Vector2 a=rect.center+points[i]*scale,b=rect.center+points[i+1]*scale;var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int k=vh.currentVertCount;
                vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
            }
        }
    }
}
