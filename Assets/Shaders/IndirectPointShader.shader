// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Indirect/IndirectPointShader"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Emission ("Emissive strength", float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members dist)
#pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float dist : SCALAR;
            };

            float4 _Color;
            float _Emission;
            float4 _PlayerPosition;
            StructuredBuffer<float3> _InstancePositions;

            v2f vert(appdata v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                uint instance_id = GetIndirectInstanceID(svInstanceID);

                v2f o;
                float3 instance_pos = _InstancePositions[instance_id];
                o.vertex = UnityObjectToClipPos(v.vertex.xyz + instance_pos);
                UNITY_TRANSFER_FOG(o, o.vertex);

                o.dist = distance(_PlayerPosition.xz, instance_pos.xz);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // float4 col = _Color * (_Emission + clamp(3 / i.dist, 0, 3));
                float4 col = lerp(float4(1, 0, 0, 1), float4(0, 0, 1, 1), 5 / i.dist)
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}