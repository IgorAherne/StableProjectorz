using UnityEngine;
using Unity.Mathematics;
using System.Runtime.InteropServices;

namespace Voxell.Graphics
{
  public static class StrideSize
  {
    // ints
    public static readonly int s_int = Marshal.SizeOf(typeof(int));
    public static readonly int s_int2 = Marshal.SizeOf(typeof(int2));
    public static readonly int s_int3 = Marshal.SizeOf(typeof(int3));
    public static readonly int s_int4 = Marshal.SizeOf(typeof(int4));

    public static readonly int s_int2x2 = Marshal.SizeOf(typeof(int2x2));
    public static readonly int s_int2x3 = Marshal.SizeOf(typeof(int2x3));
    public static readonly int s_int2x4 = Marshal.SizeOf(typeof(int2x4));

    public static readonly int s_int3x2 = Marshal.SizeOf(typeof(int3x2));
    public static readonly int s_int3x3 = Marshal.SizeOf(typeof(int3x3));
    public static readonly int s_int3x4 = Marshal.SizeOf(typeof(int3x4));

    public static readonly int s_int4x2 = Marshal.SizeOf(typeof(int4x2));
    public static readonly int s_int4x3 = Marshal.SizeOf(typeof(int4x3));
    public static readonly int s_int4x4 = Marshal.SizeOf(typeof(int4x4));

    // uints
    public static readonly int s_uint = Marshal.SizeOf(typeof(uint));
    public static readonly int s_uint2 = Marshal.SizeOf(typeof(uint2));
    public static readonly int s_uint3 = Marshal.SizeOf(typeof(uint3));
    public static readonly int s_uint4 = Marshal.SizeOf(typeof(uint4));

    public static readonly int s_uint2x2 = Marshal.SizeOf(typeof(uint2x2));
    public static readonly int s_uint2x3 = Marshal.SizeOf(typeof(uint2x3));
    public static readonly int s_uint2x4 = Marshal.SizeOf(typeof(uint2x4));

    public static readonly int s_uint3x2 = Marshal.SizeOf(typeof(uint3x2));
    public static readonly int s_uint3x3 = Marshal.SizeOf(typeof(uint3x3));
    public static readonly int s_uint3x4 = Marshal.SizeOf(typeof(uint3x4));

    public static readonly int s_uint4x2 = Marshal.SizeOf(typeof(uint4x2));
    public static readonly int s_uint4x3 = Marshal.SizeOf(typeof(uint4x3));
    public static readonly int s_uint4x4 = Marshal.SizeOf(typeof(uint4x4));

    // floats
    public static readonly int s_float = Marshal.SizeOf(typeof(float));
    public static readonly int s_float2 =  Marshal.SizeOf(typeof(float2));
    public static readonly int s_float3 =  Marshal.SizeOf(typeof(float3));
    public static readonly int s_float4 =  Marshal.SizeOf(typeof(float4));

    public static readonly int s_float2x2 = Marshal.SizeOf(typeof(float2x2));
    public static readonly int s_float2x3 = Marshal.SizeOf(typeof(float2x3));
    public static readonly int s_float2x4 = Marshal.SizeOf(typeof(float2x4));

    public static readonly int s_float3x2 = Marshal.SizeOf(typeof(float3x2));
    public static readonly int s_float3x3 = Marshal.SizeOf(typeof(float3x3));
    public static readonly int s_float3x4 = Marshal.SizeOf(typeof(float3x4));

    public static readonly int s_float4x2 = Marshal.SizeOf(typeof(float4x2));
    public static readonly int s_float4x3 = Marshal.SizeOf(typeof(float4x3));
    public static readonly int s_float4x4 = Marshal.SizeOf(typeof(float4x4));

    public static readonly int s_boneWeight = Marshal.SizeOf(typeof(BoneWeight));
  }
}