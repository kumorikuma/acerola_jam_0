Shader "Custom/DeliveryLocationShader" {
    Properties {
        _LineColor ("Line Color", Color) = (1, 1, 1, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _LineWidth ("Line Width", Range(0, 0.1)) = 0.005
    }

    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float3 barycentric : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _LineWidth;
            fixed4 _LineColor;
            fixed4 _BackgroundColor;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.barycentric = float3(0, 0, 0);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2f input[3], inout TriangleStream<v2f> output) {
                v2f v0 = input[0];
                v2f v1 = input[1];
                v2f v2 = input[2];

                v0.barycentric = float3(1, 0, 0);
                v1.barycentric = float3(0, 1, 0);
                v2.barycentric = float3(0, 0, 1);

                output.Append(v0);
                output.Append(v1);
                output.Append(v2);
            }

            fixed4 frag(v2f i) : SV_Target {
                // Calculate the distance to the nearest edge
                float minDist = min(min(i.barycentric.x, i.barycentric.y), i.barycentric.z);

                // Render the wireframe based on the distance to the nearest edge
                float edge = smoothstep(_LineWidth, 2 * _LineWidth, minDist);
                return lerp(_LineColor, _BackgroundColor, edge);
            }
            ENDCG
        }
    }
}