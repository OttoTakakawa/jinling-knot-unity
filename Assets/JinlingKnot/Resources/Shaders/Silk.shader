Shader "Jinling/Silk" {
 Properties { _Clock("Weave clock",Float)=0 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float _Clock;
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 birth:TEXCOORD1;};
 struct v2f{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float2 uv:TEXCOORD1;float birth:TEXCOORD2;};
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;o.birth=v.birth.x;return o;}
 fixed4 frag(v2f i):SV_Target {
 float light=dot(normalize(i.normal),normalize(float3(-.3,.9,-.25)));
 float3 ink=float3(.45,.095,.025),orange=float3(.98,.26,.055),highlight=float3(1,.56,.16);
 float3 color=light<-.25?ink:light>.55?highlight:orange;
 float braid=sin(i.uv.y*19+i.uv.x*18.84956);
 color*=1-smoothstep(.89,1,braid)*.075;
 float trim=smoothstep(.97,.995,sin(i.uv.x*6.28318));
 color=lerp(color,float3(1,.88,.59),trim*.65);
 float newborn=exp(-max(0,_Clock-i.birth)*4)*.12;
 return fixed4(lerp(color,float3(1,.77,.34),newborn),1);
 }
 ENDCG }
 } Fallback "Unlit/Color"
}
