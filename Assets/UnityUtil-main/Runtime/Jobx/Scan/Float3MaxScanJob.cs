using UnityEngine.Profiling;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Voxell.Jobx
{
  public sealed class Float3MaxScanJob : System.IDisposable
  {
    private int _valueCount;
    private NativeArray<float3> na_values;
    private NativeArray<float3> na_prevValues;
    private HillisSteeleFloat3MaxScanJob maxScanJob;

    public Float3MaxScanJob(ref NativeArray<float3> na_values)
    {
      this._valueCount = na_values.Length;
      this.na_values = na_values;
      this.na_prevValues = new NativeArray<float3>(na_values, Allocator.Persistent);

      this.maxScanJob = new HillisSteeleFloat3MaxScanJob(
        ref na_values, ref na_prevValues
      );
    }

    /// <summary>Perform a Hillis Steele inclusive max scan.</summary>
    public void InclusiveMaxScan()
    {
      Profiler.BeginSample("InclusiveMaxScan");
      JobHandle jobHandle;
      for (int offset=1; offset < _valueCount; offset <<= 1)
      {
        na_prevValues.CopyFrom(na_values);
        maxScanJob.offset = offset;
        jobHandle = maxScanJob.Schedule(_valueCount, Jobx.XL_BATCH_SIZE);
        jobHandle.Complete();
      }
      Profiler.EndSample();
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct HillisSteeleFloat3MaxScanJob : IJobParallelFor
    {
      public int offset;

      public NativeArray<float3> na_values;
      [NativeDisableParallelForRestriction, ReadOnly]
      public NativeArray<float3> na_prevValues;

      public HillisSteeleFloat3MaxScanJob(
        ref NativeArray<float3> na_values, ref NativeArray<float3> na_prevValues
      )
      {
        this.na_values = na_values;
        this.na_prevValues = na_prevValues;
        this.offset = 1;
      }

      public void Execute(int index)
      {
        int sumIdx = index - offset;
        if (sumIdx >= 0) na_values[index] = math.max(na_values[index], na_prevValues[sumIdx]);
      }
    }

    public void Dispose() => na_prevValues.Dispose();
  }
}