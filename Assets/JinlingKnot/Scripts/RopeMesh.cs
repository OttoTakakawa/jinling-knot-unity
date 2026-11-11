using UnityEngine;
using UnityEngine.Rendering;

namespace JinlingKnot
{
    public sealed class RopeMesh : MonoBehaviour
    {
        const int Sides = 12;
        KnotRoute route;
        Mesh mesh;
        Vector3[] vertices, normals, centers;
        Vector2[] uvs, born;
        int[] indices;
        float[] ds;
        float[] sin = new float[Sides+1], cos = new float[Sides+1];
        int lastRings = -1, previousEnd;
        float previousProgress=-1, previousPull=0;
        public void Initialize(KnotRoute value, Material material)
        {
            route = value; ds = value.data.pullDistances;
            int n=ds.Length*(Sides+1);
            vertices=new Vector3[n]; normals=new Vector3[n]; uvs=new Vector2[n]; born=new Vector2[n]; centers=new Vector3[ds.Length];
            indices=new int[(ds.Length-1)*Sides*6];
            for(int s=0;s<=Sides;s++) { sin[s]=Mathf.Sin(s*Mathf.PI*2/Sides); cos[s]=Mathf.Cos(s*Mathf.PI*2/Sides); }
            for(int i=0;i<ds.Length;i++)
            {
                float time = route.TimeAt(ds[i]);
                for(int s=0;s<=Sides;s++) { int j=i*(Sides+1)+s; uvs[j]=new Vector2((float)s/Sides,ds[i]); born[j]=new Vector2(time,0); }
                if(i==ds.Length-1)continue;
                for(int s=0;s<Sides;s++)
                {
                    int a=i*(Sides+1)+s,b=a+Sides+1,k=(i*Sides+s)*6;
                    indices[k]=a;indices[k+1]=a+1;indices[k+2]=b;indices[k+3]=a+1;indices[k+4]=b+1;indices[k+5]=b;
                }
            }
            mesh=new Mesh{name="Persistent silk rope",indexFormat=IndexFormat.UInt32}; mesh.MarkDynamic();
            mesh.SetVertexBufferParams(n,
                new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3,0),
                new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.Float32,3,1),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,2,2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float32,2,3));
            mesh.SetVertexBufferData(uvs,0,0,n,2);mesh.SetVertexBufferData(born,0,0,n,3);
            mesh.bounds=new Bounds(new Vector3(0,1,0),new Vector3(60,15,60));
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            UpdateRope(0,-1);
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--knot-capture")>=0)VerifyIncrementalUpdates();
        }
        public void UpdateRope(float progress, float pullAmount)
        {
            long measurement=PerformanceCapture.Begin();
            int end=Mathf.Min(KnotRoute.Index(ds,progress)+1,ds.Length-1);
            if(progress<=0)end=1;
            int first=pullAmount<0&&previousPull<0&&progress>=previousProgress?Mathf.Max(0,previousEnd-1):0;
            for(int i=first;i<=end;i++) centers[i]=route.Display(Mathf.Min(ds[i],progress),pullAmount)+Vector3.up*.14f;
            for(int i=first;i<=end;i++)
            {
                float d=Mathf.Min(ds[i],progress);
                Vector3 tangent=route.Tangent(d,pullAmount);
                Vector3 side=Vector3.Cross(Vector3.up,tangent).normalized;
                Vector3 up=Vector3.Cross(tangent,side).normalized;
                for(int s=0;s<=Sides;s++)
                {
                    int j=i*(Sides+1)+s;
                    Vector3 normal=side*cos[s]+up*sin[s];
                    normals[j]=normal;
                    float twist=Mathf.Sin(d*19+(float)s/Sides*18.84956f);
                    vertices[j]=centers[i]+normal*(.16f+twist*.003f);
                }
            }
            int offset=first*(Sides+1),count=(end-first+1)*(Sides+1);
            const MeshUpdateFlags flags=MeshUpdateFlags.DontRecalculateBounds;
            mesh.SetVertexBufferData(vertices,offset,offset,count,0,flags);
            mesh.SetVertexBufferData(normals,offset,offset,count,1,flags);
            previousEnd=end;previousProgress=progress;previousPull=pullAmount;
            int rings=progress<=.001f?0:end;
            if(lastRings!=rings) { mesh.SetIndices(indices,0,rings*Sides*6,MeshTopology.Triangles,0,false);lastRings=rings; }
            PerformanceCapture.End(measurement,false);
        }
        void VerifyIncrementalUpdates()
        {
            float[] progress={0,.25f,.26f,.7f,.4f,1,1,0,.6f};
            float[] pull={-1,-1,-1,-1,-1,-1,.6f,-1,-1};
            for(int test=0;test<progress.Length;test++)
            {
                float p=progress[test]*route.Total;UpdateRope(p,pull[test]);
                var actual=mesh.vertices;int end=p<=0?1:Mathf.Min(KnotRoute.Index(ds,p)+1,ds.Length-1);
                for(int i=0;i<=end;i++)
                {
                    float d=Mathf.Min(ds[i],p);var center=route.Display(d,pull[test])+Vector3.up*.14f;
                    var tangent=route.Tangent(d,pull[test]);var side=Vector3.Cross(Vector3.up,tangent).normalized;var up=Vector3.Cross(tangent,side).normalized;
                    for(int j=0;j<=Sides;j++)
                    {
                        var normal=side*cos[j]+up*sin[j];var expected=center+normal*(.16f+Mathf.Sin(d*19+(float)j/Sides*18.84956f)*.003f);
                        if((expected-actual[i*(Sides+1)+j]).sqrMagnitude>1e-8f)throw new System.InvalidOperationException("Incremental rope geometry mismatch");
                    }
                }
            }
            UpdateRope(0,-1);Debug.Log("CHECK PASS: incremental rope GPU vertices match full reconstruction in 9 growth/reset/pull scenarios");
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
