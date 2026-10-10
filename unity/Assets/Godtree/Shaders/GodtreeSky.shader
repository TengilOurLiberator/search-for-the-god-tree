// The sky: a soft gradient from a pale horizon to a deep blue overhead, a small bright sun,
// and slow puffy clouds with a warm lit side and a cool shaded side, drawn from layered noise
// so no image is needed. Works in single-pass instanced VR.
Shader "Godtree/Sky"
{
    Properties
    {
        _ZenithColor ("Overhead", Color) = (0.20, 0.42, 0.86, 1)
        _HorizonColor ("Horizon", Color) = (0.72, 0.86, 0.98, 1)
        _GroundColor ("Below Horizon", Color) = (0.70, 0.83, 0.96, 1)
        _CloudLight ("Cloud Lit Side", Color) = (1.0, 0.98, 0.93, 1)
        _CloudShade ("Cloud Shaded Side", Color) = (0.66, 0.72, 0.90, 1)
        _Coverage ("Cloud Cover", Range(0, 1)) = 0.46
        _CloudScale ("Cloud Size", Range(0.2, 4)) = 1.1
        _Drift ("Drift Speed", Range(0, 0.1)) = 0.006
        _SunColor ("Sun", Color) = (1.0, 0.96, 0.82, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _CloudLight;
                half4 _CloudShade;
                half _Coverage;
                half _CloudScale;
                half _Drift;
                half4 _SunColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            // Layered noise: big shapes with smaller lumps on top.
            float Clouds(float2 p)
            {
                float sum = 0.0, weight = 0.5;
                for (int octave = 0; octave < 5; octave++)
                {
                    sum += Noise(p) * weight;
                    p = p * 2.03 + 17.0;
                    weight *= 0.5;
                }
                return sum;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float3 sunDir = normalize(_MainLightPosition.xyz);

                half up = saturate(dir.y);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(up, 0.55));
                sky = lerp(sky, _GroundColor.rgb, saturate(-dir.y * 8.0));

                // Sun: a small disc and a wide soft glow.
                half toSun = saturate(dot(dir, sunDir));
                sky += _SunColor.rgb * (smoothstep(0.9993, 0.9997, toSun) * 2.0 + pow(toSun, 60.0) * 0.25);

                if (dir.y > 0.0)
                {
                    // Flatten the dome onto a sheet high above, so clouds bunch toward the horizon.
                    float2 sheet = dir.xz / (dir.y + 0.18) * _CloudScale + _Time.y * _Drift * float2(1.0, 0.35);
                    float density = Clouds(sheet);
                    // The same sheet sampled a step toward the sun: thinner there means this side is lit.
                    float towardSun = Clouds(sheet + normalize(sunDir.xz + 0.0001) * 0.18);
                    half cover = smoothstep(1.0 - _Coverage, 1.0 - _Coverage + 0.10, density);
                    // Two soft bands of tone rather than a smooth blend, as painted clouds have.
                    half lit = smoothstep(-0.02, 0.05, density - towardSun);
                    half core = smoothstep(1.0 - _Coverage + 0.10, 1.0 - _Coverage + 0.28, density);
                    half3 cloud = lerp(_CloudShade.rgb, _CloudLight.rgb, saturate(lit * 0.75 + (1.0 - core) * 0.45));
                    // Fade out toward the horizon, where the haze takes over.
                    half fade = smoothstep(0.03, 0.22, dir.y);
                    sky = lerp(sky, cloud, cover * fade * 0.96);
                }

                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
}
