using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Voxell.Jobx
{
  public class ReverseJob<T> where T : struct
  {
    private int _arraySize;
    private int _jobSize;

    private NativeArray<T> na_array;
    private ReverseArrayJob reverseArrayJob;

    public ReverseJob(ref NativeArray<T> na_array)
    {
      this._arraySize = na_array.Length;
      this._jobSize = na_array.Length/2;
      this.na_array = na_array;

      reverseArrayJob = new ReverseArrayJob(ref na_array, _arraySize);
    }

    /// <summary>Reverse native array in parallel.</summary>
    public void ReverseArray()
    {
      JobHandle jobHandle = reverseArrayJob.Schedule(_jobSize, Jobx.XL_BATCH_SIZE);
      jobHandle.Complete();
    }

    [BurstCompile]
    private struct ReverseArrayJob : IJobParallelFor
    {
      public int arraySize;
      [NativeDisableParallelForRestriction] public NativeArray<T> na_array;

      public ReverseArrayJob(ref NativeArray<T> na_array, int arraySize)
      {
        this.na_array = na_array;
        this.arraySize = arraySize;
      }

      public void Execute(int index)
      {
        T elem = na_array[index];
        na_array[index] = na_array[arraySize - index];
        na_array[arraySize - index] = elem;
      }
    }
  }
}