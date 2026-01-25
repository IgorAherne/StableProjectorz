using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Voxell.Inspector
{
  public static class VXEditorUtil
  {
    public static readonly Color DefaultBackgroundColor = GUI.backgroundColor;
    public static readonly Color DefaultContentColor = GUI.contentColor;
    public static readonly float DefaultLabelWidth = EditorGUIUtility.labelWidth;
    public static readonly float DefaultFieldWidth = EditorGUIUtility.fieldWidth;

    public static IEnumerable<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate)
    {
      if (target == null)
      {
        Debug.LogError("The target object is null. Check for missing scripts.");
        yield break;
      }

      List<Type> types = GetSelfAndBaseTypes(target);

      for (int i = types.Count - 1; i >= 0; i--)
      {
        IEnumerable<MethodInfo> methodInfos = types[i]
          .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
          .Where(predicate);

        foreach (MethodInfo methodInfo in methodInfos)
        {
          yield return methodInfo;
        }
      }
    }

    /// <summary>
    ///		Get type and all base types of target, sorted as following:
    ///		<para />[target's type, base type, base's base type, ...]
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private static List<Type> GetSelfAndBaseTypes(object target)
    {
      List<Type> types = new List<Type>()
      {
        target.GetType()
      };

      while (types.Last().BaseType != null)
      {
        types.Add(types.Last().BaseType);
      }

      return types;
    }
  }
}