using UnityEngine;
using UnityEngine.Rendering;
namespace JinlingKnot
{
    public sealed class RiverContactEffects : MonoBehaviour
    {
        Material material;CityRig city;
        void Start()
        {
            material=new Material(Shader.Find("Jinling/ContactRipple"));
            city=GetComponent<CityRig>();
            if(city){var list=new System.Collections.Generic.List<Material>(city.materials);list.Add(material);city.materials=list.ToArray();}
            // Existing authored hull meshes. Quays use the baked shoreline in Water.shader.
            foreach(var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                if(r.name!="Architecture 162"&&r.name!="Architecture 172")continue;
                var b=r.bounds;var o=GameObject.CreatePrimitive(PrimitiveType.Quad);o.name="Hull foam ripples";o.layer=30;
                Destroy(o.GetComponent<Collider>());o.transform.SetParent(transform,true);
                o.transform.position=new Vector3(b.center.x,-.415f,b.center.z);
                o.transform.rotation=Quaternion.Euler(90,0,0);
                o.transform.localScale=new Vector3(b.size.x+2.2f,b.size.z+2.0f,1);
                var render=o.GetComponent<MeshRenderer>();render.sharedMaterial=material;render.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
}
