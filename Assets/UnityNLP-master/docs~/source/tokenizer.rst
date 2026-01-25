Tokenizer
~~~~~~~~~

A tokenizer's goal is to detect individual words in a sentence and split them up into individual tokens. This functionality is the core of almost all NLP task.

English Maximum Entropy Tokenizer
---------------------------------

Example
=======

In this example, a pretrained model is used to tokenize a fairly complex sentence which consists of symbols and punctuations that we rarely see in normal sentences.

.. code-block:: csharp

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
      tokenizer = new EnglishMaximumEntropyTokenizer(FileUtil.GetStreamingAssetFilePath(tokenizerModel));
      tokens = tokenizer.Tokenize(sentence);
    }
  }

.. image:: ../../Pictures~/TokenizerExample.png
  :alt: TokenizerExample
