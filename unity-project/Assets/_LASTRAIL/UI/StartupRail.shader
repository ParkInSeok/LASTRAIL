Shader "LASTRAIL/StartupRail"
{
    Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; float4 uv:TEXCOORD0; float4 extra:TEXCOORD1; };
            struct Output { float4 vertex:SV_POSITION; float4 color:COLOR; float3 uv:TEXCOORD0; float2 mode:TEXCOORD1; };
            sampler2D _MainTex;
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=float3(v.uv.xy,v.extra.x); o.mode=v.extra.yz; return o; }
            fixed4 frag(Output i):SV_Target {
                float2 uv=i.uv.xy/i.uv.z;
                float across=lerp(saturate(uv.x),1-saturate(uv.x),i.mode.y);
                float groundCross=abs(frac(across*16)*2-1);
                // The texture's outer gravel margin is opaque and has a verified length loop.
                // Exclude the metal rails and sleepers from the surrounding wasteland.
                float2 sampled=float2(lerp(across,.01+groundCross*.17,i.mode.x),frac(uv.y));
                fixed4 c=tex2D(_MainTex,sampled)*i.color;
                if(i.mode.x>.5) { c.rgb=lerp(float3(.21,.22,.215),c.rgb*float3(.8,.74,.66),saturate((i.uv.z-1)*.3)); c.a=1; }
                else c.a*=saturate(min(uv.x,1-uv.x)*24);
                return c;
            }
            ENDCG
        }
    }
}
