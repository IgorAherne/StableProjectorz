using UnityEngine;
using UnityEngine.Profiling;

namespace Voxell.Graphics
{
  using Mathx;

  public sealed class HillisSteeleFloat3MaxScan : AbstractScan
  {
    private static ComputeShader cs_hillisSteeleFloat3MaxScan;
    private static int kn_hillisSteeleFloat3MaxScan;

    private readonly int _dataSize;
    private readonly int _gridSize;

    private ComputeBuffer cb_prev;

    public HillisSteeleFloat3MaxScan(int dataSize)
    {
      _dataSize = dataSize;
      _gridSize = MathUtil.CalculateGrids(_dataSize, Graphics.M_BLOCK_SZ);
      cb_prev = new ComputeBuffer(dataSize, StrideSize.s_float3);
    }

    public static void InitKernels()
    {
      if (cs_hillisSteeleFloat3MaxScan != null) return;
      cs_hillisSteeleFloat3MaxScan = Resources.Load<ComputeShader>("Scan/HillisSteeleFloat3MaxScan");
      kn_hillisSteeleFloat3MaxScan = cs_hillisSteeleFloat3MaxScan.FindKernel("HillisSteeleFloat3MaxScan");
    }

    public void Scan(ref ComputeBuffer cb_in)
    {
      Profiler.BeginSample("HillisSteeleFloat3MaxScan");
      cs_hillisSteeleFloat3MaxScan.SetInt(PropertyID.len, _dataSize);
      cs_hillisSteeleFloat3MaxScan.SetBuffer(kn_hillisSteeleFloat3MaxScan, BufferID.cb_in, cb_in);
      cs_hillisSteeleFloat3MaxScan.SetBuffer(kn_hillisSteeleFloat3MaxScan, BufferID.cb_prev, cb_prev);

      for (int offset=1; offset < _dataSize; offset <<= 1)
      {
        ComputeShaderUtil.CopyBufferFloat3(ref cb_in, ref cb_prev, _dataSize);
        cs_hillisSteeleFloat3MaxScan.SetInt(PropertyID.offset, offset);
        cs_hillisSteeleFloat3MaxScan.Dispatch(kn_hillisSteeleFloat3MaxScan, _gridSize, 1, 1);
      }
      Profiler.EndSample();
    }

    public override void Dispose()
    {
      cb_prev.Dispose();
    }
  }
}