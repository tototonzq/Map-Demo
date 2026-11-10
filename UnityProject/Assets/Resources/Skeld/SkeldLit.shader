// เทียบเท่า THREE.MeshStandardMaterial + หมอกจากตัวละคร (Built-in Render Pipeline, Forward)
Shader "Skeld/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.4
        _Metallic ("Metallic", Range(0,1)) = 0.2
        [HDR] _Emission ("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow finalcolor:skeldFinal
        #pragma target 3.0
        #include "SkeldFog.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        fixed4 _Emission;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 worldPos;
        };

        void skeldFinal(Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            color.rgb *= SkeldFogFactor(IN.worldPos);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = _Emission.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
