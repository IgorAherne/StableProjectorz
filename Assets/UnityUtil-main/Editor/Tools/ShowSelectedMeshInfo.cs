using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Voxell.Inspector.Tools
{
  public class ShowSelectedMeshInfo : EditorWindow
  {
    private bool selectionChanged = false;

    private int _totalMeshCount = 0;
    private int _totalVertexCount = 0;
    private int _totalSubMeshCount = 0;
    private uint _totalTriCount = 0;

    private MeshFilter[] _meshFilters;
    private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    private List<Mesh> _meshes;
    private List<Component> _components;

    private int[] _vertexCounts;
    private int[] _subMeshCounts;
    private uint[][] _triCounts;

    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("Tools/Voxell/Mesh Info")]
    public static void ShowWindow()
    {
      EditorWindow window = GetWindow(typeof(ShowSelectedMeshInfo));
      window.titleContent = new GUIContent("Mesh Info");
    }

    void OnEnable() => selectionChanged = true;

    void OnGUI()
    {
      GameObject[] selections = Selection.gameObjects;

      // if have selection
      if (selections.Length == 0)
      {
        EditorGUILayout.LabelField("Select gameobject from scene or hierarchy..");
      } else
      {
        string selectedLabels = "[";
        for (int s=0; s < selections.Length-1; s++)
          selectedLabels += selections[s].name + ", ";
        selectedLabels += selections[selections.Length-1].name + "]";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selections: ", EditorStyles.boldLabel, GUILayout.Width(70.0f));
        EditorGUILayout.LabelField(selectedLabels);
        EditorGUILayout.EndHorizontal();

        // update mesh info only if selection changed
        if (selectionChanged)
        {
          selectionChanged = false;

          _totalMeshCount = 0;
          _totalVertexCount = 0;
          _totalSubMeshCount = 0;
          _totalTriCount = 0;

          _meshes = new List<Mesh>();
          _components = new List<Component>();

          for (int s=0; s < selections.Length; s++)
          {
            GameObject selection = selections[s];
            // check all _meshFilters
            _meshFilters = selection.GetComponentsInChildren<MeshFilter>();
            for (int mf=0, length=_meshFilters.Length; mf < length; mf++)
            {
              if (!_meshes.Contains(_meshFilters[mf].sharedMesh))
              {
                _meshes.Add(_meshFilters[mf].sharedMesh);
                _components.Add(_meshFilters[mf]);
              }
            }

            // check all skinned mesh renderers
            _skinnedMeshRenderers = selection.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int sm=0, length=_skinnedMeshRenderers.Length; sm < length; sm++)
            {
              if (!_meshes.Contains(_skinnedMeshRenderers[sm].sharedMesh))
              {
                _meshes.Add(_skinnedMeshRenderers[sm].sharedMesh);
                _components.Add(_skinnedMeshRenderers[sm]);
              }
            }
          }

          _totalMeshCount = _meshes.Count;
          _vertexCounts = new int[_meshes.Count];
          _subMeshCounts = new int[_meshes.Count];
          _triCounts = new uint[_meshes.Count][];
          for (int m=0; m < _totalMeshCount ; m++)
          {
            _vertexCounts[m] = _meshes[m].vertexCount;
            _subMeshCounts[m] = _meshes[m].subMeshCount;
            _triCounts[m] = new uint[_subMeshCounts[m]];

            _totalVertexCount += _vertexCounts[m];
            _totalSubMeshCount += _subMeshCounts[m];
            for (int sub=0; sub < _subMeshCounts[m]; sub++)
            {
              _triCounts[m][sub] = _meshes[m].GetIndexCount(sub) / 3;
              _totalTriCount += _triCounts[m][sub];
            }
          }
        }

        // display stats
        EditorGUILayout.LabelField($"Mesh Count: {_totalMeshCount}");
        EditorGUILayout.LabelField($"Total Vertex Count: {_totalVertexCount}");
        EditorGUILayout.LabelField($"Total Sub Mesh Count: {_totalSubMeshCount}");
        EditorGUILayout.LabelField($"Total Triangle Count: {_totalTriCount}");
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height - 5 * 22.0f));
        for (int m=0; m < _totalMeshCount; m++)
        {
          EditorGUILayout.BeginVertical(VXEditorStyles.box);

          EditorGUILayout.BeginHorizontal();

          // ping buttons
          if (GUILayout.Button(new GUIContent("M", "Ping Mesh in Project"), GUILayout.Width(22.0f)))
            EditorGUIUtility.PingObject(_meshes[m]);

          if (GUILayout.Button(new GUIContent("G", "Ping GameObject in Hierarchy"), GUILayout.Width(22.0f)))
            EditorGUIUtility.PingObject(_components[m]);

          EditorGUILayout.LabelField(_meshes[m].name, EditorStyles.boldLabel);
          EditorGUILayout.EndHorizontal();

          EditorGUILayout.LabelField($"Vertex Count: {_vertexCounts[m]}");
          EditorGUILayout.LabelField($"Sub Mesh Count: {_subMeshCounts[m]}");
          ReorderableList triCountList = new ReorderableList(_triCounts[m], typeof(uint), true, true, false, false);
          triCountList.drawHeaderCallback = (Rect rect) =>
          {
            EditorGUI.indentLevel += 1;
            EditorGUI.LabelField(rect, $"Triangle Counts");
            EditorGUI.indentLevel -= 1;
          };

          triCountList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            EditorGUI.LabelField(rect, $"Sub Mesh #{index}: {_triCounts[m][index]}");

          triCountList.DoLayoutList();

          EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
      }
    }

    void OnSelectionChange()
    {
      selectionChanged = true;
      // force redraw window
      Repaint();
    }
  }
}