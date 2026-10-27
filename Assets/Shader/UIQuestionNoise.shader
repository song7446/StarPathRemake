Shader "StarPath/UI/QuestionNoise"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.45
        _Distortion ("Horizontal Distortion", Range(0, 0.05)) = 0.008
        _BandWidth ("Band Width", Range(0.01, 0.5)) = 0.18
        _TravelDuration ("Travel Duration", Float) = 1.2
        _PauseDuration ("Pause Duration", Float) = 2
        _NoiseFPS ("Noise Change Speed", Float) = 18

        // Unity UI Mask 지원
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
            "CanUseSpriteAtlas" = "False"
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
            Name "QuestionNoise"

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _NoiseStrength;
            float _Distortion;
            float _BandWidth;
            float _TravelDuration;
            float _PauseDuration;
            float _NoiseFPS;

            float RandomValue(float2 seed)
            {
                return frac(
                    sin(dot(seed, float2(127.1, 311.7))) * 43758.5453
                );
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float travel = max(0.01, _TravelDuration);
                float cycle = travel + max(0.0, _PauseDuration);
                float cycleTime = fmod(_Time.y, cycle);

                float progress = saturate(cycleTime / travel);
                float active = 1.0 - step(travel, cycleTime);

                // 아래쪽 바깥에서 시작해 위쪽 바깥으로 이동
                float bandCenter = lerp(
                    -_BandWidth,
                    1.0 + _BandWidth,
                    progress
                );

                float distanceToBand = abs(i.uv.y - bandCenter);

                float band = 1.0 - smoothstep(
                    _BandWidth * 0.2,
                    _BandWidth,
                    distanceToBand
                );

                band *= active;

                // 매 프레임 부드럽게 움직이는 대신
                // 일정 간격으로 패턴을 바꿔 지직거리는 느낌 생성
                float tick = floor(_Time.y * max(1.0, _NoiseFPS));
                float row = floor(i.uv.y * 150.0);

                float rowNoise = RandomValue(float2(row, tick));

                float2 distortedUV = i.uv;
                distortedUV.x +=
                    (rowNoise * 2.0 - 1.0) * _Distortion * band;

                fixed4 color =
                    tex2D(_MainTex, saturate(distortedUV))
                    + _TextureSampleAdd;

                // 길쭉한 가로 노이즈 조각
                float2 noiseCell = floor(
                    i.uv * float2(65.0, 230.0)
                );

                float grain = RandomValue(
                    noiseCell + float2(tick, tick * 0.37)
                );

                float noise =
                    (grain * 2.0 - 1.0) * 0.6
                    + (rowNoise * 2.0 - 1.0) * 0.4;

                color.rgb = saturate(
                    color.rgb + noise * _NoiseStrength * band
                );

                color *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(
                    i.localPosition.xy,
                    _ClipRect
                );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }

            ENDCG
        }
    }
}