// shader that performs ray casting using a 3D texture
// adapted from a Cg example by Nvidia
// http://developer.download.nvidia.com/SDK/10/opengl/samples.html
// Gilles Ferrand, University of Manitoba / RIKEN, 2016–2017

Shader "Custom/Ray Casting" {
	
	Properties {
		// the data cube
		[NoScaleOffset] _Data ("Data Texture", 3D) = "" {}
		_DataChannel ("Data Channel", Vector) = (0,0,0,1) // in which channel were the data value stored?
		_Axis ("Axes order", Vector) = (1, 2, 3) // coordinate i=0,1,2 in Unity corresponds to coordinate _Axis[i]-1 in the data
		_TexFilling ("Data filling factors", Vector) = (1, 1, 1) // if only a fraction of the data texture is to be sampled
		// data slicing and thresholding (X, Y, Z are user coordinates)
		_SliceAxis1Min ("Slice along axis X: min", Range(0,1)) = 0
		_SliceAxis1Max ("Slice along axis X: max", Range(0,1)) = 1
		_SliceAxis2Min ("Slice along axis Y: min", Range(0,1)) = 0
		_SliceAxis2Max ("Slice along axis Y: max", Range(0,1)) = 1
		_SliceAxis3Min ("Slice along axis Z: min", Range(0,1)) = 0
		_SliceAxis3Max ("Slice along axis Z: max", Range(0,1)) = 1
		_DataMin ("Data threshold: min", Range(0,1)) = 0
		_DataMax ("Data threshold: max", Range(0,1)) = 1
		_StretchPower ("Data stretch power", Range(0.1,3)) = 1  // increase it to highlight the highest data values
		// normalization of data intensity (has to be adjusted for each data set)
		_NormPerStep ("Intensity normalization per step", Float) = 1
		_NormPerRay  ("Intensity normalization per ray" , Float) = 1
		_Steps ("Max number of steps", Range(1,1024)) = 128 // should ideally be as large as data resolution, strongly affects frame rate
	}

	SubShader {
		
		Tags { "Queue" = "Transparent" }

		Pass {
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			ZTest LEqual
			ZWrite Off
			Fog { Mode off }

			CGPROGRAM
	        #pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler3D _Data;
			float4 _DataChannel;
			float3 _Axis;
			float3 _TexFilling;
			float _SliceAxis1Min, _SliceAxis1Max;
			float _SliceAxis2Min, _SliceAxis2Max;
			float _SliceAxis3Min, _SliceAxis3Max;
			float _DataMin, _DataMax;
			float _StretchPower;
			float _NormPerStep;
			float _NormPerRay;
			float _Steps;

			// calculates intersection between a ray and a box
			// http://www.siggraph.org/education/materials/HyperGraph/raytrace/rtinter3.htm
			bool IntersectBox(float3 ray_o, float3 ray_d, float3 boxMin, float3 boxMax, out float tNear, out float tFar)
			{
			    // compute intersection of ray with all six bbox planes
			    float3 invR = 1.0 / ray_d;
			    float3 tBot = invR * (boxMin.xyz - ray_o);
			    float3 tTop = invR * (boxMax.xyz - ray_o);
			    // re-order intersections to find smallest and largest on each axis
			    float3 tMin = min (tTop, tBot);
			    float3 tMax = max (tTop, tBot);
			    // find the largest tMin and the smallest tMax
			    float2 t0 = max (tMin.xx, tMin.yz);
			    float largest_tMin = max (t0.x, t0.y);
			    t0 = min (tMax.xx, tMax.yz);
			    float smallest_tMax = min (t0.x, t0.y);
			    // check for hit
			    bool hit = (largest_tMin <= smallest_tMax);
			    tNear = largest_tMin;
			    tFar = smallest_tMax;
			    return hit;
			}

			struct vert_input {
			    float4 pos : POSITION;
			};

			struct frag_input {
			    float4 pos : SV_POSITION;
			    float3 ray_o : TEXCOORD1; // ray origin
			    float3 ray_d : TEXCOORD2; // ray direction
			};

			// vertex program
			frag_input vert(vert_input i)
			{
				frag_input o;

			    // calculate eye ray in object space
				o.ray_d = -ObjSpaceViewDir(i.pos);
				o.ray_o = i.pos.xyz - o.ray_d;
				// calculate position on screen (unused)
				o.pos = UnityObjectToClipPos(i.pos);

				return o;
			}

			// gets data value at a given position
			float4 get_data(float3 pos) {
				// sample texture (pos is normalized in [0,1])
				float3 posTex = float3(pos[_Axis[0]-1],pos[_Axis[1]-1],pos[_Axis[2]-1]);
				posTex = (posTex-0.5) * _TexFilling + 0.5;
				float4 data4 = tex3Dlod(_Data, float4(posTex,0));
				float data = _DataChannel[0]*data4.r + _DataChannel[1]*data4.g + _DataChannel[2]*data4.b + _DataChannel[3]*data4.a;
				// slice and threshold
				data *= step(_SliceAxis1Min, posTex.x);
				data *= step(_SliceAxis2Min, posTex.y);
				data *= step(_SliceAxis3Min, posTex.z);
				data *= step(posTex.x, _SliceAxis1Max);
				data *= step(posTex.y, _SliceAxis2Max);
				data *= step(posTex.z, _SliceAxis3Max);
				data *= step(_DataMin, data);
				data *= step(data, _DataMax);
				// colourize
				float4 col = float4(data, data, data, data);
				return col;
			}

#define FRONT_TO_BACK // ray integration order (BACK_TO_FRONT not working when being inside the cube)
			
			// fragment program
			float4 frag(frag_input i) : COLOR
			{
			    i.ray_d = normalize(i.ray_d);
			    // calculate eye ray intersection with cube bounding box
				float3 boxMin = { -0.5, -0.5, -0.5 };
				float3 boxMax = {  0.5,  0.5,  0.5 };
			    float tNear, tFar;
			    bool hit = IntersectBox(i.ray_o, i.ray_d, boxMin, boxMax, tNear, tFar);
			    if (!hit) discard;
			    if (tNear < 0.0) tNear = 0.0;
			    // calculate intersection points
			    float3 pNear = i.ray_o + i.ray_d*tNear;
			    float3 pFar  = i.ray_o + i.ray_d*tFar;
			    // convert to texture space
				pNear = pNear + 0.5;
				pFar  = pFar  + 0.5;
				
			    // march along ray inside the cube, accumulating color
#ifdef FRONT_TO_BACK
				float3 ray_pos = pNear;
				float3 ray_dir = pFar - pNear;
#else
				float3 ray_pos = pFar;
				float3 ray_dir = pNear - pFar;
#endif
				float3 ray_step = normalize(ray_dir) * sqrt(3) / _Steps;
				float4 ray_col = 0;
				for(int k = 0; k < _Steps; k++)
				{
					float4 voxel_col = get_data(ray_pos);
					voxel_col.a = _NormPerStep * length(ray_step) * pow(voxel_col.a,_StretchPower);
#ifdef FRONT_TO_BACK
	        		//voxel_col.rgb *= voxel_col.a;
			        //ray_col = (1.0f - ray_col.a) * voxel_col + ray_col;
					ray_col.rgb = ray_col.rgb + (1 - ray_col.a) * voxel_col.a * voxel_col.rgb;
					ray_col.a   = ray_col.a   + (1 - ray_col.a) * voxel_col.a;
#else
			        ray_col.rgb = (1-voxel_col.a)*ray_col.rgb + voxel_col.a * voxel_col.rgb;
			        ray_col.a   = (1-voxel_col.a)*ray_col.a   + voxel_col.a;
#endif
					ray_pos += ray_step;
					if (ray_pos.x < 0 || ray_pos.y < 0 || ray_pos.z < 0) break;
					if (ray_pos.x > 1 || ray_pos.y > 1 || ray_pos.z > 1) break;
				}
				ray_col *= _NormPerRay;
				ray_col = clamp(ray_col,0,1);
		    	return ray_col;
			}

			ENDCG

		}

	}

	FallBack Off
}