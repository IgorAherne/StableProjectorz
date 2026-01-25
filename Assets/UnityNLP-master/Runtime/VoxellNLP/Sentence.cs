using System.Collections.Generic;
using Voxell.NLP.Tokenize;
using Voxell.NLP.PosTagger;
using Voxell.NLP.Stem;

namespace Voxell.NLP
{
  [System.Serializable]
  public class Sentence
  {
    public Sentence(
      string text,
      string label,
      ITokenizer tokenizer,
      IPosTagger posTagger,
      IStemmer stemmer
    )
    {
      this.text = text;
      this.label = label;
      string[] tokens = tokenizer.Tokenize(text);
      string[] tags = posTagger.Tag(tokens);

      words = new List<Token>();
      for (int t=0; t < tokens.Length; t++)
        words.Add(new Token(tokens[t], tags[t], ref stemmer));
    }

    public string text;
    public string label;
    public List<Token> words;
    public double[] vector;
  }
}
