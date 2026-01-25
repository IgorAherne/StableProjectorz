using System.Collections.Generic;
using UnityEngine;
using Voxell.Inspector;

namespace Voxell.NLP.Classifier
{
  [System.Serializable]
  public struct ClassifyOptions
  {
    [StreamingAssetFilePath, SerializeField] private string modelFilePath;
    [Tooltip("Vocabulary size")] public int dimension;
    [Tooltip("Types of labels"), InspectOnly] public List<string> labels;

    public void AddLabel(string label)
    {
      if (!labels.Contains(label)) labels.Add(label);
    }

    public string GetModelFilePath() => FileUtilx.GetStreamingAssetFilePath(modelFilePath);
  }
}
