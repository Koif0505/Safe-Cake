Shader "Custom/ReflecitiveSmart" {

    Properties{

        _Color("Color", Color) = (1,1,1,1)
        _MainTex("MainTex (RGBA) Day", 2D) = "white" {}
        _MaskReflection("Mask reflection", 2D) = "white" {}
        _ColorMask("Color Mask", Color) = (1,1,1,1)
        _RefflectColor("Refflect Color", Color) = (1,1,1,1)
        _Cube("Reflection Cubemap", CUBE) = "white" {}
        _BumpMap("Normalmap", 2D) = "bump" {}
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Glossiness("Smoothness", Range(0,1)) = 0.5

        _UseTextureForNight("Use Texture for Night", Range(0,1)) = 0


    }
        SubShader{

            Tags {"Queue" = "AlphaTest" }
            LOD 200
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows keepalpha
            #pragma target 3.0

            sampler2D _MainTex;
            sampler2D _MaskReflection;
            sampler2D _BumpMap;
            samplerCUBE _Cube;

            struct Input {
                float2 uv_MainTex;
                float3 worldRefl;
                INTERNAL_DATA
            };

            
            half _Glossiness;
            half _Metallic;

            
            fixed4 _RefflectColor;
            fixed4 _Color;
            fixed4 _ColorMask;
            fixed4 _ReflectColor;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            void surf(Input IN, inout SurfaceOutputStandard o) {

                fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

                fixed3 mask = tex2D(_MaskReflection, IN.uv_MainTex).rgb;

                o.Albedo = (c.rgb * _Color) + (texCUBE(_Cube, WorldReflectionVector(IN, o.Normal)).rgb * mask * (1 + (1 - c.a))  * _RefflectColor);
                
                o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));

                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;

                o.Alpha = c.a; 

            }
            ENDCG
        }

        Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"

}
