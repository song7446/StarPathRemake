Shader "StarPath/UIStarGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Glow Color", Color) = (0.7, 0.85, 1, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1
        _Falloff ("Falloff", Range(0.5, 5)) = 1.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        // 배경에 빛을 더한다.
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
            };

            float4 _Color;
            float _Intensity;
            float _Falloff;
            float4 _ClipRect;

            v2f vert(appdata input)
            {
                v2f output;

                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;

                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                // 중심은 0, 원의 가장자리는 1.
                float distanceFromCenter =
                    length((input.uv - 0.5) * 2.0);

                // 가장자리에서 부드럽게 사라지는 원형 빛.
                float glow = 1.0 - smoothstep(
                    0.0, 1.0, distanceFromCenter);

                glow = pow(saturate(glow), _Falloff);

                float4 color = input.color;
                color.rgb *= _Intensity;
                color.a *= glow;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(
                    input.localPosition.xy,
                    _ClipRect
                );
                #endif

                return color;
            }

            ENDCG
        }
    }
}