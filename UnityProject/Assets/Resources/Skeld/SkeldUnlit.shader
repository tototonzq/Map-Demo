// เทียบเท่า THREE.MeshBasicMaterial / PointsMaterial (ไม่รับแสง) + หมอกแบบเลือกเปิด/ปิดได้
// ตั้งค่า blend / cull / zwrite ผ่าน property เพื่อให้ใช้ shader เดียวได้ทั้งของทึบและของโปร่งใส
Shader "Skeld/Unlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _Cull ("Cull", Float) = 2
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _UseFog ("Use Fog", Float) = 1
        [HideInInspector] _PointSize ("Point Size", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Cull [_Cull]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "SkeldFog.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _UseFog;
            float _PointSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 wpos : TEXCOORD1;
                fixed4 color : COLOR;
                float psize : PSIZE;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color;
                o.psize = _PointSize;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color * i.color;
                if (_UseFog > 0.5) c.rgb *= SkeldFogFactor(i.wpos);
                return c;
            }
            ENDCG
        }
    }
}
