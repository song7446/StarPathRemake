Shader "StarPath/UI/GlitchTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _Progress ("Progress", Range(0,1)) = 0
        _BandCount ("Band Count", Float) = 65
        _NoiseFPS ("Noise FPS", Float) = 18
        _NoiseBrightness ("Noise Brightness", Range(0,1)) = 0.35
        _NoiseTime ("Noise Time", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
            };

            fixed4 _Color;
            float4 _ClipRect;

            float _Progress;
            float _BandCount;
            float _NoiseFPS;
            float _NoiseBrightness;
            float _NoiseTime;

            float Hash(float2 value)
            {
                return frac(
                    sin(dot(value, float2(127.1, 311.7)))
                    * 43758.5453
                );
            }

            v2f vert(appdata input)
            {
                v2f output;

                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float progress = saturate(_Progress);

                float frame = floor(_NoiseTime * max(1, _NoiseFPS));
                float band = floor(input.uv.y * max(1, _BandCount));

                // 가로 띠마다 다른 타이밍으로 화면을 가린다.
                float bandNoise = Hash(float2(band, frame));

                // 가로로 긴 조각을 만들어 띠가 끊기는 느낌을 준다.
                float cellX = floor(input.uv.x * 24);
                float blockNoise = Hash(
                    float2(cellX + band * 17, frame)
                );

                float threshold =
                    bandNoise * 0.85 + blockNoise * 0.15;

                float coverage = smoothstep(
                    threshold - 0.035,
                    threshold + 0.035,
                    progress
                );

                // 전환 중에만 회색 잡음이 보인다.
                float activity = 4 * progress * (1 - progress);

                float grain = Hash(float2(
                    floor(input.uv.x * 700) + band * 31,
                    frame
                ));

                float brightBand = step(0.8, bandNoise);

                float brightness =
                    (grain * 0.2 + brightBand * 0.8)
                    * _NoiseBrightness
                    * activity;

                // 양 끝에서는 잡음이나 반투명 조각이 남지 않는다.
                if (progress <= 0)
                {
                    coverage = 0;
                    brightness = 0;
                }
                else if (progress >= 1)
                {
                    coverage = 1;
                    brightness = 0;
                }

                fixed4 result = fixed4(
                    brightness,
                    brightness,
                    brightness,
                    coverage
                );

                result *= input.color;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(
                    input.localPosition.xy,
                    _ClipRect
                );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }

            ENDCG
        }
    }
}