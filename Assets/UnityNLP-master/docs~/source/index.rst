UnityNLP
~~~~~~~~

Introduction
------------

This package references 2 repositories, `OpenNLP <https://github.com/AlexPoint/OpenNlp/>`_ and `CherubNLP <https://github.com/SciSharp/CherubNLP>`_. UnityNLP is a collection of natural language processing tools written in C# that is targeted towards the Unity Engine. Currently it provides the following NLP tools:

- tokenizer
- sentence splitter
- part-of-speech tagger
- chunker (used to "find non-recursive syntactic annotations such as noun phrase chunks")
- parser
- name finder
- coreference tool
- interface to the WordNet lexical database
- topical classifier

Installation
------------

This package depends on the `UnityUtil <https://github.com/voxell-tech/UnityUtil>`_ package and the `UnityAI <https://github.com/voxell-tech/UnityAI>`_ package.

1. Clone the `UnityUtil <https://github.com/voxell-tech/UnityUtil>`_ repository into your ``Packages`` folder.
2. Clone the `UnityAI <https://github.com/voxell-tech/UnityAI>`_ repository into your ``Packages`` folder.
3. Clone this repository into your Packages folder.
4. Download all `essential models <https://drive.google.com/file/d/19bD2h0LBIArczYtQMHuoqdNRuUZrWdOX/view?usp=sharing>`_ and import them into the project.
5. Place the models in the StreamingAssets folder.
6. And you are ready to go!

.. toctree::
  :maxdepth: 2
  :caption: Contents:

  tokenizer
  sentence_splitter
  pos_tagger
  named_entity_recognition
  classifier


.. Indices and tables
.. ==================

.. * :ref:`genindex`
.. * :ref:`modindex`
.. * :ref:`search`
