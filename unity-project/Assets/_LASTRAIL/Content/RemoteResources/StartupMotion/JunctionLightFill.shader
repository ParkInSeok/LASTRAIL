Shader "LastRail/JunctionLightFill" {
 Properties { _MainTex("Texture",2D)="white"{} _Progress("Progress",Float)=0 _Aspect("Aspect",Float)=.5625 }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" }
 Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 float _Progress,_Aspect;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {
  float d=length((i.uv-.5)*float2(_Aspect,1));
  float p=smoothstep(.12,1,_Progress);
  float radius=lerp(.006,1.25,p*p);
  float core=1-smoothstep(radius*.25,radius,d);
  float halo=exp(-d*d/max(.00001,radius*radius))*p*.45;
  float cover=smoothstep(.86,1,_Progress);
  float alpha=max(cover,saturate(core+halo)*smoothstep(0,.25,_Progress));
  float3 color=lerp(float3(.12,.55,.92),float3(.76,.94,1),saturate(core*.75+cover));
  return float4(color,alpha*i.color.a);
 }
 ENDCG
 }
 }
}