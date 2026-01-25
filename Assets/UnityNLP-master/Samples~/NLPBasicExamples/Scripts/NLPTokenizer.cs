using UnityEngine;
using Voxell;
using Voxell.NLP.Tokenize;
using Voxell.Inspector;

public class NLPTokenizer : MonoBehaviour
{
  [StreamingAssetFilePath] public string tokenizerModel;
  [TextArea(1, 5)] public string sentence;
  public string[] tokens;

  private EnglishMaximumEntropyTokenizer tokenizer;

  [Button]
  public void Tokenize()
  {
    tokenizer = new EnglishMaximumEntropyTokenizer(FileUtilx.GetStreamingAssetFilePath(tokenizerModel));
    tokens = tokenizer.Tokenize(sentence);
  }
}
