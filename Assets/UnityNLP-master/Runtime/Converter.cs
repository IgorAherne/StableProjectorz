using UnityEngine;
using System;
using System.IO;
using SharpEntropy;
using SharpEntropy.IO;

namespace ModelConverter
{
  /// <summary>
  /// Summary description for Converter.
  /// </summary>
  public static class Converter
  {
    private static bool ConvertFolder(string folder)
    {
      BinaryGisModelWriter writer = new BinaryGisModelWriter();

      foreach (string file in Directory.GetFiles(folder))
      {
        if (file.Substring(file.Length - 4, 4) == ".bin")
        {
          Debug.Log("converting " + file + " ...");
          writer.Persist(new GisModel(new JavaBinaryGisModelReader(file)), file.Replace(".bin", ".nbin"));
          Debug.Log("done");
        }
      }

      string[] directories = Directory.GetDirectories(folder);
      for (int d=0; d < directories.Length; d++)
      {
        if (!ConvertFolder(directories[d]))
          return false;
      }

      return true;
    }
  }
}
