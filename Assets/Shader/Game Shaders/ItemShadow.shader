/*
Round contact shadow under dropped items (ItemShadowRenderer), drawn for every visible item in a single procedural draw.

Every instance is a box hanging below an item, built here from the item position in _ItemShadowPivots (no mesh).
The box faces are only used to find the pixels to shade: each pixel reads the scene depth, rebuilds the opaque surface behind it
and darkens it if that surface is inside the box and faces up, so the shadow wraps around block edges, stairs and objects.
Inner faces are drawn without depth test, so every pixel is shaded exactly once and the shadow still works with the camera inside a box.

The top of the box is below the lowest point a spinning item reaches, so items never shadow themselves.
It is drawn in the pre-refraction queue: Ice is drawn over it, and refractive liquids show it underwater
*/
Shader "Draconic/ItemShadow"{
	Properties{
		_ShadowStrength("Strength", Range(0, 1)) = 0.5
		_ShadowInner("Inner Radius", Range(0, 1)) = 0.35
		_ShadowMinRadiusScale("Radius Scale at Max Depth", Range(0.1, 1)) = 0.6
		_ShadowFadeDistance("Fade Distance", Float) = 48
		// Set by ItemShadowRenderer
		[HideInInspector] _ItemShadowBox("Box", Vector) = (0.55, 0.2, 3, 0.9)
	}

	SubShader{
		Tags{"RenderPipeline"="HDRenderPipeline" "Queue"="Transparent-260" "RenderType"="Transparent" "IgnoreProjector"="True"}

		Pass{
			Name "ForwardOnly"
			Tags{"LightMode"="ForwardOnly"}

			Cull Front
			ZWrite Off
			ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma target 4.5
			#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

			// Unity positions of the items (xyz)
			StructuredBuffer<float4> _ItemShadowPivots;

			CBUFFER_START(UnityPerMaterial)
				// x: diameter, y: depth of the box top below the item, z: depth of the box bottom below the item, w: height resting items float above the floor
				float4 _ItemShadowBox;
				float _ShadowStrength;
				float _ShadowInner;
				float _ShadowMinRadiusScale;
				float _ShadowFadeDistance;
			CBUFFER_END

			static const float3 CUBE_CORNERS[8] = {
				float3(-0.5, -0.5, -0.5), float3(0.5, -0.5, -0.5), float3(-0.5, 0.5, -0.5), float3(0.5, 0.5, -0.5),
				float3(-0.5, -0.5, 0.5), float3(0.5, -0.5, 0.5), float3(-0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5)
			};

			// Clockwise seen from outside, like Unity meshes, so Cull Front keeps the inner faces
			static const uint CUBE_TRIANGLES[36] = {
				0, 2, 3, 0, 3, 1, // -Z
				5, 7, 6, 5, 6, 4, // +Z
				4, 6, 2, 4, 2, 0, // -X
				1, 3, 7, 1, 7, 5, // +X
				2, 6, 7, 2, 7, 3, // +Y
				4, 0, 1, 4, 1, 5  // -Y
			};

			struct Varyings{
				float4 positionCS : SV_POSITION;
				nointerpolation float3 pivotRWS : TEXCOORD0;
			};

			Varyings Vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID){
				Varyings output;

				float3 pivotRWS = GetCameraRelativePositionWS(_ItemShadowPivots[instanceID].xyz);
				float3 center = pivotRWS - float3(0.0, (_ItemShadowBox.y + _ItemShadowBox.z) * 0.5, 0.0);
				float3 size = float3(_ItemShadowBox.x, _ItemShadowBox.z - _ItemShadowBox.y, _ItemShadowBox.x);

				output.positionCS = TransformWorldToHClip(center + CUBE_CORNERS[CUBE_TRIANGLES[vertexID]] * size);
				output.pivotRWS = pivotRWS;

				// Collapses the box, so shadows past the fade distance are never rasterized
				if(length(pivotRWS) > _ShadowFadeDistance)
					output.positionCS = float4(0.0, 0.0, 0.0, 1.0);

				return output;
			}

			// Camera relative position of the opaque surface seen at a pixel
			float3 SurfacePosition(int2 pixel){
				pixel = clamp(pixel, int2(0, 0), int2(_ScreenSize.xy) - 1);

				float depth = LoadCameraDepth(uint2(pixel));
				float2 uv = (pixel + 0.5) * _ScreenSize.zw;

				return ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
			}

			// Takes the neighbour on the same surface (the closest one), so normals don't bleed over block silhouettes
			float3 SurfaceDelta(float3 center, float3 next, float3 previous){
				float3 forward = next - center;
				float3 backward = center - previous;
				float forwardLength = dot(forward, forward);
				float backwardLength = dot(backward, backward);

				// Samples clamped at the screen border return the center itself
				if(forwardLength == 0.0)
					return backward;
				if(backwardLength == 0.0)
					return forward;

				return forwardLength < backwardLength ? forward : backward;
			}

			float SurfaceUp(int2 pixel, float3 surface){
				float3 dx = SurfaceDelta(surface, SurfacePosition(pixel + int2(1, 0)), SurfacePosition(pixel - int2(1, 0)));
				float3 dy = SurfaceDelta(surface, SurfacePosition(pixel + int2(0, 1)), SurfacePosition(pixel - int2(0, 1)));
				float3 normal = SafeNormalize(cross(dy, dx));

				// Visible surfaces face the camera, which is at the origin of camera relative space
				if(dot(normal, surface) > 0.0)
					normal = -normal;

				return normal.y;
			}

			float4 Frag(Varyings input) : SV_Target{
				int2 pixel = int2(input.positionCS.xy);
				float3 surface = SurfacePosition(pixel);
				float3 offset = surface - input.pivotRWS;
				float radius = _ItemShadowBox.x * 0.5;
				float below = -offset.y;

				if(below < _ItemShadowBox.y || below > _ItemShadowBox.z || abs(offset.x) > radius || abs(offset.z) > radius)
					discard;

				// 0 down to the resting floor height, 1 at the bottom of the box. The shadow shrinks and fades as the item rises
				float rise = saturate((below - _ItemShadowBox.w) / max(_ItemShadowBox.z - _ItemShadowBox.w, 0.001));
				float distanceToCenter = length(offset.xz) / (radius * lerp(1.0, _ShadowMinRadiusScale, rise));

				float alpha = _ShadowStrength * (1.0 - smoothstep(_ShadowInner, 1.0, distanceToCenter));
				alpha *= (1.0 - rise) * (1.0 - rise);
				alpha *= 1.0 - smoothstep(_ShadowFadeDistance * 0.75, _ShadowFadeDistance, length(surface));

				if(alpha <= 0.0)
					discard;

				alpha *= smoothstep(0.5, 0.8, SurfaceUp(pixel, surface));

				return float4(0.0, 0.0, 0.0, alpha);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
