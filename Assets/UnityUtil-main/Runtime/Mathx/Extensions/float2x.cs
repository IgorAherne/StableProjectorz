using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;

namespace Voxell.Mathx
{
  public static class float2x
  {
    /// <summary>Shorthand for writing float2(-1, 0).</summary>
    public static readonly float2 left = new float2(-1.0f, 0.0f);

    /// <summary>Shorthand for writing float2(1, 0).</summary>
    public static readonly float2 right = new float2(1.0f, 0.0f);

    /// <summary>Shorthand for writing float2(0, 1).</summary>
    public static readonly float2 up = new float2(0.0f, 1.0f);

    /// <summary>Shorthand for writing float2(0, -1).</summary>
    public static readonly float2 down = new float2(0.0f, -1.0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 perpendicular(this float2 vector) => new float2(-vector.y, vector.x);

    // Returns the angle in degrees between /from/ and /to/.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float angle(this float2 from, float2 to)
    {
      // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
      float denominator = (float)math.sqrt(math.lengthsq(from) * math.lengthsq(to));
      if (denominator < Vector2.kEpsilonNormalSqrt) return 0.0f;

      float dot = math.clamp(math.dot(from, to) / denominator, -1.0f, 1.0f);
      return math.acos(dot) * Mathf.Rad2Deg;
    }
  }
}