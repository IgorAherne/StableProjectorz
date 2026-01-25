using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Voxell.Jobx
{
  public class SumScanJob : System.IDisposable
  {
    private int _arrayLength;
    private NativeArray<int> na_array;
    private NativeArray<int> na_prevArray;
    private HillisSteeleSumScanJob sumScanJob;

    public SumScanJob(ref NativeArray<int> na_array)
    {
      this._arrayLength = na_array.Length;
      this.na_array = na_array;
      this.na_prevArray = new NativeArray<int>(na_array, Allocator.Persistent);
      this.sumScanJob = new HillisSteeleSumScanJob(ref na_array, ref na_prevArray);
    }

    /// <summary>Perform a Hillis Steele inclusive sum scan.</summary>
    public void InclusiveSumScan()
    {
      Profiler.BeginSample("InclusiveSumScan");
      JobHandle jobHandle;

      for (int offset=1; offset < _arrayLength; offset <<= 1)
      {
        na_prevArray.CopyFrom(na_array);
        sumScanJob.offset = offset;
        jobHandle = sumScanJob.Schedule(_arrayLength, Jobx.XL_BATCH_SIZE);
        jobHandle.Complete();
      }
      Profiler.EndSample();
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct HillisSteeleSumScanJob : IJobParallelFor
    {
      public int offset;

      public NativeArray<int> na_array;
      [NativeDisableParallelForRestriction, ReadOnly]
      public NativeArray<int> na_prevArray;

      public HillisSteeleSumScanJob(ref NativeArray<int> na_array, ref NativeArray<int> na_prevArray)
      {
        this.na_array = na_array;
        this.na_prevArray = na_prevArray;
        this.offset = 1;
      }

      public void Execute(int index)
      {
        int sumIdx = index - offset;
        if (sumIdx >= 0) na_array[index] += na_prevArray[sumIdx];
      }
    }

    public void Dispose() => na_prevArray.Dispose();
  }
}