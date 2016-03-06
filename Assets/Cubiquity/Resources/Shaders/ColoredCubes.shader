Shader "ColoredCubes"
{	
	Properties
	{
		_DiffuseMap ("Diffuse map", 2D) = "white" {}
		_NormalMap ("Normal map", 2D) = "bump" {}
		_NoiseStrength ("Noise strength", Range (0.0,0.5)) = 0.1
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert addshadow
		#pragma target 3.0
		#pragma glsl
		
		float4x4 _World2Volume;
		
		struct Input
		{
			float4 color : COLOR;
			float4 volumePos;
		};
	
		sampler2D _DiffuseMap;
		sampler2D _NormalMap;
		float _NoiseStrength;
		
		#include "ColoredCubesUtilities.cginc"
	
	
		void vert (inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input,o);
			
			// Unity can't cope with the idea that we're peforming lighting without having per-vertex
			// normals. We specify dummy ones here to avoid having to use up vertex buffer space for them.
			v.normal = float3 (0.0f, 0.0f, 1.0f);
			v.tangent = float4 (1.0f, 0.0f, 0.0f, 1.0f);     
			
			// Volume-space position is use for adding noise.
			float4 worldPos = mul(_Object2World, v.vertex);
			o.volumePos =  mul(_World2Volume, worldPos);
		}
	
		void surf (Input IN, inout SurfaceOutput o)
		{
			// Compute the normal vector using derivative instructions. This save us space compared to passing
			// normals with the mesh data, and also means we don't need to duplicate vertices on cube corners.
			// This trick only works because we are using flat-shading not smooth shading.
			float3 volumeNormal = normalize(cross(ddx(IN.volumePos.xyz), ddy(IN.volumePos.xyz)));

			// Our derivative trick above can easily end up with normal vectors pointing in the opposite direction
			// to what we want. This is because the exact behaviour seems to depend on a nuber of factors including
			// render system, operating system, Unity version, and play-mode vs, edit-mode.
			//
			// To fix this we flip normal vectors if they point away from the eye. This results on incorrect
			// normals on back-facing triangles, but we can't see those anyway.
			float4 volumeSpaceCameraPos = mul(_World2Volume, float4(_WorldSpaceCameraPos, 1.0));
			float3 volumeToCamera = normalize(volumeSpaceCameraPos.xyz - IN.volumePos.xyz);
			if(dot(volumeToCamera, volumeNormal) < 0.0) // If the normal faces away from the camera
			{
				volumeNormal *= -1.0; // Flip it
			}

			
			// This fixes inaccuracies/rounding errors which can otherwise occur
			volumeNormal = floor(volumeNormal + float3(0.5, 0.5, 0.5));	
			
			// Because we know our normal is pointing along one of the three main axes we can trivially compute a tangent space.
			float3 volumeTangent = volumeNormal.yzx;
			float3 volumeBinormal = volumeNormal.zxy;
			
			// And from our tangent space we can now compute texture coordinates.
			float2 texCoords = float2(dot(IN.volumePos.xyz, volumeTangent), dot(IN.volumePos.xyz, volumeBinormal));
			texCoords = texCoords - float2(0.5, 0.5);  // Required because integer positions are at the center of the voxel.
			
			// Get the normal from the normal map (we no longer need the normal we calculated earlier).
			float3 normalFromNormalMap = UnpackNormal(tex2D(_NormalMap, texCoords));
			
			// Move the normal into the correct space. I do wonder whether some of this is unnecessary, and actually reduces to something trivial...
			float3x3 volumeToTangentMatrix = float3x3(
			volumeTangent.x, volumeBinormal.x, volumeNormal.x, 
			volumeTangent.y, volumeBinormal.y, volumeNormal.y, 
			volumeTangent.z, volumeBinormal.z, volumeNormal.z);
			normalFromNormalMap = mul(volumeToTangentMatrix, normalFromNormalMap);
			
			// Add noise - we use volume space to prevent noise scrolling if the volume moves.
			float noise = positionBasedNoise(float4(IN.volumePos.xyz - (volumeNormal * 0.1), _NoiseStrength));
			
			// Sample the other texture maps
			float3 diffuseVal = tex2D(_DiffuseMap, texCoords);
			
			// Pass the various values to Unity.
			o.Albedo = (IN.color.xyz + float3(noise, noise, noise)) * diffuseVal;   
			o.Alpha = 1.0;     
			o.Normal = normalFromNormalMap;
		}				
		ENDCG	
	}
}
