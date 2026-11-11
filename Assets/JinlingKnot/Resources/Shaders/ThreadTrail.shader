Shader "Jinling/ThreadTrail" {
 SubShader {Tags{"Queue"="Transparent+10" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off Cull Off
 Pass{ CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct v{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color;return o;}
 fixed4 frag(v i):SV_Target{return fixed4(i.color.rgb,i.color.a*smoothstep(0,.35,1-abs(i.uv.y*2-1)));}
 ENDCG }
 }
}
