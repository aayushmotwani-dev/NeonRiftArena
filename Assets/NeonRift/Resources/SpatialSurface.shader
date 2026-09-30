Shader "SpatialRift/Surface"
{
    Properties
    {
        _MainTex ("Surface Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _Metallic ("Metallic", Range(0,1)) = 0.35
        _Smoothness ("Smoothness", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Tint;
        fixed4 _EmissionColor;
        half _Metallic;
        half _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 surface = tex2D(_MainTex, IN.uv_MainTex) * _Tint;
            o.Albedo = surface.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
