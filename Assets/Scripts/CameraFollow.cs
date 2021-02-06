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
    public GameObject Player1;
    public GameObject Player2;
    private Vector3 middlePoint;
    private float distance;
    private float cameraSize = 6;
    private bool combinated;
    private bool player1active;
    private bool player2active;
    private void Start()
    {
        MainCamera = GetComponent<Camera>();
        cameraPos = camTransform.position;
        targetPos = Target1.position;
        combinated = false;
    }

     private void Update() {
         

        if(distance > 9.9f && !combinated){     
        MainCamera.orthographicSize = Mathf.Lerp(MainCamera.orthographicSize, 8.5f, 0.005f);
        } else {
            MainCamera.orthographicSize = Mathf.Lerp(MainCamera.orthographicSize, cameraSize, 0.005f);
        }

         middlePoint = (Target1.position + Target2.position) / 2;
        targetPos = Target1.position;

        if(Player1.activeInHierarchy == false && Player2.activeInHierarchy == true){

            combinated = true;
            middlePoint = Target2.position;
            cameraPos = new Vector3 (middlePoint.x, middlePoint.y + height, -20);
            transform.position = cameraPos;  

        }else {
            cameraPos = new Vector3 (middlePoint.x, middlePoint.y + height, -20);
            transform.position = cameraPos;  
        };

           if(Player2.activeInHierarchy == false && Player1.activeInHierarchy ==true){

            combinated = true;
            middlePoint = Target1.position;
            cameraPos = new Vector3 (middlePoint.x, middlePoint.y + height, -20);
            transform.position = cameraPos;  

        }else {
            cameraPos = new Vector3 (middlePoint.x, middlePoint.y + height, -20);
        transform.position = cameraPos;  

        };

        if (Player1.activeInHierarchy && Player2.activeInHierarchy){
             combinated = false;
        };

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