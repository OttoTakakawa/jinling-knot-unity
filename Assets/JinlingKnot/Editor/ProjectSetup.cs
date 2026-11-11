using System;
using System.Collections.Generic;
using System.IO;
using JinlingKnot;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace JinlingKnot.Editor
{
    [Serializable] class CityDescription { public GroupDescription[] groups;public MeshDescription[] meshes; }
    [Serializable] class GroupDescription { public float start; }
    [Serializable] class MeshDescription { public int group,vertices,indices;public Color color;public float emission;public bool water; }
    public static class ProjectSetup
    {
        const string Root="Assets/JinlingKnot";
        [MenuItem("金陵绳结/重建迁移场景")]
        public static void CreateScene()
        {
            Directory.CreateDirectory(Root+"/Generated/Meshes");Directory.CreateDirectory(Root+"/Generated/Materials");Directory.CreateDirectory(Root+"/Scenes");AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game=new GameObject("Jinling Knot").AddComponent<KnotGame>();
            game.menuPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JinlingKnotUI/Prefabs/JinlingMainMenu.prefab");
            game.hudPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JinlingKnotUI/Prefabs/HUD.prefab");
            game.pausePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JinlingKnotUI/Prefabs/Paused.prefab");
            game.completePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JinlingKnotUI/Prefabs/Completed.prefab");
            game.retryPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JinlingKnotUI/Prefabs/Retry.prefab");
            var city=new GameObject("Qinhuai City").AddComponent<CityRig>();game.city=city;
            var desc=JsonUtility.FromJson<CityDescription>(Resources.Load<TextAsset>("City").text);
            city.groups=new CityGroup[desc.groups.Length];
            for(int i=0;i<city.groups.Length;i++) {var o=new GameObject("Facade "+i);o.transform.SetParent(city.transform);city.groups[i]=new CityGroup{root=o.transform,start=desc.groups[i].start};}
            var mats=new Dictionary<string,Material>();
            using(var reader=new BinaryReader(new MemoryStream(Resources.Load<TextAsset>("CityGeometry").bytes)))
            for(int i=0;i<desc.meshes.Length;i++)
            {
                var d=desc.meshes[i];var v=new Vector3[d.vertices];var n=new Vector3[d.vertices];var idx=new int[d.indices];
                for(int j=0;j<v.Length;j++){v[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());n[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}
                for(int j=0;j<idx.Length;j++)idx[j]=reader.ReadInt32();
                var mesh=new Mesh{name="Qinhuai "+i,indexFormat=IndexFormat.UInt32};mesh.vertices=v;mesh.normals=n;mesh.triangles=idx;mesh.RecalculateBounds();
                string path=Root+"/Generated/Meshes/City_"+i.ToString("D3")+".asset";AssetDatabase.CreateAsset(mesh,path);
                string key=d.water?"water":ColorUtility.ToHtmlStringRGB(d.color)+"_"+d.emission.ToString("F2",System.Globalization.CultureInfo.InvariantCulture);
                if(!mats.TryGetValue(key,out var material))
                {
                    material=new Material(Shader.Find(d.water?"Jinling/Water":"Jinling/City")){name=key};material.SetColor("_Color",ArtColor(d.color));material.SetFloat("_Glow",Mathf.Min(.12f,d.emission));
                    AssetDatabase.CreateAsset(material,Root+"/Generated/Materials/"+key+".mat");mats[key]=material;
                }
                var o=new GameObject(d.water?"River":"Architecture "+i);o.transform.SetParent(d.group<0?city.transform:city.groups[d.group].root,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            city.materials=new List<Material>(mats.Values).ToArray();
            // Restore Chinese plaques omitted by mesh extraction (Label3D is text, not geometry).
            AddPlaque(city,"金陵",new Vector3(0,3f,15.19f),.0875f,Quaternion.Euler(0,180,0));
            PlayerSettings.companyName="Jinling";PlayerSettings.productName="金陵绳结 · 秦淮一线";PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.defaultScreenWidth=1672;PlayerSettings.defaultScreenHeight=941;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.colorSpace=ColorSpace.Gamma;PlayerSettings.runInBackground=true;
            QualitySettings.vSyncCount=1;QualitySettings.antiAliasing=4;
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/JinlingKnot.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/JinlingKnot.unity",true)};
            AssetDatabase.SaveAssets();Debug.Log("MIGRATION SCENE READY: "+desc.meshes.Length+" meshes, "+desc.groups.Length+" reveal groups");
        }
        static Color ArtColor(Color c)
        {
            // Recolor existing authored meshes; no new architecture is synthesized.
            if(c.r>.9f&&c.g>.65f)return new Color(1f,.83f,.44f);
            if(c.r>.65f&&c.g<c.r*.55f)return new Color(.94f,.25f,.075f);
            if(c.r>.7f&&c.g>.65f)return new Color(.97f,.88f,.67f);
            if(c.r>.6f&&c.g>.4f)return new Color(.78f,.59f,.29f);
            if(c.r>.35f&&c.g<c.r*.65f)return new Color(.48f,.23f,.13f);
            if(c.g>c.r*1.2f)return new Color(.17f,.40f,.26f);
            if(c.r<.3f)return new Color(.065f,.24f,.17f);
            return Color.Lerp(new Color(.48f,.60f,.40f),new Color(.96f,.87f,.67f),Mathf.InverseLerp(.34f,.65f,c.r));
        }
        static void AddPlaque(CityRig city,string text,Vector3 pos,float scale,Quaternion rotation)
        {
            // Plaque remains part of the gate reveal group.
            var o=new GameObject("Gate plaque");o.transform.SetParent(city.groups[0].root,false);o.transform.position=pos;o.transform.rotation=rotation;
            var tm=o.AddComponent<TextMesh>();tm.text=text;tm.font=Resources.Load<Font>("NotoSansCJKsc-Regular");tm.fontSize=64;tm.characterSize=scale;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.96f,.71f,.37f);o.GetComponent<Renderer>().sharedMaterial=tm.font.material;
        }
        [MenuItem("金陵绳结/构建 macOS 预览")]
        public static void BuildMac()
        {
            if(!File.Exists(Root+"/Scenes/JinlingKnot.unity"))CreateScene();
            Directory.CreateDirectory("Builds");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Scenes/JinlingKnot.unity"},locationPathName="Builds/JinlingKnot.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+result.summary.result);
            Debug.Log("MIGRATION BUILD PASSED");
        }
        public static void PrepareAndBuild(){CreateScene();BuildMac();}
    }
}
