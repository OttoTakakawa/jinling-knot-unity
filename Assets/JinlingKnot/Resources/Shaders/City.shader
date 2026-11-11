Shader "Jinling/City" {
 Properties { _Color("Print color",Color)=(1,1,1,1) _Glow("Warm accent",Float)=0 _Darkness("Darkness",Range(0,1))=0 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float _Glow,_Darkness;
 struct v2f{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target {
 float light=dot(normalize(i.normal),normalize(float3(-.4,.85,-.3)));
 float shade=light>.55?1.0:light>-.15?.88:.72;
 float grain=frac(sin(dot(floor(i.world*115),float3(12.9898,78.233,37.719)))*43758.5453)-.5;
 float3 color=_Color.rgb*shade+grain*.018;
 color=lerp(color,_Color.rgb,saturate(_Glow));
 return fixed4(color*(1-_Darkness),1);
 }
 ENDCG }
 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 } Fallback "Unlit/Color"
}
