using UnityEngine;
 
public class CameraFollow : MonoBehaviour
{

    public Camera MainCamera;
    public Transform camTransform;
    public Transform Target1;
    public Transform Target2;
    private Vector3 cameraPos;
    private Vector3 targetPos;
    private Vector3 middlePos;
    public float height = 0;

    private Vector3 middlePoint;

    private float distance;
    private float cameraSize = 4.88f;



    private void Start()
    {
        MainCamera = GetComponent<Camera>();
        cameraPos = camTransform.position;
        targetPos = Target1.position;  
    }

     private void Update() {
         

        if(distance > 16.9f){     
        MainCamera.orthographicSize = Mathf.Lerp(MainCamera.orthographicSize, 8.5f, 0.1f);
        } else {
            MainCamera.orthographicSize = Mathf.Lerp(MainCamera.orthographicSize, cameraSize, 0.1f);
        }


         middlePoint = (Target1.position + Target2.position) / 2;
        targetPos = Target1.position;
        cameraPos = new Vector3 (middlePoint.x, middlePoint.y + height, -20);
        transform.position = cameraPos;  

        //Calcular distancia entre los objetos
        distance = Vector3.Distance(Target1.position, Target2.position);
        //Debug.Log(distance);

    }

     private void OnDrawGizmos() {
         Gizmos.color = Color.red;
         Gizmos.DrawLine(Target1.position, Target2.position);
         Gizmos.DrawSphere(middlePoint, 0.3f);

    }


}