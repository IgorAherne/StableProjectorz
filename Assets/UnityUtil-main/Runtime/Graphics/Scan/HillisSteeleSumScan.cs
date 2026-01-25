using UnityEngine;
using UnityEngine.Profiling;

namespace Voxell.Graphics
{
  using Mathx;

  public sealed class HillisSteeleSumScan : AbstractScan
  {
    private static ComputeShader cs_hillisSteeleSumScan;
    private static int kn_hillisSteeleSumScan;

    private readonly int _dataSize;
    private readonly int _gridSize;

    private ComputeBuffer cb_prev;

    public HillisSteeleSumScan(int dataSize)
    {
      _dataSize = dataSize;
      _gridSize = MathUtil.CalculateGrids(_dataSize, Graphics.M_BLOCK_SZ);
      cb_prev = new ComputeBuffer(dataSize, StrideSize.s_uint);
    }

    public static void InitKernels()
    {
      if (cs_hillisSteeleSumScan != null) return;
      cs_hillisSteeleSumScan = Resources.Load<ComputeShader>("Scan/HillisSteeleSumScan");
      kn_hillisSteeleSumScan = cs_hillisSteeleSumScan.FindKernel("HillisSteeleSumScan");
    }

    public void Scan(ref ComputeBuffer cb_in)
    {
      Profiler.BeginSample("HillisSteeleSumScan");
      cs_hillisSteeleSumScan.SetInt(PropertyID.len, _dataSize);
      cs_hillisSteeleSumScan.SetBuffer(kn_hillisSteeleSumScan, BufferID.cb_in, cb_in);
      cs_hillisSteeleSumScan.SetBuffer(kn_hillisSteeleSumScan, BufferID.cb_prev, cb_prev);

      for (int offset=1; offset < _dataSize; offset <<= 1)
      {
        ComputeShaderUtil.CopyBuffer(ref cb_in, ref cb_prev, _dataSize);
        cs_hillisSteeleSumScan.SetInt(PropertyID.offset, offset);
        cs_hillisSteeleSumScan.Dispatch(kn_hillisSteeleSumScan, _gridSize, 1, 1);
      }
      Profiler.EndSample();
    }

    public override void Dispose()
    {
      cb_prev.Dispose();
    }
  }
}