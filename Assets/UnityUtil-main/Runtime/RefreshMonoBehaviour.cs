using UnityEngine;

namespace Voxell
{
  [ExecuteAlways]
  public abstract class RefreshMonoBehaviour : MonoBehaviour
  {
    #if UNITY_EDITOR
    [HideInInspector] public bool _refresh = true;
    #endif
    public virtual void OnDrawGizmos()
    {
      #if UNITY_EDITOR
      if (_refresh)
      {
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
      }
      #endif
    }
  }
}
