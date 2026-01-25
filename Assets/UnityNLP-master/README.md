# Unity NLP

This package references 2 repositories, [OpenNLP](https://github.com/AlexPoint/OpenNlp) and [CherubNLP](https://github.com/SciSharp/CherubNLP). UnityNLP is a collection of natural language processing tools written in C# that is targeted towards the Unity Engine. Currently it provides the following NLP tools:

- [x] tokenizer
- [x] sentence splitter
- [x] part-of-speech tagger
- [x] chunker (used to "find non-recursive syntactic annotations such as noun phrase chunks")
- [x] parser
- [x] name finder
- [x] coreference tool
- [x] interface to the WordNet lexical database
- [x] topical classifier

You can find the documentation at: https://unitynlp.readthedocs.io/

## Table of contents
- [Unity NLP](#unity-nlp)
  - [Table of contents](#table-of-contents)
  - [Installation](#installation)
  - [Examples](#examples)
    - [Tokenizer](#tokenizer)
    - [Sentence Splitter](#sentence-splitter)
    - [Part of Speech](#part-of-speech)
    - [Named Entity Recognition](#named-entity-recognition)
    - [Multinomial Naive Bayes Classifier](#multinomial-naive-bayes-classifier)
  - [Support the project!](#support-the-project)
  - [Join the community!](#join-the-community)
  - [License](#license)
  - [References](#references)

## Installation

External dependencies:

- VX Util ([UnityUtil](https://github.com/voxell-tech/UnityUtil))
- VX AI ([UnityAI](https://github.com/voxell-tech/UnityAI))

1. Clone the [UnityUtil](https://github.com/voxell-tech/UnityUtil) repository into your `Packages` folder.
2. Clone the [UnityAI](https://github.com/voxell-tech/UnityAI) repository into your `Packages` folder.
3. Clone this repository into your `Packages` folder.
4. Download all [essential models](https://drive.google.com/file/d/19bD2h0LBIArczYtQMHuoqdNRuUZrWdOX/view?usp=sharing) and import them into the project.
5. Place the models in the StreamingAssets folder.
6. And you are ready to go!

*Make a feature request in the issues tab if you think there is something missing or if you have new ideas!*

## Examples

All the sample code are in the `Samples~` folder which can be imported into unity from the package manager.

### Tokenizer

![Tokenize](Pictures~/TokenizerExample.png)

### Sentence Splitter

![SentenceSplitter](Pictures~/SentenceSplitterExample.png)

### Part of Speech

For the full list of part of speech abbreviations, please refer to the [Penn Treebank Project](https://www.ling.upenn.edu/courses/Fall_2003/ling001/penn_treebank_pos.html).

![POS](Pictures~/POSTaggerExample.png)

### Named Entity Recognition

![NER](Pictures~/NamedEntityRecognitionExample.png)

### Multinomial Naive Bayes Classifier

![NBClassifier](Pictures~/MultinomialNaiveBayesClassifierExample.png)

## Support the project!

<a href="https://www.patreon.com/voxelltech" target="_blank">
  <img src="https://teaprincesschronicles.files.wordpress.com/2020/03/support-me-on-patreon.png" alt="patreon" width="200px" height="56px"/>
</a>

<a href ="https://ko-fi.com/voxelltech" target="_blank">
  <img src="https://uploads-ssl.webflow.com/5c14e387dab576fe667689cf/5cbed8a4cf61eceb26012821_SupportMe_red.png" alt="kofi" width="200px" height="40px"/>
</a>

## Join the community!

<a href ="https://discord.gg/WDBnuNH" target="_blank">
  <img src="https://gist.githubusercontent.com/nixon-voxell/e7ba303906080ffdf65b106f684801b5/raw/65b0338d5f4e82f700d3c9f14ec9fc62f3fd278e/JoinVXDiscord.svg" alt="discord" width="200px" height="200px"/>
</a>


## License

This repository as a whole is licensed under the Apache License 2.0. Individual files may have a different, but compatible license.

See [license file](./LICENSE) for details.

## References

- [SharpNLP archive](https://archive.codeplex.com/?p=sharpnlp)
- [OpenNLP](https://github.com/AlexPoint/OpenNlp)
- [CherubNLP](https://github.com/SciSharp/CherubNLP)
