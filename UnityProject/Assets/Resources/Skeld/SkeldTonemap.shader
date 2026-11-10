// THREE.ACESFilmicToneMapping (toneMappingExposure = 1.15) — port ตรงจาก three.js (Stephen Hill fit)
// ใช้เป็น post-process ท้ายสุดบนกล้อง (ดู ToneMapper.cs)
Shader "Hidden/Skeld/Tonemap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 1.15
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Exposure;

            static const float3x3 ACESInputMat = float3x3(
                0.59719, 0.35458, 0.04823,
                0.07600, 0.90834, 0.01566,
                0.02840, 0.13383, 0.83777);

            static const float3x3 ACESOutputMat = float3x3(
                 1.60475, -0.53108, -0.07367,
                -0.10208,  1.10813, -0.00605,
                -0.00327, -0.07276,  1.07602);

            float3 RRTAndODTFit(float3 v)
            {
                float3 a = v * (v + 0.0245786) - 0.000090537;
                float3 b = v * (0.983729 * v + 0.4329510) + 0.238081;
                return a / b;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float3 col = c.rgb * (_Exposure / 0.6);
                col = mul(ACESInputMat, col);
                col = RRTAndODTFit(col);
                col = mul(ACESOutputMat, col);
                return fixed4(saturate(col), c.a);
            }
            ENDCG
        }
    }
}
