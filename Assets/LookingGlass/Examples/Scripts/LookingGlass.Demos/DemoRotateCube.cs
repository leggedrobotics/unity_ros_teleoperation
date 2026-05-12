using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LookingGlass;

public class DemoRotateCube : MonoBehaviour
{
    public InterProcessCommunicator ipc;
    // Start is called before the first frame update
    void Start()
    {
        ipc.OnMessageReceived += ReceiveMessage;
    }

    public void rotateUp(){
        //+x
        transform.Rotate(new Vector3(200f,0f,0f) * Time.deltaTime);
    }

    public void rotateDown(){
        //-x
        transform.Rotate(new Vector3(-200f,0f,0f) * Time.deltaTime);
    }

    public void rotateLeft(){
        //+y
        transform.Rotate(new Vector3(0f,200f,0f) * Time.deltaTime);
    }

    public void rotateRight(){
        //-y
        transform.Rotate(new Vector3(0f,-200f,0f) * Time.deltaTime);
    }

    void ReceiveMessage(string message){
        //should use case switch tbh
        if(message == "RotateLeft"){
            rotateLeft();
            //Debug.Log("i ran");
        }
        if(message == "RotateRight"){
            rotateRight();
        }
        if(message == "RotateUp"){
            rotateUp();
        }
        if(message == "RotateDown"){
            rotateDown();
        }
        //Debug.Log("hi");
    }
}
