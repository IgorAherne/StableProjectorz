using Unity.Mathematics;
using Unity.Collections;
using NUnit.Framework;
using Random = UnityEngine.Random;

namespace Voxell.Jobx
{
  using Mathx;

  public class JobxTests
  {
    private const int ARRAY_COUNT = 2000;

    [Test]
    public void SumScanJobTest()
    {
      int[] array = new int[ARRAY_COUNT];
      for (int i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomInt();

      NativeArray<int> na_array = new NativeArray<int>(array, Allocator.TempJob);
      SumScanJob sumScanJob = new SumScanJob(ref na_array);
      sumScanJob.InclusiveSumScan();

      // using serial inclusive sum scan method to make sure that the parallel method works
      int sum = 0;
      for (int i=0; i < ARRAY_COUNT; i++)
      {
        sum += array[i];
        Assert.AreEqual(sum, na_array[i]);
      }

      na_array.Dispose();
      sumScanJob.Dispose();
    }

    [Test]
    public void Float3MinScanJobTest()
    {
      float3[] array = new float3[ARRAY_COUNT];
      for (int i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomFloat3();

      NativeArray<float3> na_array = new NativeArray<float3>(array, Allocator.TempJob);
      Float3MinScanJob float3MinScanJob = new Float3MinScanJob(ref na_array);
      float3MinScanJob.InclusiveMinScan();

      // using serial inclusive min scan method to make sure that the parallel method works
      float3 float3Max = array[0];
      for (int i=0; i < ARRAY_COUNT; i++)
      {
        float3Max = math.min(float3Max, array[i]);
        Assert.AreEqual(float3Max, na_array[i]);
      }

      na_array.Dispose();
      float3MinScanJob.Dispose();
    }

    [Test]
    public void Float3MaxScanJobTest()
    {
      float3[] array = new float3[ARRAY_COUNT];
      for (int i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomFloat3();

      NativeArray<float3> na_array = new NativeArray<float3>(array, Allocator.TempJob);
      Float3MaxScanJob float3MaxScanJob = new Float3MaxScanJob(ref na_array);
      float3MaxScanJob.InclusiveMaxScan();

      // using serial inclusive max scan method to make sure that the parallel method works
      float3 float3Max = array[0];
      for (int i=0; i < ARRAY_COUNT; i++)
      {
        float3Max = math.max(float3Max, array[i]);
        Assert.AreEqual(float3Max, na_array[i]);
      }

      na_array.Dispose();
      float3MaxScanJob.Dispose();
    }

    [Test]
    public void RadixSortJobTest()
    {
      uint[] array = new uint[ARRAY_COUNT];
      int[] indices = MathUtil.GenerateSeqArray(ARRAY_COUNT);
      for (uint i=0; i < ARRAY_COUNT; i++) array[i] = GenerateRandomUInt();

      NativeArray<uint> na_values = new NativeArray<uint>(array, Allocator.TempJob);
      NativeArray<int> na_indices = new NativeArray<int>(indices, Allocator.TempJob);
      RadixSortJob radixSortJob = new RadixSortJob(ref na_values, ref na_indices);
      radixSortJob.Sort();

      // check if sorting works
      for (int i=0; i < ARRAY_COUNT-1; i++)
        Assert.GreaterOrEqual(na_values[i+1], na_values[i]);
      // check if indices are sorted properly
      for (int i=0; i < ARRAY_COUNT; i++)
        Assert.AreEqual(array[na_indices[i]], na_values[i]);

      na_values.Dispose();
      na_indices.Dispose();
      radixSortJob.Dispose();
    }

    private float3 GenerateRandomFloat3() => Random.insideUnitSphere * Random.Range(0.0f, (float)ARRAY_COUNT);
    private int GenerateRandomInt() => Random.Range(0, ARRAY_COUNT);
    private uint GenerateRandomUInt() => (uint)Random.Range(0, ARRAY_COUNT);
  }
}
