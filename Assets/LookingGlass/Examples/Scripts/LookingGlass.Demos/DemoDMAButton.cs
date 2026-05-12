using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LookingGlass;

public class DemoDMAButton : MonoBehaviour
{
    public InterProcessCommunicator ipc;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void rotateCubeUp(){
        ipc.SendData("RotateUp");
    }

    public void rotateCubeDown(){
        ipc.SendData("RotateDown");
    }
    
    public void rotateCubeLeft(){
        ipc.SendData("RotateLeft");
    }
    
    public void rotateCubeRight(){
        ipc.SendData("RotateRight");
    }
}
