using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Voxell.Mathx
{
  public static class float3x
  {
    /// <summary>Shorthand for writing float3(0, 0, 1).</summary>
    public static readonly float3 one = new float3(1.0f, 1.0f, 1.0f);

    /// <summary>Shorthand for writing float3(0, 0, 1).</summary>
    public static readonly float3 forward = new float3(0.0f, 0.0f, 1.0f);

    /// <summary>Shorthand for writing float3(0, 0, -1).</summary>
    public static readonly float3 back = new float3(0.0f, 0.0f, -1.0f);

    /// <summary>Shorthand for writing float3(-1, 0, 0).</summary>
    public static readonly float3 left = new float3(-1.0f, 0.0f, 0.0f);

    /// <summary>Shorthand for writing float3(1, 0, 0).</summary>
    public static readonly float3 right = new float3(1.0f, 0.0f, 0.0f);

    /// <summary>Shorthand for writing float3(0, 1, 0).</summary>
    public static readonly float3 up = new float3(0.0f, 1.0f, 0.0f);

    /// <summary>Shorthand for writing float3(0, -1, 0).</summary>
    public static readonly float3 down = new float3(0.0f, -1.0f, 0.0f);
  }
}