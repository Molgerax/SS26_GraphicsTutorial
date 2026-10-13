Shader "Fungal/FungalTexture"
{
    Properties
    {
    }
	
    SubShader 
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline" 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry" 
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FungusData.hlsl"
            #include "HSV.hlsl"
            
			struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0; // UVs come from TEXCOORD0
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            
            
            // Declare a texture like this
            TEXTURE2D(_MainTex);
            
            SAMPLER(sampler_MainTex);
            
            Texture2D<uint> _TextureWrite;
            uint _Resolution;
            
            
            // Every texture also assigns automatically the float4 <MyTextureName>_ST
            // It holds the Scale and Translation from the material inspector
            // We can use that to scale the UVs later for more control per material
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END
            
            
            
            
			Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                // Here we scale the UVs by the scaling factor and add the offset factor
                OUT.uv = IN.uv;
                
			    return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
				float4 color = 0;
				color.a = 1;
                
                uint3 index = uint3(IN.uv * _Resolution, 0);
                uint cell = _TextureWrite.Load(index);
                
                float cellMass = GetFungalMass(cell);
                float attractant = GetChemoAttractant(cell);
                
                float mass01 = cellMass;
                float attractant01 = attractant;
                
                float3 hsv = 0;
                hsv.z = mass01;
                hsv.y = attractant01;
                
                color.rgb = hsv2rgb(hsv);
                
                color.g = mass01;
                color.b = attractant01;
                color.r = 0;
                
                if (!IsCellAvailable(cell))
                    color.rgb = float3(1, 1, 0.5);
                
                return color;
			}
			
			ENDHLSL
        }
    }
}