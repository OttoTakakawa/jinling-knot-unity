Shader "Jinling/ThreadSpark" {
 Properties {_Color("Tint",Color)=(1,.85,.45,1)}
 SubShader {Tags{"Queue"="Transparent+20" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off Cull Off
 Pass{ CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct v{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv*2-1;o.color=i.color*_Color;return o;}
 fixed4 frag(v i):SV_Target{float2 p=abs(i.uv);float r=length(p);float core=pow(saturate(1-r),4);float star=pow(saturate(1-p.x-p.y),3);float arms=(1-smoothstep(.015,.09,min(p.x,p.y)))*pow(saturate(1-max(p.x,p.y)),1.5);float alpha=saturate(core*.4+star*.5+arms);return fixed4(i.color.rgb,alpha*i.color.a);}
 ENDCG }
 }
}
