using SharpWordNet;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Voxell.Inspector;
using Voxell.NLP.PosTagger;
using Voxell.NLP.Tokenize;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace spz {

	public class SD_Prompt_NounHighlighter : MonoBehaviour
	{
	    public static SD_Prompt_NounHighlighter instance { get; private set; } = null;

	    [FormerlySerializedAs("_color_positive")][SerializeField] Color _colorPositive;
	    [SerializeField] Color _colorPositive_lora;
	    [Space(10)]
	    [FormerlySerializedAs("_color_negative")][SerializeField] Color _colorNegative;
	    [SerializeField] Color _colorNegative_lora;
	    [Space(10)]
	    [StreamingAssetFilePath] public string tokenizerModelFileName;
	    [StreamingAssetFilePath] public string posTaggerModelFileName;
	    [StreamingAssetFilePath] public string tagDictFileName;
	    private EnglishMaximumEntropyTokenizer tokenizer;
	    private EnglishMaximumEntropyPosTagger posTagger;

	    void Awake(){
	        if (instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }

	    void Start(){
	        string tokenizerModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, tokenizerModelFileName);
	        string posTaggerModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, posTaggerModelFileName);
	        string tagDictPath = System.IO.Path.Combine(Application.streamingAssetsPath, tagDictFileName);
	        tokenizer = new EnglishMaximumEntropyTokenizer(tokenizerModelPath);
	        posTagger = new EnglishMaximumEntropyPosTagger(posTaggerModelPath, tagDictPath);
	    }

	    public string HighlightNouns(string prompt, bool isPositivePrompt)
	    {
	        if(string.IsNullOrWhiteSpace(prompt)){ return prompt; }
	        if(Settings_MGR.instance.get_prompt_textHighlight() == false){ return prompt; }

	        Color col = isPositivePrompt? _colorPositive : _colorNegative;
	        string hex = ColorUtility.ToHtmlStringRGB(col);

	        // Tokenize the input
	        var tokens = new string(prompt.SelectMany((c, i) =>
	            char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) ? new[] { c }
	            : new[] { ' ', c, ' ' })
	        .ToArray()).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

	        // Tag the tokens
	        var lowTokens = tokens.Select(t => t.ToLowerInvariant()).ToArray();
	        var tags = posTagger.Tag(lowTokens);


	        StringBuilder highlightedText = new StringBuilder();

	        int ix = prompt.IndexOf(tokens[0]);//in case if there is white space at the start: " a door"
	        highlightedText.Append(' ', ix);

	        for (int i=0; i<tokens.Length; i++){
	            // Highlight nouns
	            // https://www.ling.upenn.edu/courses/Fall_2003/ling001/penn_treebank_pos.html
	            if (lowTokens[i].Length==1 && char.IsPunctuation(lowTokens[i][0])){
	                highlightedText.Append(tokens[i]);
	            }else if(isColorableNoun( tags[i], tokens[i]) ){
	                highlightedText.Append($"<color=#{hex}>{tokens[i]}</color>");
	            }else{
	                highlightedText.Append(tokens[i]);
	            }
	            ix += tokens[i].Length;
            
	            for( ; ix<prompt.Length; ix++){
	                if(char.IsWhiteSpace(prompt[ix])==false && char.IsSeparator(prompt[ix])==false){ break; }
	                highlightedText.Append(prompt[ix]);
	            }
	        }

	        string resultText = highlightedText.ToString();
	        Color_the_Loras(ref resultText, isPositivePrompt);
	        return resultText;
	    }


	    bool isColorableNoun(string tag, string token){
	        //to allow <lora:myLora.safetensors:1.2> etc.
	        bool isSpecialWord  =  token == "<"  ||  token == ">"  || token=="." || token==":";
	             isSpecialWord |=  token == "lora" || token=="safetensors" || token==".pth";

	        return tag.StartsWith("NN")  &&  !isSpecialWord;
	    }


	    void Color_the_Loras(ref string resultText, bool isPositivePrompt){
	        // Define the color code for the lora expressions
	        Color loraColor = isPositivePrompt ? _colorPositive_lora : _colorNegative_lora;
	        string loraHex = ColorUtility.ToHtmlStringRGB(loraColor);

	        int index = 0;
	        while (index < resultText.Length){
	            int loraStart = resultText.IndexOf("<lora:", index, StringComparison.OrdinalIgnoreCase);
	            if (loraStart == -1){ break; }// No more lora expressions

	            int loraEnd = FindClosingAngleBracket(resultText, loraStart);
	            if (loraEnd == -1){ break; }// Malformed lora expression, no closing '>'

	            // Extract the lora expression
	            int length = loraEnd - loraStart + 1;
	            string loraExpression = resultText.Substring(loraStart, length);

	            // Remove any color tags inside the lora expression
	            string cleanedExpression = Regex.Replace(loraExpression, @"<\/?color[^>]*?>", "");

	            // Wrap with the lora color tag
	            string coloredExpression = $"<color=#{loraHex}>{cleanedExpression}</color>";

	            // Replace in the resultText
	            resultText = resultText.Substring(0, loraStart) + coloredExpression + resultText.Substring(loraEnd + 1);

	            // Move index past this lora expression
	            index = loraStart + coloredExpression.Length;
	        }
	    }

	    int FindClosingAngleBracket(string text, int startIndex){
	        int depth = 0;
	        for (int i=startIndex; i<text.Length; i++){
	            if (text[i] == '<'){  
	                depth++;
	                continue;
	            }
	            if (text[i] == '>'){
	                depth--;
	                if(depth == 0){ return i; }
	            }
	        }
	        return -1; // No matching closing '>'
	    }
	}
}//end namespace
