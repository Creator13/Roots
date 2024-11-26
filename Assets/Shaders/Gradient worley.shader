Shader "Custom/VoronoiWithDensity" {
    Properties {
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _PointCount ("Point Count", Float) = 10
        _TileScale ("Tile Scale", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _NoiseTexture;
            float _PointCount;
            float _TileScale;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 hash(float2 p) {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)),
                                       dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            float voronoi(float2 uv, float2 cellOffset, sampler2D noiseTex, float tileScale, float pointCount) {
                uv *= tileScale;

                float2 cellPos = floor(uv) + cellOffset;
                float2 cellOrigin = cellPos + hash(cellPos);
                
                // Modify cell position based on noise texture
                float density = tex2D(noiseTex, frac(cellPos / tileScale)).r; 
                cellOrigin += (hash(cellPos) - 0.5) * density;

                return distance(uv, cellOrigin);
            }

            float3 voronoiColor(float2 uv, sampler2D noiseTex, float tileScale, float pointCount) {
                float2 coord = uv * pointCount;
                float minDist = 1.0;
                float color = float(0.0);

                for (int y = -1; y <= 1; ++y) {
                    for (int x = -1; x <= 1; ++x) {
                        float2 offset = float2(x, y);
                        float dist = voronoi(uv, offset, noiseTex, tileScale, pointCount);

                        if (dist < minDist) {
                            minDist = dist;
                            color = frac(offset * 1.2345); // Use offset for color variation
                        }
                    }
                }

                return color;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 color = voronoiColor(i.uv, _NoiseTexture, _TileScale, _PointCount);
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
