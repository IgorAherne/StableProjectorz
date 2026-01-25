using System;
using System.Collections.Generic;
using System.Text;
//using Bigtree.Algorithm.Matrix;
using Txt2Vec;

namespace Voxell.NLP.Featuring
{
  public class Word2VecFeatureExtractor : IFeatureExtractor
  {
    public int Dimension { get; set; }
    public List<Sentence> Sentences { get; set; }
    public List<Tuple<string, int>> Dictionary { get; set; }
    public List<string> Features { get; set; }
    //public Shape Shape { get; set; }
    public VectorGenerator Vg { get; set; }
    public int SentenceVectorSize { get; set; }
    public string ModelFile { get; set; }

    public void Vectorize(List<string> features)
    {
      Init();

      Sentences.ForEach(s => {
        List<string> wordLemmas = new List<string>();
        s.words.ForEach(word => {
          if (features.Contains(word.lemma))
            wordLemmas.Add(word.lemma);
        });
        Vec sentenceVec = Vg.Sent2Vec(wordLemmas);

        s.vector = sentenceVec.VecNodes.ToArray();
      });

    }

    private void Init()
    {
      if(Vg == null)
      {
        Args args = new Args();
        args.ModelFile = ModelFile;
        Vg = new VectorGenerator(args);
        SentenceVectorSize = this.Vg.Model.VectorSize;
        Features = new List<string>();
        for (int i = 0; i < SentenceVectorSize; i++)
          Features.Add($"f-{i}");
      }
    }
  }
}
