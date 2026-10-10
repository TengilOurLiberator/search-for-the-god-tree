// A far-away painted card (the Godtree on the horizon). Unlit, alpha-blended, never fogged by
// the scene's fog; instead it carries its own haze, so it reads as immensely distant.
// Works in single-pass instanced VR.
Shader "Godtree/Backdrop"
{
    Properties
    {
        _BaseMap ("Image", 2D) = "white" {}
        _HazeColor ("Haze Color", Color) = (0.70, 0.83, 0.96, 1)
        _Haze ("Haze Amount", Range(0, 1)) = 0.45
        _Opacity ("Opacity", Range(0, 1)) = 0.92
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-100" }

        Pass
        {
            Name "Backdrop"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _HazeColor;
                half _Haze;
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 image = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                // More haze toward the base, where there is more air between us and the tree.
                half haze = saturate(_Haze + (1.0 - input.uv.y) * 0.25);
                half3 color = lerp(image.rgb, _HazeColor.rgb, haze);
                return half4(color, image.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
