using System;
using System.Linq;
using UnityEngine;
 
public class GetClickPoint : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 currentPosition;
    public GameObject obj;
 
    void Start()
    {
        mainCamera = Camera.main;
    }
 
    void Update()
    {
        if (Input.GetMouseButton(0)) {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity)) {
                currentPosition.x = (float)Math.Round(hit.point.x / 2, MidpointRounding.ToEven) * 2f ;
                currentPosition.y = 1.45f ;
                currentPosition.z = (float)Math.Round( (hit.point.z + 0.25f) / 2, MidpointRounding.ToEven ) * 2f;
            }
            obj.transform.position = currentPosition;
            Debug.Log(currentPosition);
        }
    }
 
    
}