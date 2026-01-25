//Copyright (C) 2006 Richard J. Northedge
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

//This file is based on the JWNLDictionary.java source file found in the
//original java implementation of OpenNLP. That source file contains the following header:

//Copyright (C) 2003 Thomas Morton
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this program; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using SharpWordNet;
using System.Collections.Generic;

namespace Voxell.NLP.Coreference.Mention
{
  
  /// <summary> An implementation of the Dictionary interface using the JWNL library. </summary>
  public class JWNLDictionary
  {
    private WordNetEngine mEngine;
    private WordNetEngine.MorphologicalProcessOperation morphologicalProcess;
    private static string[] empty = new string[0];
    
    public JWNLDictionary(string searchDirectory)
    {
      mEngine = new DataFileEngine(searchDirectory);
      morphologicalProcess += mEngine.LookupExceptionsOperation;
      morphologicalProcess += mEngine.LookupIndexWordOperation;
    }
    
    public virtual string[] getLemmas(string word, string tag)
    {
      string pos;
      if (tag.StartsWith("N") || tag.StartsWith("n")) pos = "noun";
      else if (tag.StartsWith("N") || tag.StartsWith("v")) pos = "verb";
      else if (tag.StartsWith("J") || tag.StartsWith("a")) pos = "adjective";
      else if (tag.StartsWith("R") || tag.StartsWith("r")) pos = "adverb";
      else pos = "noun";

      return mEngine.GetBaseForms(word, pos, morphologicalProcess);
    }
    
    public virtual string getSenseKey(string lemma, string pos, int sense)
    {
      IndexWord indexWord = mEngine.GetIndexWord(lemma, "noun");
      if (indexWord == null) return null;
      return indexWord.SynsetOffsets[sense].ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    
    public virtual int getNumSenses(string lemma, string pos)
    {
      IndexWord indexWord = mEngine.GetIndexWord(lemma, "noun");
      if (indexWord == null) return 0;
      return indexWord.SenseCount;
    }

    private void getParents(Synset currentSynset, List<string> parentOffsets)
    {
      for (int currentRelation = 0;currentRelation < currentSynset.RelationCount;currentRelation++)
      {
        Relation relation = currentSynset.GetRelation(currentRelation);
        if (relation.SynsetRelationType.Name == "Hypernym")
        {
          Synset parentSynset = relation.TargetSynset;
          parentOffsets.Add(parentSynset.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture));
          getParents(parentSynset, parentOffsets);
        }
      }
    }

    public virtual string[] getParentSenseKeys(string lemma, string pos, int sense)
    {
      Synset[] synsets = mEngine.GetSynsets(lemma, "noun");
      if (synsets.Length > sense)
      {
          List<string> parents = new List<string>();
          getParents(synsets[sense], parents);
          return parents.ToArray();
      }
      else return empty;
    }
  }
}