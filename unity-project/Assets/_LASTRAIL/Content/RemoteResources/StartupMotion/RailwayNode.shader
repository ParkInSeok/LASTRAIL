Shader "LastRail/UI/RailwayNode" {
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 v vert(a x){v o;o.vertex=UnityObjectToClipPos(x.vertex);o.uv=x.uv;o.color=x.color;return o;}
 fixed4 frag(v i):SV_Target {float d=length(i.uv-.5);float core=exp(-d*d*1800);float halo=exp(-d*d*50)*.14;return fixed4(lerp(i.color.rgb,float3(.85,.97,1),core),saturate(core+halo)*i.color.a);}
 ENDCG }
 }}