/*
 * CherubNLP Library
 * Copyright (C) 2018 Haiping Chen
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Voxell.NLP.Stem
{
  /// <summary>
  /// A stemmer that uses regular expressions to identify morphological affixes.
  /// Any substrings that match the regular expressions will be removed.
  /// </summary>
  public class RegexStemmer : IStemmer
  {
    private string _pattern;
    private Regex _regex;
    private Dictionary<string, string> replacements = new Dictionary<string, string>();

    public void CreatePattern(string pattern=null)
    {
      if (string.IsNullOrEmpty(_pattern))
      {
        // replacements["nning"] = "n"; // running
        // replacements["pping"] = "p"; // skipping
        // replacements["tting"] = "t"; // putting
        // replacements["ing"] = "";
        replacements["am"] = "be";
        replacements["is"] = "be";
        replacements["are"] = "be";
        replacements["was"] = "be";
        replacements["were"] = "be";

        _pattern = string.Join("$|", replacements.Keys) + "$";
      }

      _regex = new Regex(_pattern);
    }

    public string Stem(string word)
    {
      Match match = _regex.Matches(word).Cast<Match>().FirstOrDefault();
      return match == null ? word : word.Substring(0, match.Index) + replacements[match.Value];
    }
  }
}
