using UnityEngine;

namespace Voxell.Graphics
{
  public abstract class AbstractScan : System.IDisposable
  {
    private protected static class PropertyID
    {
      public static readonly int len = Shader.PropertyToID("_len");
      public static readonly int offset = Shader.PropertyToID("_offset");
    }

    private protected static class BufferID
    {
      public static readonly int cb_in = Shader.PropertyToID("cb_in");
      public static readonly int cb_out = Shader.PropertyToID("cb_out");
      public static readonly int cb_prev = Shader.PropertyToID("cb_prev");
      public static readonly int cb_blockSums = Shader.PropertyToID("cb_blockSums");
    }

    public abstract void Dispose();
  }
}