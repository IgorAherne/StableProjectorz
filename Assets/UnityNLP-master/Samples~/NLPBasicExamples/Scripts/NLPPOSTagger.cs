using UnityEngine;
using Voxell;
using Voxell.NLP.PosTagger;
using Voxell.NLP.Tokenize;
using Voxell.Inspector;

public class NLPPOSTagger : MonoBehaviour
{
  [StreamingAssetFilePath] public string tokenizerModel;
  [StreamingAssetFilePath] public string posTaggerModel;
  [StreamingAssetFilePath] public string tagDict;
  [TextArea(1, 5)] public string sentence;
  public string[] tokens;
  public string[] posTags;

  private EnglishMaximumEntropyTokenizer tokenizer;
  private EnglishMaximumEntropyPosTagger posTagger;

  [Button]
  public void Tag()
  {
    // link to POS tags meanings: https://www.ling.upenn.edu/courses/Fall_2003/ling001/penn_treebank_pos.html
    tokenizer = new EnglishMaximumEntropyTokenizer(FileUtilx.GetStreamingAssetFilePath(tokenizerModel));
    posTagger = new EnglishMaximumEntropyPosTagger(
      FileUtilx.GetStreamingAssetFilePath(posTaggerModel),
      FileUtilx.GetStreamingAssetFilePath(tagDict));

    tokens = tokenizer.Tokenize(sentence);
    posTags = posTagger.Tag(tokens);
  }
}
