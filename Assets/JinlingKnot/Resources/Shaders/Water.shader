Shader "Jinling/Water" {
 Properties {
 _DeepColor("Deep turquoise",Color)=(.065,.34,.36,1)
 _ShallowColor("Shallow jade",Color)=(.29,.64,.55,1)
 _FoamColor("Foam",Color)=(.84,.94,.83,1)
 _ReflectionTex("Reflection",2D)="black"{}
 _HasReflection("Reflection enabled",Float)=0
 _Absorption("RGB absorption per metre",Vector)=(.65,.16,.10,0)
 _Scattering("RGB scattering per metre",Vector)=(.10,.30,.27,0)
 _BedColor("Riverbed tint",Color)=(.34,.47,.32,1)
 _FoamWidth("Shore foam width",Range(.1,1))=.42
 _Darkness("Darkness",Range(0,1))=0
 }
 SubShader {Tags{"RenderType"="Transparent" "Queue"="Transparent"} ZWrite Off Cull Back
 GrabPass { "_RiverBackground" }
 Pass { CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
 sampler2D _ReflectionTex,_RiverBackground;float4 _Absorption,_Scattering,_BedColor;float _FoamWidth;sampler2D _ShoreDistance; float4 _DeepColor,_ShallowColor,_FoamColor;float _Darkness,_HasReflection;
 struct v2f{float4 pos:SV_POSITION;float3 world:TEXCOORD0;float4 screen:TEXCOORD1;float eye:TEXCOORD2;float4 grab:TEXCOORD3;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.screen=ComputeScreenPos(o.pos);o.eye=-UnityObjectToViewPos(v.vertex).z;o.grab=ComputeGrabScreenPos(o.pos);return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 fixed4 frag(v2f i):SV_Target{
 float2 p=i.world.xz;float t=_Time.y;
 // Crossed, long gentle waves perturb reflection instead of drawing noise contours.
 float a=dot(p,float2(.36,.78))+t*.48;
 float b=dot(p,float2(-.73,.25))-t*.37;
 float c=dot(p,float2(1.57,.92))+t*.73;
 float2 slope=float2(.36,.78)*cos(a)*.06+float2(-.73,.25)*cos(b)*.045+float2(1.57,.92)*cos(c)*.012;
 float3 n=normalize(float3(-slope.x,1,-slope.y));float3 v=normalize(_WorldSpaceCameraPos-i.world);
 float2 screen=i.screen.xy/i.screen.w;
 float scene=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)));
 float viewCos=max(.15,dot(v,UNITY_MATRIX_V[2].xyz));
 float rayDepth=max(0,scene-i.eye)/viewCos;
 // One cached distance sample replaces a loop over 24 static shore segments.
 float shore=tex2Dlod(_ShoreDistance,float4((p+32)/64,0,0)).r;
 // Beyond the bake domain there are no quays; avoid clamped-edge foam.
 shore+=length(max(abs(p)-32,0));
 // Authored bathymetry fallback: this scene has vertical quays but no riverbed.
 // Actual opaque underwater geometry limits the optical path where present.
 float bedDepth=min(3.2,.22+shore*.65);
 float path=min(rayDepth,bedDepth/max(.25,v.y));
 float gap=min(rayDepth*abs(v.y),bedDepth);
 float far=smoothstep(16,100,i.eye);
 float3 sigmaT=max(.001,_Absorption.rgb+_Scattering.rgb);
 float3 transmission=exp(-sigmaT*path);
 float3 bottom=_BedColor.rgb;
 float hasBottom=step(rayDepth,bedDepth/max(.25,v.y));
 float3 captured=tex2Dproj(_RiverBackground,UNITY_PROJ_COORD(i.grab)).rgb;
 bottom=lerp(bottom,captured,hasBottom);
 float3 lightDir=normalize(float3(-.4,.82,-.32));
 float wrapped=saturate((dot(n,lightDir)+.5)/1.5);
 float forward=pow(saturate(dot(v,-lightDir)*.5+.5),3);
 float illumination=.72+wrapped*.38+forward*.4;
 // Beer-Lambert extinction + integrated, single-scattering source term.
 float3 scatter=(_Scattering.rgb/sigmaT)*(1-transmission)*float3(.68,.92,.83)*illumination;
 float3 base=bottom*transmission+scatter;
 float fresnel=pow(1-saturate(dot(n,v)),4);
 float3 reflection=tex2D(_ReflectionTex,saturate(screen+slope*.025)).rgb;
 float3 color=lerp(base,reflection*.85+base*.15,_HasReflection*(.16+fresnel*.4));
 color+=(sin(a)*.5+sin(b)*.3)*.006;
 float3 halfDir=normalize(v+normalize(float3(-.4,.82,-.32)));
 float spec=pow(saturate(dot(n,halfDir)),180);
 color=lerp(color,float3(.86,.94,.79),smoothstep(.35,.65,spec)*.48);
 // Sparse, short glints: never a continuous line or a full-surface foam pattern.
 float2 cell=floor(p*.65),q=frac(p*.65)-.5;float seed=hash(cell);
 float life=pow(saturate(sin(t*(.65+seed*.3)+seed*50)),14);
 float glint=(1-smoothstep(.025,.05,abs(q.y)))*(1-smoothstep(.06,.16,abs(q.x)));
 color=lerp(color,_FoamColor.rgb,glint*life*step(.88,seed)*.55*(1-far));
 // Sparse moving foam islands in open water. One cell evaluation, no particle objects.
 float2 foamGrid=(p+float2(t*.055,t*.025))/3.8;
 float2 fc=floor(foamGrid),fq=frac(foamGrid)-.5;
 float fs=hash(fc+17.3);
 fq-=float2(hash(fc+3.7)-.5,hash(fc+9.1)-.5)*.22;
 float rotation=fs*31;float cr=cos(rotation),sr=sin(rotation);
 float2 ellipse=float2(fq.x*cr-fq.y*sr,fq.x*sr+fq.y*cr)*float2(1,1.25+hash(fc+4.1)*1.1);
 float angle=atan2(ellipse.y,ellipse.x);
 float radius=.11+hash(fc+5.9)*.055+.019*sin(angle*3+fs*19)+.012*sin(angle*5-fs*12+t*.4);
 float dist=length(ellipse);
 float edgeAA=max(fwidth(dist),.003);
 float island=1-smoothstep(radius-edgeAA,radius+edgeAA,dist);
 float hole=(1-smoothstep(.035,.065,length(ellipse-float2(.055,-.025))))*step(.82,fs);
 float bubbles=(1-smoothstep(.017,.029,length(ellipse-float2(.22,.07))))
             +(1-smoothstep(.012,.024,length(ellipse-float2(-.19,-.09))));
 float surfaceFoam=saturate(island*(1-hole)+bubbles)*step(.76,fs);
 surfaceFoam*=smoothstep(.55,1.4,shore)*(1-smoothstep(28,65,i.eye))*(1-smoothstep(24,32,max(abs(p.x),abs(p.y))));
 color=lerp(color,_FoamColor.rgb,surfaceFoam*.78);
 // Rounded scallops and detached foam ribbons restricted to the shore.
 float wobble=sin(p.x*3.1+p.y*1.3+t*.8)*.07+sin(p.y*4.7-p.x*.9-t*.6)*.045;
 float contact=min(shore,gap*.8);
 float edge=_FoamWidth+wobble;
 float aa=max(fwidth(contact),.012);
 float foam=1-smoothstep(edge-aa,edge+aa,contact);
 float holes=smoothstep(.76,.90,sin(p.x*7.2+t*.35)*sin(p.y*6.4-t*.24));
 foam*=1-holes*smoothstep(.08,.3,contact)*.85;
 float outer=_FoamWidth+.35+.14*sin(t*.85+p.x*.5+p.y*.3)+wobble;
 float ribbon=1-smoothstep(.035,.065,abs(shore-outer));
 float breaks=smoothstep(-.1,.25,sin(p.x*2.1+p.y*1.8-t*.65));
 float mintHalo=(1-smoothstep(.4,1.6,shore))*.23;
 color=lerp(color,float3(.37,.73,.62),mintHalo);
 color=lerp(color,_FoamColor.rgb,max(foam*.94,ribbon*breaks*.78));
 return fixed4(color*(1-_Darkness),1);
 }
 ENDCG }
 }
}
