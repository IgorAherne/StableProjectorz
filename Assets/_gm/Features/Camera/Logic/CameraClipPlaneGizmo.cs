using UnityEngine;

namespace spz {

	[RequireComponent(typeof(Camera))]
	public class CameraClipPlaneGizmos : MonoBehaviour
	{
	    private Camera cam;

	    Depth_UserCamera _depthCam;

	    void Awake(){
	        cam = GetComponent<Camera>();
	        _depthCam = GetComponent<Depth_UserCamera>();
	    }

	    void OnDrawGizmos()
	    {
	        if (cam == null) return;

	        float near = cam.nearClipPlane;
	        float far = cam.farClipPlane;
	        DrawPlane(near, Color.green);
	        DrawPlane(far, Color.red);
	    }

	    void DrawPlane(float distance, Color color)
	    {
	        Vector3 cameraCenter = cam.transform.position;
	        Vector3 cameraNormal = cam.transform.forward;

	        // Calculate the corners of the plane
	        Vector3 planeCenter = cameraCenter + cameraNormal * distance;
	        Vector3 topLeft = cam.ViewportToWorldPoint(new Vector3(0, 1, distance));
	        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, distance));
	        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, distance));
	        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1, 0, distance));

	        // Draw the plane
	        Gizmos.color = color;
	        Gizmos.DrawLine(topLeft, topRight);
	        Gizmos.DrawLine(topRight, bottomRight);
	        Gizmos.DrawLine(bottomRight, bottomLeft);
	        Gizmos.DrawLine(bottomLeft, topLeft);

	        // Optionally, draw a line from the camera to the plane center
	        Gizmos.DrawLine(cameraCenter, planeCenter);
	    }
	}
}//end namespace
