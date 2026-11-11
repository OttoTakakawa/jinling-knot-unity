using UnityEngine;
namespace JinlingKnot
{
    [RequireComponent(typeof(Renderer))]
    public sealed class RiverShoreline : MonoBehaviour
    {
        Texture2D distance;
        void Start()
        {
            var data=Resources.Load<TextAsset>("ShoreDistance");
            distance=new Texture2D(512,512,TextureFormat.RFloat,false,true){name="Static shore distance",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            distance.LoadRawTextureData(data.bytes);distance.Apply(false,true);
            GetComponent<Renderer>().sharedMaterial.SetTexture("_ShoreDistance",distance);
        }
        void OnDestroy(){if(distance)Destroy(distance);}
    }
}
