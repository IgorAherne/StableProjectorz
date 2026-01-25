namespace Voxell.Jobx
{
  public static class Jobx
  {
    // how to use??
    // if the computation of each element is fairly simple,
    // use a larger batch size and vice versa

    // obviously, there are lots of factors playing here,
    // so use the unity's profiler to benchmark your algorithm
    // running on different batch sizes
    public const int XS_BATCH_SIZE = 64;
    public const int S_BATCH_SIZE = 128;
    public const int M_BATCH_SIZE = 256;
    public const int L_BATCH_SIZE = 512;
    public const int XL_BATCH_SIZE = 1024;
  }
}