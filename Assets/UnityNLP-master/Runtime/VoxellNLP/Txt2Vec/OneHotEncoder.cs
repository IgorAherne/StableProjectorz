using System.Collections.Generic;
using System.Linq;

namespace Voxell.NLP.Txt2Vec
{
  /// <summary>
  /// A one hot encoding is a representation of categorical variables as binary vectors. 
  /// Each integer value is represented as a binary vector that is all zero values except the index of the integer, which is marked with a 1.
  /// </summary>
  public class OneHotEncoder
  {
    public List<Sentence> Sentences { get; set; }

    public List<string> words { get; set; }

    public void Encode(Sentence sentence)
    {
      InitDictionary();

      var vector = words.Select(x => 0D).ToArray();

      sentence.words.ForEach(w =>
      {
        int index = words.IndexOf(w.lemma);
        if(index > 0)
          vector[index] = 1;
      });

      sentence.vector = vector;
    }

    public List<string> EncodeAll()
    {
      InitDictionary();
      Sentences.ForEach(sent => Encode(sent));
      //Parallel.ForEach(Sentences, sent => Encode(sent));

      return words;
    }

    private List<string> InitDictionary()
    {
      if (words == null)
      {
        words = new List<string>();
        Sentences.ForEach(x =>
        {
          words.AddRange(x.words.Where(w => w.IsAlpha).Select(w => w.lemma));
        });
        words = words.Distinct().OrderBy(x => x).ToList();
      }

      return words;
    }
  }
}
