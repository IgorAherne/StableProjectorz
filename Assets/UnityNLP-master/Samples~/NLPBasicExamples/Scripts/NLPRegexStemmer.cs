using UnityEngine;
using Voxell;
using Voxell.NLP.Stem;
using Voxell.NLP.Tokenize;
using Voxell.Inspector;

public class NLPRegexStemmer : MonoBehaviour
{
  [StreamingAssetFilePath] public string tokenizerModel;
  [TextArea(1, 5)] public string sentence;
  public string[] tokens;
  public string[] stemmedTokens;

  private EnglishMaximumEntropyTokenizer tokenizer;
  private RegexStemmer regexStemmer;

  [Button]
  public void Stem()
  {
    tokenizer = new EnglishMaximumEntropyTokenizer(FileUtilx.GetStreamingAssetFilePath(tokenizerModel));
    regexStemmer = new RegexStemmer();
    regexStemmer.CreatePattern();

    // tokenize
    tokens = tokenizer.Tokenize(sentence);
    stemmedTokens = new string[tokens.Length];
    // stem
    for (int t=0; t < tokens.Length; t++)
      stemmedTokens[t] = regexStemmer.Stem(tokens[t]);
  }
}
