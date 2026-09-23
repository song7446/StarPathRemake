Shader "StarPath/ChapterBlur"
{
    Properties
    {
        _PixelSize ("Blur Strength", Range(1, 64)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ChapterBlur"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                half4 original = SAMPLE_TEXTURE2D_X(
                    _BlitTexture, sampler_LinearClamp, uv
                );

                // 기존 코드와 호환: 1이면 효과 없음
                float strength = max(0.0, _PixelSize - 1.0);

                if (strength <= 0.001)
                    return original;

                float radius = strength * 0.5;
                float2 texelSize = _BlitTexture_TexelSize.xy;

                float4 result = original;
                float totalWeight = 1.0;

                // 원형으로 분산된 위치를 샘플링
                // 네모 블록 경계 없이 주변 색을 부드럽게 섞는다.
                [unroll]
                for (int i = 0; i < 32; i++)
                {
                    float t = (i + 0.5) / 32.0;
                    float angle = i * 2.39996323;
                    float distance = sqrt(t);

                    float2 direction = float2(
                        cos(angle),
                        sin(angle)
                    );

                    float2 offset =
                        direction * distance * radius * texelSize;

                    float weight = exp(-2.0 * t);

                    result += SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv + offset
                    ) * weight;

                    totalWeight += weight;
                }

                return result / totalWeight;
            }
            ENDHLSL
        }
    }
}