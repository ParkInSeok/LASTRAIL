Shader "LastRail/LobbyCabinMotion"
{
    Properties { _MainTex("Cabin",2D)="white"{} _Landscape("Landscape",2D)="white"{} _Clock("Clock",Float)=0 _Motion("Motion",Float)=1 _Interference("Hologram Interference",Float)=0 _InterferenceCenter("Interference Center",Vector)=(.529,.44,0,0) _InterferenceEnd("Road End",Vector)=(.529,.44,0,0) _InterferenceRoad("Road Mode",Float)=0 }
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
            sampler2D _MainTex,_Landscape;float _Clock,_Motion,_Interference;float4 _InterferenceCenter,_InterferenceEnd;float _InterferenceRoad;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float3 scenery(float2 uv,float speed)
            {
                // Mirror repeat prevents a hard wrap seam even when terrain edges differ.
                float x=1-abs(frac((uv.x*.43+_Clock*speed)*.5)*2-1);
                return tex2D(_Landscape,float2(x,1-uv.y)).rgb;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=float2(i.uv.x,1-i.uv.y);
                float lamp=exp(-dot((p-float2(.855,.37))/float2(.052,.061),(p-float2(.855,.37))/float2(.052,.061))*2);
                float2 sampleUv=i.uv;
                sampleUv.x+=lamp*sin(_Clock*.77-.35)*.00065*_Motion;
                float4 c=tex2D(_MainTex,sampleUv);
                float map=smoothstep(.35,.39,p.y)*(1-smoothstep(.59,.63,p.y));
                float blue=saturate((c.b-c.r)*4)*saturate((c.g-c.r)*3)*map;
                float scan=pow(saturate(sin(p.y*100-_Clock*.65)),16)*.025;
                c.rgb*=1+blue*(.025*sin(_Clock*1.3)+scan)*_Motion;
                // Disturb only the cyan emissive residual, never displace parchment/cabin UVs.
                float tick=floor(_Clock*48);
                float band=floor(p.y*155);
                float noise=frac(sin(band*12.9898+tick*78.233)*43758.5453);
                float2 delta=(p-_InterferenceCenter.xy)*float2(1,.563);
                float2 segment=(_InterferenceEnd.xy-_InterferenceCenter.xy)*float2(1,.563);
                float projection=saturate(dot(delta,segment)/max(.000001,dot(segment,segment)));
                float radius=length(delta-segment*projection*_InterferenceRoad);
                float localMask=1-smoothstep(lerp(.045,.008,_InterferenceRoad),lerp(.15,.038,_InterferenceRoad),radius);
                float tear=step(.38,noise)*_Interference*localMask;
                float2 shiftedUv=sampleUv+float2((noise-.5)*.018*tear,0);
                float3 shifted=tex2D(_MainTex,shiftedUv).rgb;
                float3 emission=float3(0,max(c.g-c.r,0),max(c.b-c.r,0))*map;
                float3 displaced=float3(0,max(shifted.g-shifted.r,0),max(shifted.b-shifted.r,0))*map;
                float node=1-smoothstep(.018,.07,radius);
                float dropout=_Interference*localMask*(.45*step(.55,noise)+.45*node+.25*_InterferenceRoad);
                c.rgb=c.rgb-emission+lerp(emission,displaced,tear*.8)*(1-dropout);
                return c*i.color;
            }
            ENDCG
        }
    }
}
