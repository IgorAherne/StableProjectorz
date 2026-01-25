using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace spz {

	public class SP_ShaderBuilder : EditorWindow{
	    Object inputFileObject;
	    string outputFilePath = "";
	    List<TokenFilePair> tokenFilePairs = new List<TokenFilePair>();
	    SP_ShaderBuilderConfig config;
    
	    [MenuItem("Tools/Shader Builder")]
	    public static void ShowWindow(){
	        GetWindow<SP_ShaderBuilder>("Shader Builder").Show();
	    }

	    void OnEnable(){
	        LoadConfig();
	    }

	    void OnGUI(){
	        EditorGUI.BeginChangeCheck();
	        GUILayout.Label("Input Shader File:", EditorStyles.boldLabel);
	        inputFileObject = EditorGUILayout.ObjectField("File", inputFileObject, typeof(Object), false);
	        if (EditorGUI.EndChangeCheck()){
	            if (inputFileObject != null){
	                string inputFilePath = AssetDatabase.GetAssetPath(inputFileObject);
	                string directory = Path.GetDirectoryName(inputFilePath);
	                string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
	                string extension = Path.GetExtension(inputFilePath);
	                outputFilePath = Path.Combine(directory, fileName + "_Combined" + extension);
	                LoadConfig();
	            }else{
	                outputFilePath = "";
	            }
	        }

	        GUILayout.Label("Output Shader File:", EditorStyles.boldLabel);
	        outputFilePath = EditorGUILayout.TextField("File Path", outputFilePath);
	        GUILayout.Space(10);
	        GUILayout.Label("Token File Pairs:", EditorStyles.boldLabel);
	        for (int i = 0; i < tokenFilePairs.Count; i++){
	            EditorGUILayout.BeginHorizontal();
	            EditorGUILayout.LabelField("Token", GUILayout.Width(50));
	            tokenFilePairs[i].token = EditorGUILayout.TextField(tokenFilePairs[i].token);
	            EditorGUILayout.LabelField("File", GUILayout.Width(30));
	            tokenFilePairs[i].fileObject = EditorGUILayout.ObjectField(tokenFilePairs[i].fileObject, typeof(Object), false);
	            if (GUILayout.Button("-", GUILayout.Width(20))){
	                RemovePair(i);
	            }
	            EditorGUILayout.EndHorizontal();
	        }
	        if (GUILayout.Button("+")){
	            tokenFilePairs.Add(new TokenFilePair());
	        }
	        GUILayout.Space(10);
	        if (GUILayout.Button("Build Shader")){
	            BuildShader();
	        }
	        if (GUI.changed){
	            SaveConfig();
	        }
	    }

	    void LoadConfig(){
	        if (inputFileObject == null){ return; }

	        string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(inputFileObject));
	        string configPath = Path.Combine(directory, "ShaderBuilderConfig.asset");
	        config = AssetDatabase.LoadAssetAtPath<SP_ShaderBuilderConfig>(configPath);
	        if (config != null){
	            outputFilePath = config.outputFilePath;
	            tokenFilePairs = new List<TokenFilePair>(config.tokenFilePairs);
	        }else{
	            config = ScriptableObject.CreateInstance<SP_ShaderBuilderConfig>();
	            AssetDatabase.CreateAsset(config, configPath);
	            AssetDatabase.SaveAssets();
	        }
	    }

	    void SaveConfig(){
	        if (config == null){ return; }
	        config.outputFilePath = outputFilePath;
	        config.tokenFilePairs = new List<TokenFilePair>(tokenFilePairs);
	        EditorUtility.SetDirty(config);
	        AssetDatabase.SaveAssets();
	    }

	    void RemovePair(int index){
	        tokenFilePairs.RemoveAt(index);
	    }

	    void BuildShader(){
	        if (inputFileObject == null || string.IsNullOrEmpty(outputFilePath)){
	            Debug.LogError("Please specify input and output file paths.");
	            return;
	        }
	        string inputFilePath = AssetDatabase.GetAssetPath(inputFileObject);
	        if (string.IsNullOrEmpty(inputFilePath)){
	            Debug.LogError("Invalid input file.");
	            return;
	        }
	        string shaderContent = File.ReadAllText(inputFilePath);
	        // Add comment at the start of the output file
	        string comment = "//This file was automatically created via SP_ShaderBuilder editor script.\n" +
	                         "//It was made by combining several smaller files, to replace special tokens.\n\n";
	        shaderContent = comment + shaderContent;
	        // Perform token replacement
	        foreach (TokenFilePair pair in tokenFilePairs){
	            FindToken(pair, ref shaderContent);
	        }
	        // Write the final shader content to the output file
	        File.WriteAllText(outputFilePath, shaderContent);
	        AssetDatabase.Refresh();
	        Debug.Log("Shader built successfully.");
	    }


	    void FindToken(TokenFilePair pair, ref string shaderContent){
	        if (pair.fileObject == null){ return; }
	        string token = pair.token;

	        string cgincFilePath = AssetDatabase.GetAssetPath(pair.fileObject);
	        if (File.Exists(cgincFilePath)==false){
	            Debug.LogWarning("CGINC file not found: " + cgincFilePath);
	            return;
	        }
	        string cgincContent = File.ReadAllText(cgincFilePath);
	        int tokenIndex = shaderContent.IndexOf(token);
	        if (tokenIndex == -1){ return; }
                
	        int indentationLevel = DetermineIndentationLevel(shaderContent, tokenIndex);
	        string indentation = new string(' ', indentationLevel * 4);
	        cgincContent = IndentLines(cgincContent, indentation);
	        shaderContent = shaderContent.Replace(token, cgincContent);
	    }


	    int DetermineIndentationLevel(string content, int index){
	        int level = 0;
	        while (index > 0 && content[index - 1] == ' '){
	            index--;
	            level++;
	        }
	        return level / 4;
	    }

	    string IndentLines(string content, string indentation){
	        string[] lines = content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
	        bool isFirstNonEmptyLine = true;
	        for (int i = 0; i < lines.Length; i++){
	            if (string.IsNullOrWhiteSpace(lines[i])) { continue; }

	            if (isFirstNonEmptyLine){
	                lines[i] = lines[i].TrimStart();
	                isFirstNonEmptyLine = false;
	            }else{
	                lines[i] = indentation + lines[i];
	            }
	        }
	        return string.Join("\n", lines);
	    }
	}


	[System.Serializable]
	public class TokenFilePair{
	    public string token = "";
	    public Object fileObject;
	}
	[System.Serializable]
	public class SP_ShaderBuilderConfig : ScriptableObject{
	    public string outputFilePath = "";
	    public List<TokenFilePair> tokenFilePairs = new List<TokenFilePair>();
	}
}//end namespace
