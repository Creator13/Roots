Shader "Custom/LitInstanced"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                uint instanceID : SV_InstanceID;
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                uint instanceID : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Cutoff;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            StructuredBuffer<float4x4> _Instances;

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Fetch per-instance data from the structured buffer
                float4x4 instanceMatrix = _Instances[input.instanceID];
                float3 positionWS = mul(instanceMatrix, float4(input.positionOS.xyz, 1.0)).xyz;

                // Transform the vertex position
                output.positionHCS = TransformObjectToHClip(float4(positionWS, 1.0));

                // UV transformation
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // Calculate normal in world space for lighting
                float3x3 normalMatrix = (float3x3)instanceMatrix;
                output.normalWS = normalize(mul(normalMatrix, input.normalOS));

                output.instanceID = input.instanceID;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample the texture
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Clip based on alpha cutoff for transparent objects
                clip(texColor.a - _Cutoff);

                // Prepare inputs for lighting function
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirectionWS = normalize(GetWorldSpaceViewDir(input.positionHCS.xyz));

                // Calculate lighting
                Light mainLight = GetMainLight();
                half3 lightColor = mainLight.color;
                float3 lightDirWS = normalize(mainLight.direction);

                // Diffuse lighting
                float diffuse = max(0, dot(normalWS, lightDirWS));
                half3 diffuseColor = lightColor * diffuse;

                // Simple environment lighting approximation
                half3 ambientColor = half3(0.1, 0.1, 0.1);
                half3 color = texColor.rgb * (_Metallic + ambientColor + diffuseColor);

                return half4(color, texColor.a);
            }
            ENDHLSL
        }
    }
}
