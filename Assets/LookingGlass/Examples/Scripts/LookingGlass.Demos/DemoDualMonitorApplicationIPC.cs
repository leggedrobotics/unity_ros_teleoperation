using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LookingGlass.DualMonitorApplication;

namespace LookingGlass.Demos {
    public class DemoDualMonitorApplicationIPC : DualMonitorApplicationBaseIPC {

        /**********/
        /* fields */
        /**********/

        [Header("Demo IPC")]
        // just an example cube we will rotate using IPC from Foldout to LKG
        public Transform cubeTransform;
        // another example text we will set using IPC from LKG back to Foldout
        public Text rotationText;

        /***********/
        /* methods */
        /***********/

        public override void Update() {
            // run base update
            base.Update();
            // send cube rotation data if it's the looking glass
            if (display == DualMonitorApplicationDisplay.LookingGlass) {
                ipc.SendData("rotationVectorData:" + HologramCamera.Instance.transform.eulerAngles.ToString());
            }
        }

        // function to send the rotate cube message
        public void RandomizeCubeRotation() {
            ipc.SendData("randomizeCubeRotation");
            Debug.Log(gameObject.name + " sent: randomizeCubeRotation");
        }
        
        // this is automatically subscribed to the IPC in the base class
        public override void ReceiveMessage(string message) {

            // inherit the original sendmessage (so the quit functionality remains)
            base.ReceiveMessage(message);

            // do what you want with the message!
            switch (message) {
                case "randomizeCubeRotation":
                    cubeTransform.rotation = Random.rotation;
                    break;
                
                default:
                    if (message.Contains("rotationVectorData:")) {
                        // get just the vector
                        string vecData = message.Substring("rotationVectorData:".Length);
                        rotationText.text = "View Rotation:\n" + vecData;
                    }
                    break;
            }
        }
    }
}