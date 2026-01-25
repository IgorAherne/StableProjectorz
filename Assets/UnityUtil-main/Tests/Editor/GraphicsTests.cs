using UnityEngine;
using Unity.Mathematics;
using NUnit.Framework;
using Random = UnityEngine.Random;

namespace Voxell.Graphics
{
  using Mathx;

  public class GraphicsTests
  {
    private const int ARRAY_COUNT = 100000;

    [Test]
    public void HillisSteeleFloat3MinScanTest()
    {
      float3[] array = new float3[ARRAY_COUNT];
      float3[] scannedArray = new float3[ARRAY_COUNT];
      for (int i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomFloat3();

      ComputeBuffer cb_in = new ComputeBuffer(ARRAY_COUNT, StrideSize.s_float3);

      ComputeShaderUtil.InitKernels();
      HillisSteeleFloat3MinScan.InitKernels();

      cb_in.SetData(array);

      HillisSteeleFloat3MinScan hillisSteeleFloat3MinScan = new HillisSteeleFloat3MinScan(ARRAY_COUNT);
      hillisSteeleFloat3MinScan.Scan(ref cb_in);

      cb_in.GetData(scannedArray);

      // using serial inclusive min scan method to make sure that the parallel method works
      float3 float3Max = array[0];
      for (int i=0; i < ARRAY_COUNT; i++)
      {
        float3Max = math.min(float3Max, array[i]);
        Assert.AreEqual(float3Max, scannedArray[i]);
      }

      cb_in.Dispose();
      hillisSteeleFloat3MinScan.Dispose();
    }

    [Test]
    public void HillisSteeleFloat3MaxScanTest()
    {
      float3[] array = new float3[ARRAY_COUNT];
      float3[] scannedArray = new float3[ARRAY_COUNT];
      for (int i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomFloat3();

      ComputeBuffer cb_in = new ComputeBuffer(ARRAY_COUNT, StrideSize.s_float3);

      ComputeShaderUtil.InitKernels();
      HillisSteeleFloat3MaxScan.InitKernels();

      cb_in.SetData(array);

      HillisSteeleFloat3MaxScan hillisSteeleFloat3MaxScan = new HillisSteeleFloat3MaxScan(ARRAY_COUNT);
      hillisSteeleFloat3MaxScan.Scan(ref cb_in);

      cb_in.GetData(scannedArray);

      // using serial inclusive min scan method to make sure that the parallel method works
      float3 float3Max = array[0];
      for (int i=0; i < ARRAY_COUNT; i++)
      {
        float3Max = math.max(float3Max, array[i]);
        Assert.AreEqual(float3Max, scannedArray[i]);
      }

      cb_in.Dispose();
      hillisSteeleFloat3MaxScan.Dispose();
    }

    [Test]
    public void HillisSteeleSumScanTest()
    {
      uint[] array = new uint[ARRAY_COUNT];
      uint[] scannedArray = new uint[ARRAY_COUNT];
      for (int i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomUInt();

      ComputeBuffer cb_in = new ComputeBuffer(ARRAY_COUNT, StrideSize.s_uint);

      ComputeShaderUtil.InitKernels();
      HillisSteeleSumScan.InitKernels();

      cb_in.SetData(array);

      HillisSteeleSumScan hillisSteeleSumScan = new HillisSteeleSumScan(ARRAY_COUNT);
      hillisSteeleSumScan.Scan(ref cb_in);

      cb_in.GetData(scannedArray);

      // using serial inclusive sum scan method to make sure that the parallel method works
      uint sum = 0;
      for (int i=0; i < ARRAY_COUNT; i++)
      {
        sum += array[i];
        Assert.AreEqual(sum, scannedArray[i], i.ToString());
      }

      cb_in.Dispose();
      hillisSteeleSumScan.Dispose();
    }

    [Test]
    public void BlellochSumScanTest()
    {
      uint[] array = new uint[ARRAY_COUNT];
      uint[] scannedArray = new uint[ARRAY_COUNT];
      for (int i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomUInt();

      ComputeBuffer cb_in = new ComputeBuffer(ARRAY_COUNT, StrideSize.s_uint);
      ComputeBuffer cb_out = new ComputeBuffer(ARRAY_COUNT, StrideSize.s_uint);

      ComputeShaderUtil.InitKernels();
      BlellochSumScan.InitKernels();

      cb_in.SetData(array);
      ComputeShaderUtil.ZeroOut(ref cb_out, ARRAY_COUNT);

      BlellochSumScan blellochSumScan = new BlellochSumScan(ARRAY_COUNT);
      blellochSumScan.Scan(ref cb_in, ref cb_out, ARRAY_COUNT);

      cb_out.GetData(scannedArray);

      // using serial exclusive sum scan method to make sure that the parallel method works
      uint sum = 0;
      for (int i=0; i < ARRAY_COUNT; i++)
      {
        Assert.AreEqual(sum, scannedArray[i]);
        sum += array[i];
      }

      cb_in.Dispose();
      cb_out.Dispose();
      blellochSumScan.Dispose();
    }

    [Test]
    public void RadixSortTest()
    {
      uint[] array = new uint[ARRAY_COUNT];
      uint[] sortedArray = new uint[ARRAY_COUNT];
      int[] indices = MathUtil.GenerateSeqArray(ARRAY_COUNT);
      for (uint i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomUInt();

      ComputeBuffer cb_sort = new ComputeBuffer(ARRAY_COUNT, StrideSize.s_uint);
      ComputeBuffer cb_indices = new ComputeBuffer(ARRAY_COUNT, StrideSize.s_int);

      ComputeShaderUtil.InitKernels();
      RadixSort.InitKernels();

      cb_sort.SetData(array);
      cb_indices.SetData(indices);

      RadixSort radixSort = new RadixSort(ARRAY_COUNT);
      radixSort.Setup(ref cb_sort, ref cb_indices);
      radixSort.Sort();

      cb_sort.GetData(sortedArray);
      cb_indices.GetData(indices);

      // check if sorting works
      for (int i=0; i < ARRAY_COUNT-1; i++)
        Assert.GreaterOrEqual(sortedArray[i+1], sortedArray[i]);
      // check if indices are sorted properly
      for (int i=0; i < ARRAY_COUNT; i++)
        Assert.AreEqual(array[indices[i]], sortedArray[i]);

      cb_sort.Dispose();
      cb_indices.Dispose();
      radixSort.Dispose();
    }

    private float3 GenerateRandomFloat3() => Random.insideUnitSphere * Random.Range(0.0f, (float)ARRAY_COUNT);
    private int GenerateRandomInt() => Random.Range(0, ARRAY_COUNT);
    private uint GenerateRandomUInt() => (uint)Random.Range(0, ARRAY_COUNT);
  }
}