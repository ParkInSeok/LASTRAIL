Shader "LastRail/SeraCrystalOcclusion" {
 Properties {
  [PerRendererData] _MainTex("Crystal",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)
  _ActorTex("Actor alpha",2D)="black"{} _TargetTex("Target alpha",2D)="black"{}
  _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0 _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15 [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 texcoord:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;float2 local:TEXCOORD2;};
   sampler2D _MainTex,_ActorTex,_TargetTex;
   fixed4 _Color,_TextureSampleAdd;float4 _ClipRect;
   float4x4 _ActorInverse,_TargetInverse;
   float4 _ActorRect,_TargetRect,_ActorUv,_TargetUv;
   float _ActorEnabled,_TargetEnabled,_ActorAlpha,_TargetAlpha,_Occlusion;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex);o.local=v.vertex.xy;o.uv=v.texcoord;o.color=v.color*_Color;return o;}
   float maskAlpha(sampler2D tex,float4 world,float4x4 inverse,float4 rect,float4 uv,float enabled,float opacity){
    if(enabled<.5)return 0;float2 p=(mul(inverse,world).xy-rect.xy)/rect.zw;
    if(any(p<0)||any(p>1))return 0;return tex2D(tex,uv.xy+p*uv.zw).a*opacity;
   }
   fixed4 frag(v2f i):SV_Target {
    fixed4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
    c.a*=1-_Occlusion*maskAlpha(_ActorTex,i.world,_ActorInverse,_ActorRect,_ActorUv,_ActorEnabled,_ActorAlpha);
    c.a*=1-_Occlusion*maskAlpha(_TargetTex,i.world,_TargetInverse,_TargetRect,_TargetUv,_TargetEnabled,_TargetAlpha);
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.local,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
