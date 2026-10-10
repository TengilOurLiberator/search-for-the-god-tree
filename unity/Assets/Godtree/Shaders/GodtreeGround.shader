// The meadow floor. The painted grass texture is sampled twice at different sizes and turns
// and mixed by a slow noise, so the eye cannot find the repeat; broad patches of lighter and
// darker grass sit on top; and a worn dirt road runs straight ahead along world Z.
// Lit like Godtree/Toon: one main light with shadows, tinted shade. Works in single-pass
// instanced VR.
Shader "Godtree/Ground"
{
    Properties
    {
        _BaseMap ("Grass", 2D) = "white" {}
        _BaseColor ("Grass Tint", Color) = (0.92, 0.98, 0.90, 1)
        _Tile ("Metres Per Tile", Range(0.5, 20)) = 3.75
        _PatchColor ("Dry Patch Tint", Color) = (1.08, 1.02, 0.78, 1)
        _RoadColor ("Road Color", Color) = (0.66, 0.53, 0.36, 1)
        _RoadWidth ("Road Half Width", Range(0, 5)) = 1.3
        _RoadStart ("Road Starts At Z", Float) = 4
        _ShadowColor ("Shadow Tint", Color) = (0.60, 0.58, 0.84, 1)
        _Brightness ("Brightness", Range(0.5, 2)) = 1.08
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Tile;
            half4 _PatchColor;
            half4 _RoadColor;
            half _RoadWidth;
            float _RoadStart;
            half4 _ShadowColor;
            half _Brightness;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Smooth value noise, 0 to 1.
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 ground = input.positionWS.xz;
                float2 uvA = ground / _Tile;
                // Second layer: larger, turned about 37 degrees and shifted, so repeats never line up.
                float2 uvB = float2(ground.x * 0.799 - ground.y * 0.602, ground.x * 0.602 + ground.y * 0.799) / (_Tile * 2.37) + 0.37;
                half3 grassA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA).rgb;
                half3 grassB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB).rgb;
                half mixAB = smoothstep(0.3, 0.7, Noise(ground * 0.23));
                // Far away the fine layer shimmers into a pattern, so lean on the large one there.
                half far = saturate(distance(ground, _WorldSpaceCameraPos.xz) / 40.0);
                half3 grass = lerp(grassA, grassB, saturate(mixAB + far * 0.6)) * _BaseColor.rgb;

                // Broad sun-dried patches.
                half patch = smoothstep(0.52, 0.78, Noise(ground * 0.06 + 11.0) * 0.7 + Noise(ground * 0.19) * 0.3);
                grass *= lerp(half3(1, 1, 1), _PatchColor.rgb, patch * 0.6);

                // The road: a band along Z with a ragged, trampled edge and pebbly shading.
                half wobble = (Noise(float2(ground.y * 0.35, 3.0)) - 0.5) * 1.2;
                half edge = (Noise(ground * 1.7) - 0.5) * 0.7 + (Noise(ground * 6.0) - 0.5) * 0.2;
                half across = abs(ground.x - wobble) + edge;
                half road = (1.0 - smoothstep(_RoadWidth * 0.55, _RoadWidth, across)) * smoothstep(_RoadStart, _RoadStart + 3.0, ground.y + edge * 2.0);
                half3 dirt = _RoadColor.rgb * (0.86 + 0.22 * Noise(ground * 3.1) + 0.10 * Noise(ground * 17.0));
                half3 albedo = lerp(grass, dirt, road * 0.92);

                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half lit = saturate(dot(normal, mainLight.direction) * 4.0) * mainLight.shadowAttenuation;
                half3 shade = lerp(_ShadowColor.rgb, mainLight.color.rgb, lit);
                half3 color = albedo * shade * _Brightness;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings depthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 depthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
