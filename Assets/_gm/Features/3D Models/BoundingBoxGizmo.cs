using UnityEngine;

namespace spz {

	public class BoundingBoxGizmo : MonoBehaviour
	{
	    public Color gizmoColor = Color.green;
	    public bool useWireframe = true;

	    private Bounds bounds;

	    void Start()
	    {
	        // Calculate the bounds of the object
	        CalculateBounds();
	    }

	    void CalculateBounds()
	    {
	        // Initialize bounds with the current object's renderer
	        Renderer renderer = GetComponent<Renderer>();
	        if (renderer != null)
	        {
	            bounds = renderer.bounds;
	        }
	        else
	        {
	            bounds = new Bounds(transform.position, Vector3.zero);
	        }

	        // Include all child renderers
	        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
	        foreach (Renderer childRenderer in childRenderers)
	        {
	            bounds.Encapsulate(childRenderer.bounds);
	        }
	    }

	    void OnDrawGizmos()
	    {
	        // Ensure bounds are calculated in edit mode
	        if (bounds.size == Vector3.zero)
	        {
	            CalculateBounds();
	        }

	        // Set the color of the gizmo
	        Gizmos.color = gizmoColor;

	        if (useWireframe)
	        {
	            // Draw wireframe cube
	            Gizmos.DrawWireCube(bounds.center, bounds.size);
	        }
	        else
	        {
	            // Draw solid cube
	            Gizmos.DrawCube(bounds.center, bounds.size);
	        }
	    }
	}
}//end namespace
