using UnityEditor;
using System.Reflection;

namespace Voxell.Inspector.Tools
{
  public static class CustomMenuItem
  {
    [MenuItem ("Shortcuts/Clear Console %#d")] // CTRL + SHIFT + D
    public static void ClearConsole()
    {
      Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
      System.Type type = assembly.GetType("UnityEditor.LogEntries");
      MethodInfo method = type.GetMethod("Clear");
      method.Invoke(new object(), null);
    }
  }
}