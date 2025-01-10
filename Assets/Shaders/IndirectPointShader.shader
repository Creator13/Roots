Shader "Indirect/IndirectPointShader"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 1, 1)
        _CloseColor ("Close color", Color) = (1, 0, 0, 1)
        _Falloff ("Falloff distance", Float) = 1
        _Emission ("Emissive strength", Float) = 1
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
            float4 _CloseColor;
            float _Falloff;
            float _Emission;
            float4 _PlayerPosition;
            StructuredBuffer<float3> _InstancePositions;

            v2f vert(appdata v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                uint instance_id = GetIndirectInstanceID(svInstanceID);

                v2f o;
                float3 instance_pos = _InstancePositions[instance_id];
                float3 distance_vec = _PlayerPosition - instance_pos;
                o.dist = length(distance_vec.xz);
                    
                o.vertex = UnityObjectToClipPos(v.vertex.xyz + instance_pos);
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = lerp(_Color, _CloseColor, saturate(_Falloff / i.dist));
                col *= _Emission + clamp(_Falloff / i.dist, 0, _Falloff);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}