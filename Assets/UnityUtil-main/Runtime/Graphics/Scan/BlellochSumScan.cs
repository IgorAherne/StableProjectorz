using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxell.Graphics
{
  using Mathx;

  /// <summary>Blelloch exclusive sum scan.</summary>
  public sealed class BlellochSumScan : AbstractScan
  {
    private static ComputeShader cs_blellochSumScan;
    private static int kn_preSumScan, kn_addBlockSums;

    private readonly int[] _gridSizes;

    private ComputeBuffer[] cb_sumScanBlockSums;
    private ComputeBuffer[] cb_preSumScanTemps;
    private ComputeBuffer[] cb_inBlockSums;
    private ComputeBuffer[] cb_addBlockSumsTemps;
    private ComputeBuffer cb_dummyGrpSums;

    public BlellochSumScan(int dataSize)
    {
      // generate all grid sizes for blelloch sum scan
      List<int> blellochGridSizesList = new List<int>();
      int blellochGridSize = MathUtil.CalculateGrids(dataSize, Graphics.M_BLOCK_SZ);
      blellochGridSizesList.Add(blellochGridSize);
      while (blellochGridSize > Graphics.S_BLOCK_SZ)
      {
        blellochGridSize = MathUtil.CalculateGrids(blellochGridSize, Graphics.M_BLOCK_SZ);
        blellochGridSizesList.Add(blellochGridSize);
      }

      // store grid sizes as an array for performance
      this._gridSizes = blellochGridSizesList.ToArray();
      // number of recursives needed
      int totalRecursives = _gridSizes.Length;

      // create static compute buffer arrays so that we do need to create them on the fly during runtime
      // prevent managed allocation and garbage collection spike
      this.cb_sumScanBlockSums = new ComputeBuffer[totalRecursives];
      this.cb_preSumScanTemps = new ComputeBuffer[totalRecursives];
      this.cb_inBlockSums = new ComputeBuffer[totalRecursives];
      for (int b=0; b < totalRecursives; b++)
      {
        blellochGridSize = _gridSizes[b];
        this.cb_sumScanBlockSums[b] = new ComputeBuffer(blellochGridSize, StrideSize.s_uint);
        this.cb_preSumScanTemps[b] = new ComputeBuffer(blellochGridSize, StrideSize.s_uint);
        this.cb_inBlockSums[b] = new ComputeBuffer(blellochGridSize, StrideSize.s_uint);
      }
      this.cb_dummyGrpSums = new ComputeBuffer(1, StrideSize.s_int);
      this.cb_addBlockSumsTemps = new ComputeBuffer[totalRecursives];
      this.cb_addBlockSumsTemps[0] = new ComputeBuffer(dataSize, StrideSize.s_uint);
      for (int b=1; b < totalRecursives; b++)
        this.cb_addBlockSumsTemps[b] = new ComputeBuffer(_gridSizes[b-1], StrideSize.s_uint);
    }

    public static void InitKernels()
    {
      if (cs_blellochSumScan != null) return;
      cs_blellochSumScan = Resources.Load<ComputeShader>("Scan/BlellochSumScan");
      kn_preSumScan = cs_blellochSumScan.FindKernel("PreSumScan");
      kn_addBlockSums = cs_blellochSumScan.FindKernel("AddBlockSums");
    }

    public void Scan(
      ref ComputeBuffer cb_in, ref ComputeBuffer cb_out,
      int dataSize, int recurseNum = 0
    )
    {
      Profiler.BeginSample("BlellochSumScan");
      int blellochGridSize = _gridSizes[recurseNum];

      ComputeBuffer cb_sumScanBlockSum = cb_sumScanBlockSums[recurseNum];
      ComputeShaderUtil.ZeroOut(ref cb_sumScanBlockSum, blellochGridSize);

      // sum scan data allocated to each block
      cs_blellochSumScan.SetInt(PropertyID.len, dataSize);
      cs_blellochSumScan.SetBuffer(kn_preSumScan, BufferID.cb_out, cb_out);
      cs_blellochSumScan.SetBuffer(kn_preSumScan, BufferID.cb_in, cb_in);
      cs_blellochSumScan.SetBuffer(kn_preSumScan, BufferID.cb_blockSums, cb_sumScanBlockSum);
      cs_blellochSumScan.Dispatch(kn_preSumScan, blellochGridSize, 1, 1);

      // sum scan total sums produced by each block
      // use basic implementation if number of total sums is <= 2 * Graphics.M_BLOCK_SZ
      // (this requires only one block to do the scan)
      if (blellochGridSize <= Graphics.S_BLOCK_SZ)
      {
        ComputeShaderUtil.ZeroOut(ref cb_dummyGrpSums, 1);

        ComputeBuffer cb_preSumScanTemp = cb_preSumScanTemps[recurseNum];
        ComputeShaderUtil.CopyBuffer(ref cb_sumScanBlockSum, ref cb_preSumScanTemp, blellochGridSize);

        cs_blellochSumScan.SetInt(PropertyID.len, blellochGridSize);
        cs_blellochSumScan.SetBuffer(kn_preSumScan, BufferID.cb_out, cb_sumScanBlockSum);
        cs_blellochSumScan.SetBuffer(kn_preSumScan, BufferID.cb_in, cb_preSumScanTemp);
        cs_blellochSumScan.SetBuffer(kn_preSumScan, BufferID.cb_blockSums, cb_dummyGrpSums);
        cs_blellochSumScan.Dispatch(kn_preSumScan, 1, 1, 1);

      } else // else, recurse on this same function as you'll need the full-blown scan for the block sums
      {
        ComputeBuffer cb_inBlockSum = cb_inBlockSums[recurseNum];
        ComputeShaderUtil.CopyBuffer(ref cb_sumScanBlockSum, ref cb_inBlockSum, blellochGridSize);
        Scan(ref cb_inBlockSum, ref cb_sumScanBlockSum, blellochGridSize, recurseNum + 1);
      }

      ComputeBuffer cb_addBlockSumsTemp = cb_addBlockSumsTemps[recurseNum];
      ComputeShaderUtil.CopyBuffer(ref cb_out, ref cb_addBlockSumsTemp, dataSize);
      // add each block's total sum to its scan output in order to get the final, global scanned array
      cs_blellochSumScan.SetInt(PropertyID.len, dataSize);
      cs_blellochSumScan.SetBuffer(kn_addBlockSums, BufferID.cb_out, cb_out);
      cs_blellochSumScan.SetBuffer(kn_addBlockSums, BufferID.cb_in, cb_addBlockSumsTemp);
      cs_blellochSumScan.SetBuffer(kn_addBlockSums, BufferID.cb_blockSums, cb_sumScanBlockSum);
      cs_blellochSumScan.Dispatch(kn_addBlockSums, blellochGridSize, 1, 1);

      Profiler.EndSample();
    }

    public override void Dispose()
    {
      for (int b=0, length=_gridSizes.Length; b < length; b++)
      {
        cb_sumScanBlockSums[b]?.Dispose();
        cb_preSumScanTemps[b]?.Dispose();
        cb_inBlockSums[b]?.Dispose();
        cb_addBlockSumsTemps[b]?.Dispose();
      }
      cb_dummyGrpSums?.Dispose();
    }
  }
}