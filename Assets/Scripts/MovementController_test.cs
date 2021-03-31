using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController_test : MonoBehaviour
{
    float speed = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.eulerAngles += new Vector3(-mouseY * 2, mouseX * 2, 0);

        transform.position += new Vector3(horizontal, 0, vertical) * Time.deltaTime * speed;
    }
}
