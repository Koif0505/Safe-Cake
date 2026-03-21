Shader "Custom/SpecularNormalContrastShader"
{
    Properties
    {
        _Color("Specular Color", Color) = (1,1,1,1)     // Cor do specular
        _MainTex("Texture", 2D) = "white" {}                // Textura principal
        _NormalMap("Normal Map", 2D) = "bump" {}            // Mapa de normais
        _SpecuColor("Specular Color", Color) = (1,1,1,1)     // Cor do specular
        _Glossiness("Smoothness", Range(0, 1)) = 0.5        // Controle de brilho (Glossiness)
        _Contrast("Contrast", Range(0, 2)) = 1.0            // Controle de contraste
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            CGPROGRAM
            // Define o modelo de superfície com Specular
            #pragma surface surf StandardSpecular normalmap

            sampler2D _MainTex;     // Textura principal
            sampler2D _NormalMap;   // Mapa de normais
            fixed4 _Color;           // Cor da textura
            fixed4 _SpecuColor;      // Cor do specular
            half _Glossiness;       // Controle de brilho (Glossiness)
            float _Contrast;        // Valor de contraste

            struct Input
            {
                float2 uv_MainTex;     // Coordenadas da textura principal
                float2 uv_NormalMap;   // Coordenadas da textura do normal map
            };

            // Função de cálculo de superfície
            void surf(Input IN, inout SurfaceOutputStandardSpecular o)
            {
                // Amostra a textura principal
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

                // Aplica a fórmula de contraste
                c.rgb = ((c.rgb * _Color.rgb) - 0.5) * _Contrast + 0.5;

                // Define a cor do Albedo (cor difusa)
                o.Albedo = c.rgb;

                // Aplica o normal map
                o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));

                // Define a cor do Specular e o valor de Smoothness
                o.Specular = _SpecuColor.rgb; // Especifica a cor do Specular
                o.Smoothness = _Glossiness;  // Controle de brilho
            }
            ENDCG
        }
            FallBack "Specular"
}
