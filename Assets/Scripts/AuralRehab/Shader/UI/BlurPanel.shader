Shader "AuralRehab/UI/BlurPanel"
{
    Properties{
        _Color("Tint (A=Opacity)", Color) = (1,1,1,0.6)
        _BlurSize("Blur Size", Float) = 1.2
    }
    SubShader{
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        GrabPass{ "_GrabTex" }

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 grabPos:TEXCOORD0; };

            sampler2D _GrabTex;
            float4 _GrabTex_TexelSize;
            float4 _Color;
            float _BlurSize;

            v2f vert(appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i):SV_Target{
                float2 texel = _GrabTex_TexelSize.xy * _BlurSize;
                fixed4 c =
                    tex2Dproj(_GrabTex, i.grabPos + float4(-texel.x, -texel.y,0,0)) +
                    tex2Dproj(_GrabTex, i.grabPos + float4( texel.x, -texel.y,0,0)) +
                    tex2Dproj(_GrabTex, i.grabPos + float4(-texel.x,  texel.y,0,0)) +
                    tex2Dproj(_GrabTex, i.grabPos + float4( texel.x,  texel.y,0,0));
                c *= 0.25;
                // 살짝 어둡게
                c = lerp(c, fixed4(0,0,0,1), 0.2);
                c.a = _Color.a;          // 패널 투명도는 여기서 제어
                return c;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}