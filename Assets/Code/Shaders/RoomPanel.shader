Shader "Shikacat/RoomPanel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Floor", 2D) = "white" {}
        _WallTex ("Wall", 2D) = "clear" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _InteriorCutoff ("Interior Alpha Cutoff", Range(0.01, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        sampler2D _WallTex;
        fixed4 _Color;
        float _InteriorCutoff;

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
            fixed4 color : COLOR;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            o.color = v.color * _Color;
            return o;
        }
        ENDCG

        Pass
        {
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 floorCol = tex2D(_MainTex, i.uv) * i.color;
                fixed4 wallCol = tex2D(_WallTex, i.uv);

                fixed3 rgb = wallCol.rgb * wallCol.a + floorCol.rgb * floorCol.a * (1 - wallCol.a);
                fixed a = wallCol.a + floorCol.a * (1 - wallCol.a);
                return fixed4(rgb, a);
            }
            ENDCG
        }

        Pass
        {
            ColorMask 0
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a - 0.01);
                clip(_InteriorCutoff - tex2D(_WallTex, i.uv).a);
                return 0;
            }
            ENDCG
        }
    }
}
