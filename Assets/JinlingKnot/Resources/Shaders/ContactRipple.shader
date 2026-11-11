Shader "Jinling/ContactRipple" {
 Properties { _Darkness("Darkness",Range(0,1))=0 _Color("Foam",Color)=(.84,.94,.83,.8) }
 SubShader {Tags{"Queue"="Transparent+4" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;float _Darkness;
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy*2-1;return o;}
 fixed4 frag(v2f i):SV_Target{
 float r=length(i.uv);float a=atan2(i.uv.y,i.uv.x);
 float wave=frac(r*4-_Time.y*.35);float aa=max(fwidth(wave),.025);
 float lines=(1-smoothstep(.045,.045+aa,abs(wave-.5)));
 float breaks=smoothstep(-.5,.15,sin(a*6+r*9+_Time.y*.2));
 float fade=smoothstep(.35,.55,r)*(1-smoothstep(.78,1,r));
 return fixed4(_Color.rgb,lines*breaks*fade*_Color.a*(1-_Darkness));
 }
 ENDCG }
 }
}
