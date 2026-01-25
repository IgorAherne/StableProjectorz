namespace Voxell.NLP.Stem
{
  /// <summary>
  /// Stemmer is used to remove morphological affixes from words, leaving only the word stem.
  /// Stemming algorithms aim to remove those affixes leaving only the stem of the word.
  /// IStemmer defines a standard interface for stemmers.
  /// </summary>
  public interface IStemmer
  {
    /// <summary>
    /// Strip affixes from the token and return the stem.
    /// </summary>
    string Stem(string word);
  }
}
