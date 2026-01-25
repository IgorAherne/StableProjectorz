using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Voxell.Jobx
{
  public sealed class RadixSortJob : System.IDisposable
  {
    public NativeArray<uint> na_values;
    public NativeArray<int> na_indices;
    public NativeArray<uint> na_sortedValues;
    public NativeArray<int> na_sortedIndices;

    private int _valueCount;
    private NativeArray<bool> na_bit;
    private NativeArray<int> na_truePrefixSum;
    private NativeArray<int> na_falsePrefixSum;

    private RadixBitCheckJob radixBitCheckJob;
    private RadixSortShuffleJob radixSortShuffleJob;
    private SumScanJob trueSumScanJob;
    private SumScanJob falseSumScanJob;

    public RadixSortJob(ref NativeArray<uint> na_values, ref NativeArray<int> na_indices)
    {
      this._valueCount = na_values.Length;
      this.na_values = na_values;
      this.na_indices = na_indices;
      this.na_sortedValues = new NativeArray<uint>(na_values, Allocator.Persistent);
      this.na_sortedIndices = new NativeArray<int>(na_indices, Allocator.Persistent);
      this.na_bit = new NativeArray<bool>(_valueCount, Allocator.Persistent);
      this.na_truePrefixSum = new NativeArray<int>(_valueCount, Allocator.Persistent);
      this.na_falsePrefixSum = new NativeArray<int>(_valueCount, Allocator.Persistent);

      this.radixBitCheckJob = new RadixBitCheckJob(
        ref na_values, ref na_bit, ref na_truePrefixSum, ref na_falsePrefixSum
      );
      this.radixSortShuffleJob = new RadixSortShuffleJob(
        ref na_values, ref na_indices, ref na_sortedValues, ref na_sortedIndices,
        ref na_bit, ref na_truePrefixSum, ref na_falsePrefixSum
      );

      this.trueSumScanJob = new SumScanJob(ref na_truePrefixSum);
      this.falseSumScanJob = new SumScanJob(ref na_falsePrefixSum);
    }

    /// <summary>Sort an array of unsigned integers.</summary>
    public void Sort(int maxShiftWidth = 32)
    {
      Profiler.BeginSample("RadixSort");
      uint mask = 1;
      int valueCount = na_values.Length;
      JobHandle jobHandle;

      for (int m=0; m < maxShiftWidth; m++)
      {
        radixBitCheckJob.mask = mask;
        jobHandle = radixBitCheckJob.Schedule(valueCount, Jobx.XL_BATCH_SIZE);
        jobHandle.Complete();

        trueSumScanJob.InclusiveSumScan();
        falseSumScanJob.InclusiveSumScan();
        radixSortShuffleJob.lastFalseIdx = na_falsePrefixSum[valueCount-1];

        jobHandle = radixSortShuffleJob.Schedule(valueCount, Jobx.XL_BATCH_SIZE);
        jobHandle.Complete();
        na_values.CopyFrom(na_sortedValues);
        na_indices.CopyFrom(na_sortedIndices);

        mask <<= 1;
      }
      Profiler.EndSample();
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct RadixBitCheckJob : IJobParallelFor
    {
      public uint mask;

      [ReadOnly] public NativeArray<uint> na_values;
      // true if current bit is 1
      [WriteOnly] public NativeArray<bool> na_bit;
      [WriteOnly] public NativeArray<int> na_truePrefixSum;
      [WriteOnly] public NativeArray<int> na_falsePrefixSum;

      public RadixBitCheckJob(
        ref NativeArray<uint> na_values,
        ref NativeArray<bool> na_bit,
        ref NativeArray<int> na_truePrefixSum,
        ref NativeArray<int> na_falsePrefixSum
      )
      {
        this.na_values = na_values;
        this.na_bit = na_bit;
        this.na_truePrefixSum = na_truePrefixSum;
        this.na_falsePrefixSum = na_falsePrefixSum;
        this.mask = 1;
      }

      public void Execute(int index)
      {
        uint value = na_values[index];
        bool bit = (value & mask) > 0;

        na_bit[index] = bit;
        if (bit)
        {
          na_truePrefixSum[index] = 1;
          na_falsePrefixSum[index] = 0;
        } else
        {
          na_truePrefixSum[index] = 0;
          na_falsePrefixSum[index] = 1;
        }
      }
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct RadixSortShuffleJob : IJobParallelFor
    {
      public int lastFalseIdx;

      [ReadOnly] public NativeArray<uint> na_values;
      [ReadOnly] public NativeArray<int> na_indices;
      [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<uint> na_sortedValues;
      [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<int> na_sortedIndices;
      // true if current bit is 1
      [ReadOnly] public NativeArray<bool> na_bit;
      [ReadOnly] public NativeArray<int> na_truePrefixSum;
      [ReadOnly] public NativeArray<int> na_falsePrefixSum;

      public RadixSortShuffleJob(
        ref NativeArray<uint> na_values,
        ref NativeArray<int> na_indices,
        ref NativeArray<uint> na_sortedValues,
        ref NativeArray<int> na_sortedIndices,
        ref NativeArray<bool> na_bit,
        ref NativeArray<int> na_truePrefixSum,
        ref NativeArray<int> na_falsePrefixSum
      )
      {
        this.na_values = na_values;
        this.na_indices = na_indices;
        this.na_sortedValues = na_sortedValues;
        this.na_sortedIndices = na_sortedIndices;
        this.na_bit = na_bit;
        this.na_truePrefixSum = na_truePrefixSum;
        this.na_falsePrefixSum = na_falsePrefixSum;

        lastFalseIdx = 0;
      }

      public void Execute(int index)
      {
        int sortIdx;
        if (na_bit[index])
        {
          // minus one because we use inclusive sum scan
          sortIdx = na_truePrefixSum[index] - 1 + lastFalseIdx;
          na_sortedValues[sortIdx] = na_values[index];
          na_sortedIndices[sortIdx] = na_indices[index];
        } else
        {
          sortIdx = na_falsePrefixSum[index] - 1;
          na_sortedValues[sortIdx] = na_values[index];
          na_sortedIndices[sortIdx] = na_indices[index];
        }
      }
    }

    public void Dispose()
    {
      na_sortedValues.Dispose();
      na_sortedIndices.Dispose();
      na_bit.Dispose();
      na_truePrefixSum.Dispose();
      na_falsePrefixSum.Dispose();
      trueSumScanJob.Dispose();
      falseSumScanJob.Dispose();
    }
  }
}