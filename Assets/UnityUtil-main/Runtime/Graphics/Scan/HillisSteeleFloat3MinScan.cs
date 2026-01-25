using UnityEngine;
using UnityEngine.Profiling;

namespace Voxell.Graphics
{
  using Mathx;

  public sealed class HillisSteeleFloat3MinScan : AbstractScan
  {
    private static ComputeShader cs_hillisSteeleFloat3MinScan;
    private static int kn_hillisSteeleFloat3MinScan;

    private readonly int _dataSize;
    private readonly int _gridSize;

    private ComputeBuffer cb_prev;

    public HillisSteeleFloat3MinScan(int dataSize)
    {
      _dataSize = dataSize;
      _gridSize = MathUtil.CalculateGrids(_dataSize, Graphics.M_BLOCK_SZ);
      cb_prev = new ComputeBuffer(dataSize, StrideSize.s_float3);
    }

    public static void InitKernels()
    {
      if (cs_hillisSteeleFloat3MinScan != null) return;
      cs_hillisSteeleFloat3MinScan = Resources.Load<ComputeShader>("Scan/HillisSteeleFloat3MinScan");
      kn_hillisSteeleFloat3MinScan = cs_hillisSteeleFloat3MinScan.FindKernel("HillisSteeleFloat3MinScan");
    }

    public void Scan(ref ComputeBuffer cb_in)
    {
      Profiler.BeginSample("HillisSteeleFloat3MinScan");
      cs_hillisSteeleFloat3MinScan.SetInt(PropertyID.len, _dataSize);
      cs_hillisSteeleFloat3MinScan.SetBuffer(kn_hillisSteeleFloat3MinScan, BufferID.cb_in, cb_in);
      cs_hillisSteeleFloat3MinScan.SetBuffer(kn_hillisSteeleFloat3MinScan, BufferID.cb_prev, cb_prev);

      for (int offset=1; offset < _dataSize; offset <<= 1)
      {
        ComputeShaderUtil.CopyBufferFloat3(ref cb_in, ref cb_prev, _dataSize);
        cs_hillisSteeleFloat3MinScan.SetInt(PropertyID.offset, offset);
        cs_hillisSteeleFloat3MinScan.Dispatch(kn_hillisSteeleFloat3MinScan, _gridSize, 1, 1);
      }
      Profiler.EndSample();
    }

    public override void Dispose()
    {
      cb_prev.Dispose();
    }
  }
}