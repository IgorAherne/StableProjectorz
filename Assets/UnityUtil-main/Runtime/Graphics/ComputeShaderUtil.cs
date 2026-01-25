using UnityEngine;

namespace Voxell.Graphics
{
  using Mathx;

  public static class ComputeShaderUtil
  {
    public static ComputeShader cs_uint;
    public static ComputeShader cs_float3;

    private static int kn_CopyBuffer, kn_ZeroOut, kn_SetBufferAsThreadIdx;
    private static int kn_CopyBufferFloat3;

    /// <summary>
    /// copy a buffer to another buffer
    /// </summary>
    /// <param name="cb_in">source buffer</param>
    /// <param name="cb_out">destination buffer</param>
    /// <param name="dataSize">buffer size</param>
    public static void CopyBuffer(ref ComputeBuffer cb_in, ref ComputeBuffer cb_out, int dataSize)
    {
      int gridSize = MathUtil.CalculateGrids(dataSize, Graphics.L_BLOCK_SZ);
      cs_uint.SetInt(PropertyID.dataSize, dataSize);
      cs_uint.SetBuffer(kn_CopyBuffer, BufferID.cb_in, cb_in);
      cs_uint.SetBuffer(kn_CopyBuffer, BufferID.cb_out, cb_out);
      cs_uint.Dispatch(kn_CopyBuffer, gridSize, 1, 1);
    }

    public static void ZeroOut(ref ComputeBuffer cb_out, int dataSize)
    {
      int gridSize = MathUtil.CalculateGrids(dataSize, Graphics.L_BLOCK_SZ);
      cs_uint.SetInt(PropertyID.dataSize, dataSize);
      cs_uint.SetBuffer(kn_ZeroOut, BufferID.cb_out, cb_out);
      cs_uint.Dispatch(kn_ZeroOut, gridSize, 1, 1);
    }

    public static void SetBufferAsThreadIdx(ref ComputeBuffer cb_out, int dataSize)
    {
      int gridSize = MathUtil.CalculateGrids(dataSize, Graphics.L_BLOCK_SZ);
      cs_uint.SetInt(PropertyID.dataSize, dataSize);
      cs_uint.SetBuffer(kn_SetBufferAsThreadIdx, BufferID.cb_out, cb_out);
      cs_uint.Dispatch(kn_SetBufferAsThreadIdx, gridSize, 1, 1);
    }

    public static void CopyBufferFloat3(ref ComputeBuffer cb_in, ref ComputeBuffer cb_out, int dataSize)
    {
      int gridSize = MathUtil.CalculateGrids(dataSize, Graphics.L_BLOCK_SZ);
      cs_float3.SetInt(PropertyID.dataSize, dataSize);
      cs_float3.SetBuffer(kn_CopyBufferFloat3, BufferID.cb_in, cb_in);
      cs_float3.SetBuffer(kn_CopyBufferFloat3, BufferID.cb_out, cb_out);
      cs_float3.Dispatch(kn_CopyBufferFloat3, gridSize, 1, 1);
    }

    public static void InitKernels()
    {
      if (cs_uint != null) return;
      cs_uint = Resources.Load<ComputeShader>("Util/Util.UInt");
      cs_float3 = Resources.Load<ComputeShader>("Util/Util.Float3");

      kn_CopyBuffer = cs_uint.FindKernel("CopyBuffer");
      kn_ZeroOut = cs_uint.FindKernel("ZeroOut");
      kn_SetBufferAsThreadIdx = cs_uint.FindKernel("SetBufferAsThreadIdx");

      kn_CopyBufferFloat3 = cs_float3.FindKernel("CopyBufferFloat3");
    }

    private static class PropertyID
    {
      public static readonly int dataSize = Shader.PropertyToID("_dataSize");
    }

    private static class BufferID
    {
      public static readonly int cb_in = Shader.PropertyToID("cb_in");
      public static readonly int cb_out = Shader.PropertyToID("cb_out");
    }
  }
}