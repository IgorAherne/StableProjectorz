using System.Text.RegularExpressions;
using Voxell.NLP.Stem;

namespace Voxell.NLP
{
  [System.Serializable]
  public class Token
  {
    public Token(string text, string tag, ref IStemmer stemmer)
    {
      this.text = text;
      this.tag = tag;
      this.lemma = stemmer.Stem(text);
    }
    /// <summary>
    /// The original word text.
    /// </summary>
    public string text;

    /// <summary>
    /// Part-of-speech tag.
    /// https://www.ling.upenn.edu/courses/Fall_2003/ling001/penn_treebank_pos.html
    /// </summary>
    public string tag;

    /// <summary>
    /// The base form of the word.
    /// </summary>
    public string lemma;

    /// <summary>
    /// Is the token an alpha character?
    /// </summary>
    public bool IsAlpha
    {
      get
      {
        return Regex.IsMatch(text, @"^[a-zA-Z]+|[\u4e00-\u9fa5]+$");
      }
    }

    public double vector;
  }
}
