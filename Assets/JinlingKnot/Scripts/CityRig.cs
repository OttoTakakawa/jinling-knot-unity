using System;
using UnityEngine;

namespace JinlingKnot
{
    [Serializable] public class CityGroup { public Transform root; public float start; }
    public sealed class CityRig : MonoBehaviour
    {
        public CityGroup[] groups;
        public Material[] materials;
        public void Reveal(float distance)
        {
            foreach(var g in groups)
            {
                float t=Mathf.Clamp01((distance-g.start)/6);
                g.root.gameObject.SetActive(t>0);
                g.root.localPosition=Vector3.down*(1-Mathf.SmoothStep(0,1,t))*6;
            }
        }
        public void Darkness(float amount)
        {
            foreach(var m in materials) m.SetFloat("_Darkness",amount);
            gameObject.SetActive(amount<.9999f);
        }
    }
}
