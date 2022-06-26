// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Gaia/CombineColorChannels"
{
    Properties
    {
        // we have removed support for texture tiling/offset,
        // so make them not be displayed in material inspector
        [NoScaleOffset] _RedChannelTex ("Texture", 2D) = "black" {}
        [NoScaleOffset] _GreenChannelTex ("Texture", 2D) = "black" {}
        [NoScaleOffset] _BlueChannelTex ("Texture", 2D) = "black" {}
        [NoScaleOffset] _AlphaChannelTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            // use "vert" function as the vertex shader
            #pragma vertex vert
            // use "frag" function as the __pixel__ (fragment) shader
            #pragma fragment frag

            // vertex shader inputs
            struct appdata
            {
                float4 vertex : POSITION; // vertex position
                float2 uv : TEXCOORD0; // texture coordinate
            };

            // vertex shader outputs ("vertex to fragment")
            struct v2f
            {
                float2 uv : TEXCOORD0; // texture coordinate
                float4 vertex : SV_POSITION; // clip space position
            };

            // vertex shader
            v2f vert (appdata v)
            {
                v2f o;
                // transform position to clip space
                // (multiply with model*view*projection matrix)
                o.vertex = UnityObjectToClipPos(v.vertex);
                // just pass the texture coordinate
                o.uv = v.uv;
                return o;
            }
            
            // textures we will sample
            sampler2D _RedChannelTex;
            sampler2D _GreenChannelTex;
            sampler2D _BlueChannelTex;
            sampler2D _AlphaChannelTex;

            // pixel shader; returns low precision ("fixed4" type)
            // color ("SV_Target" semantic)
            fixed4 frag (v2f i) : SV_Target
            {
                // sample texture and return it
                fixed4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                col.r = tex2D(_RedChannelTex, i.uv);
                col.g = tex2D(_GreenChannelTex, i.uv);
                col.b = tex2D(_BlueChannelTex, i.uv);
                col.a = tex2D(_AlphaChannelTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}