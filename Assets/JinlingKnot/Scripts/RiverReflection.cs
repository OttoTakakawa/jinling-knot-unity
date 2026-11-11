using UnityEngine;
namespace JinlingKnot
{
    [RequireComponent(typeof(Renderer))]
    public sealed class RiverReflection : MonoBehaviour
    {
        Camera mirror; RenderTexture texture; Material material; bool rendering;Renderer surface;Camera mainCamera;int renderedFrame=-1;
        void Awake(){gameObject.layer=4;surface=GetComponent<Renderer>();material=surface.sharedMaterial;mainCamera=Camera.main;gameObject.AddComponent<RiverShoreline>();}
        void OnWillRenderObject()
        {
            var source=Camera.current;
            if(rendering||!source||source!=mainCamera||renderedFrame==Time.frameCount)return;
            if(!mirror)
            {
                mirror=new GameObject("River reflection camera",typeof(Camera)).GetComponent<Camera>();mirror.enabled=false;
                texture=new RenderTexture(1536,864,16,RenderTextureFormat.ARGB32){name="River reflection",filterMode=FilterMode.Bilinear,antiAliasing=2};
            }
            mirror.CopyFrom(source);mirror.enabled=false;mirror.targetTexture=texture;mirror.depthTextureMode=DepthTextureMode.None;
            mirror.cullingMask=source.cullingMask&~((1<<4)|(1<<30));mirror.useOcclusionCulling=false;mirror.allowHDR=false;mirror.allowMSAA=true;
            renderedFrame=Time.frameCount;float height=surface.bounds.max.y;
            var reflect=Matrix4x4.identity;reflect.m11=-1;reflect.m13=2*height;
            mirror.transform.position=reflect.MultiplyPoint(source.transform.position);
            mirror.transform.rotation=Quaternion.Euler(-source.transform.eulerAngles.x,source.transform.eulerAngles.y,source.transform.eulerAngles.z);
            mirror.worldToCameraMatrix=source.worldToCameraMatrix*reflect;
            Vector3 planePoint=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,height+.025f,0));
            Vector3 planeNormal=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            mirror.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(planeNormal.x,planeNormal.y,planeNormal.z,-Vector3.Dot(planePoint,planeNormal)));
            bool previous=GL.invertCulling;
            long measurement=PerformanceCapture.Begin();
            try{rendering=true;GL.invertCulling=!previous;mirror.Render();}
            finally{GL.invertCulling=previous;rendering=false;PerformanceCapture.End(measurement,true);}
            material.SetTexture("_ReflectionTex",texture);material.SetFloat("_HasReflection",1);
        }
        void OnDestroy(){if(mirror)Destroy(mirror.gameObject);if(texture){texture.Release();Destroy(texture);}if(material){material.SetFloat("_HasReflection",0);material.SetTexture("_ReflectionTex",null);}}
    }
}
