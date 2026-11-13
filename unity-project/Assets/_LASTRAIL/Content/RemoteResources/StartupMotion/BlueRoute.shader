Shader "LastRail/UI/BlueRoute" {
 Properties { [PerRendererData] _MainTex("Mask",2D)="white" {} }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex; float4 _MainTex_TexelSize;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {
 float core=tex2D(_MainTex,i.uv).a;
 float glow=0;
 [unroll] for(int x=-2;x<=2;x++) { [unroll] for(int y=-2;y<=2;y++) {
 glow+=tex2D(_MainTex,i.uv+float2(x,y)*_MainTex_TexelSize.xy*3).a/25;
 }}
 return fixed4(lerp(i.color.rgb,float3(.72,.95,1),core),saturate(core+glow*1.8)*i.color.a);
 }
 ENDCG }
 }}