using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Voxell.Mathx
{
  public static class MathUtil
  {
    /// <summary>Calculate the least amount of groups needed based on thread count.</summary>
    /// <param name="threadCount">total amount of size available</param>
    /// <param name="grpSize">maximum size of each divisions after the split</param>
    /// <returns>Group count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateGrids(int threadCount, int grpSize) => (threadCount + grpSize - 1) / grpSize;

    /// <summary>Calculate the least amount of groups needed based on thread count.</summary>
    /// <param name="threadCount">total amount of size available</param>
    /// <param name="grpSize">maximum size of each divisions after the split</param>
    /// <returns>Group count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateGrids(uint threadCount, uint grpSize) => (int)((threadCount + grpSize - 1) / grpSize);

    /// <summary>Convert float3 point location to int3 grid location.</summary>
    /// <param name="p">point</param>
    /// <param name="unitSize">unit size</param>
    /// <remarks>
    /// Point location should always be positive in all axis to obtain accurate grid location.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 PointToGrid(float3 p, float unitSize) => new int3(p / unitSize);

    /// <summary>
    /// Set all values in that array to the given value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetArray<T>(ref T[] array, T value)
    { for (int i=0; i < array.Length; i++) array[i] = value; }

    /// <summary>
    /// Generate an int array with sequence id as it's value
    /// (ex: [0, 1, 2, 3, 4] will be generated if `length` of 5 is being given)
    /// </summary>
    /// <param name="length">number of elements</param>
    public static int[] GenerateSeqArray(int length)
    {
      int[] array = new int[length];
      for (int i=0; i < length; i++) array[i] = i;
      return array;
    }

    /// <summary>Shuffles an array.</summary>
    public static void ShuffleArray<T>(ref T[] decklist)
    {
      for (int i = 0; i < decklist.Length; i++)
      {
        int randomIdx = UnityEngine.Random.Range(0, decklist.Length);
        T tempItem = decklist[randomIdx];
        decklist[randomIdx] = decklist[i];
        decklist[i] = tempItem;
      }
    }
  }
}