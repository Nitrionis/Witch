using UnityEngine;

public class RotationTest : MonoBehaviour
{
    private static float accdelta = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 1);
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.blue;
        var originalVector = Vector3.forward;
        Gizmos.DrawRay(transform.position, originalVector);
        
        var delta = accdelta += 6.0f * Time.deltaTime;
        var rotationVector = Quaternion.AngleAxis(delta, Vector3.up) * originalVector;
        
        var rotationPerp = Vector3.Cross(rotationVector, Vector3.up);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, rotationPerp);
        
        var rotatedVector = rotationVector * originalVector.x + rotationPerp * originalVector.z;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, rotatedVector);
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, rotationVector * 0.5f);
    }
}
