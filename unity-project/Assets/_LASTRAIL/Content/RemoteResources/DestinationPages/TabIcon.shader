Shader "LastRail/TabIcon"
{
    Properties { [PerRendererData] _MainTex("Icon atlas",2D)="white"{} }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            sampler2D _MainTex;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float4 c=tex2D(_MainTex,i.uv);
                // Warm metal and white silhouettes are bright in every channel;
                // blue baked glow has little red and must not survive as a square.
                float value=min(c.r,min(c.g,c.b));
                #ifdef UNITY_COLORSPACE_GAMMA
                float mask=smoothstep(.14,.28,value);
                #else
                float mask=smoothstep(.015,.065,value);
                #endif
                return float4(i.color.rgb,i.color.a*c.a*mask);
            }
            ENDCG
        }
    }
}

