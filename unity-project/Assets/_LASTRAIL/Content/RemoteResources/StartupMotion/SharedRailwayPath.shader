Shader "LastRail/UI/SharedRailwayPath" {
 Properties {[PerRendererData] _MainTex("Mask",2D)="white" {}}
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 v vert(a x){v o;o.vertex=UnityObjectToClipPos(x.vertex);o.uv=x.uv;o.color=x.color;return o;}
 float segment(float2 p,float2 a,float2 b){a.y*=.754;b.y*=.754;float2 ab=b-a;return length(p-a-ab*saturate(dot(p-a,ab)/dot(ab,ab)));}
 fixed4 frag(v i):SV_Target {
 float2 p=float2(i.uv.x,(1-i.uv.y)*.754);float d=1;
 float2 pts[15]={float2(.064,.330),float2(.15,.350),float2(.25,.360),float2(.34,.378),float2(.40,.407),float2(.44,.46),float2(.48,.502),float2(.51,.520),float2(.59,.478),float2(.66,.451),float2(.72,.435),float2(.78,.386),float2(.84,.329),float2(.89,.301),float2(.94,.284)};
 [unroll] for(int k=0;k<14;k++) d=min(d,segment(p,pts[k],pts[k+1]));
 float core=1-smoothstep(.00055,.00125,d);float glow=exp(-d*d/.000023)*.24;
 return fixed4(lerp(i.color.rgb,float3(.78,.96,1),core),saturate(core+glow)*i.color.a);
 }
 ENDCG }
 }}