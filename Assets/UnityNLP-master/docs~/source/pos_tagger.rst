POS Tagger
~~~~~~~~~~

A part-of-speech tagger takes in a word and determines what part-of-speech it is.

English Maximum Entropy Pos Tagger 
----------------------------------

Example
=======

In this example, the sentence is first being tokenized using the ``EnglishMaximumEntropyTokenizer`` into individual tokens (words). Each of these tokens are then being tagged via a pretrained model.

You can find the full list of part-of-speech tags along with their meanings `here <https://www.ling.upenn.edu/courses/Fall_2003/ling001/penn_treebank_pos.html>`_.

.. code-block:: csharp

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
      tokenizer = new EnglishMaximumEntropyTokenizer(FileUtil.GetStreamingAssetFilePath(tokenizerModel));
      posTagger = new EnglishMaximumEntropyPosTagger(
        FileUtil.GetStreamingAssetFilePath(posTaggerModel),
        FileUtil.GetStreamingAssetFilePath(tagDict));

      tokens = tokenizer.Tokenize(sentence);
      posTags = posTagger.Tag(tokens);
    }
  }

.. image:: ../../Pictures~/POSTaggerExample.png
  :alt: TokenizerExample
