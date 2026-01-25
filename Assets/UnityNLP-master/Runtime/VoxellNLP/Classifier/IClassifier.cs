using System;
using System.Collections.Generic;

namespace Voxell.NLP.Classifier
{
  public interface IClassifier
  {
    /// <summary>
    /// Training by feature vector
    /// </summary>
    /// <param name="sentences"></param>
    /// <param name="options"></param>
    void Train(List<Sentence> sentences, ClassifyOptions options);

    /// <summary>
    /// Predict by feature vector
    /// </summary>
    /// <param name="sentence"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    List<Tuple<string, double>> Classify(Sentence sentence, ClassifyOptions options);

    void SaveModel(ClassifyOptions options);

    void LoadModel(ClassifyOptions options);
  }
}
