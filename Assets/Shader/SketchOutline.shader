Shader "StarPath/SketchOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0.65,0.6,0.5,0.8)
        _Width ("Width (Texture Pixels)", Range(0,40)) = 2
        _Wobble ("Wobble (Texture Pixels)", Range(0,30)) = 0.8
        _FPS ("Changes Per Second", Range(1,20)) = 6
        _NoiseSize ("Noise Size (Texture Pixels)", Range(4,100)) = 24
        _Threshold ("Alpha Threshold", Range(0.01,0.99)) = 0.5

        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="False"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _Width, _Wobble, _FPS, _NoiseSize, _Threshold;

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7)))
                            * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
                    lerp(Hash(cell + float2(0,1)),
                         Hash(cell + float2(1,1)), f.x),
                    f.y);
            }

            float Mask(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                    return 0.0;

                float a = SampleSpriteTexture(uv).a;
                return smoothstep(_Threshold - 0.04,
                                  _Threshold + 0.04, a);
            }

            fixed4 Frag(v2f IN) : SV_Target
            {
                // 원본 그림은 움직이지 않는다.
                fixed4 source = SampleSpriteTexture(IN.texcoord);
                float alpha = source.a * IN.color.a;
                float3 rgb = source.rgb * IN.color.rgb * alpha;

                // 시간을 끊어서 손그림처럼 변화시킨다.
                float frame = floor(_Time.y * _FPS);
                float2 pixels = IN.texcoord * _MainTex_TexelSize.zw;
                float2 p = pixels / _NoiseSize;
                float2 seed = float2(frame * 13.7, frame * 7.9);

                float2 jitter = float2(
                    Noise(p + seed),
                    Noise(p + seed + float2(43.1, 17.3))
                ) * 2.0 - 1.0;

                float2 uv = IN.texcoord
                          + jitter * _Wobble * _MainTex_TexelSize.xy;

                // 주변 8방향을 검사하여 실루엣을 확장한다.
                float expanded = Mask(uv);

                [unroll]
                for (int k = 0; k < 8; k++)
                {
                    float angle = k * 0.785398163;
                    float2 direction = float2(cos(angle), sin(angle));
                    float2 offset = direction * _Width
                                  * _MainTex_TexelSize.xy;

                    expanded = max(expanded, Mask(uv + offset));
                }

                float ring = saturate(expanded - Mask(uv));
                ring *= step(0.001, _Width);

                // 외곽선은 원본 그림 뒤에 합성한다.
                float outlineAlpha = ring * _OutlineColor.a * IN.color.a;
                rgb += _OutlineColor.rgb * outlineAlpha * (1.0 - alpha);
                alpha += outlineAlpha * (1.0 - alpha);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}