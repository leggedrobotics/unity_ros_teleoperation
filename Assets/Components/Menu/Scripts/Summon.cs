using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Summon : MonoBehaviour
{
    public InputActionReference summonAction;
    public InputActionReference handActiveAction;
    public InputActionReference palmRotationAction;
    public InputActionReference palmPositionAction;

    // public GameObject palm;
    public bool active;
    public bool reversed = false;
    public bool useHand = true;
    public float speed = 1f;
    public Vector3 offset = new Vector3(0,0.5f,0);

    // Update is called once per frame
    void Update()
    {
        float angle;
        Vector3 palm;
        // check if the right hand is tracked, if not use controller input only
        if (useHand && handActiveAction.action.ReadValue<float>() > .5f)
        {
            angle = 180 - Quaternion.Angle(palmRotationAction.action.ReadValue<Quaternion>() , Quaternion.Euler(Vector3.up));
            palm = palmPositionAction.action.ReadValue<Vector3>();
            palm += offset;
        }
        else
        {
            angle = 180;
            palm = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
        }
        
        Vector3 diff = palm - transform.position;
        

        // apply offset to palm

        if((angle < 5 || summonAction.action.ReadValue<float>()>0.5f ) && diff.magnitude > 0.15f)
        {
            transform.position += diff * Time.deltaTime * speed / diff.magnitude;
            // rotate over time to face forward
            if (reversed)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(palm - Camera.main.transform.position + offset / 2), 180);
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - palm + offset / 2), 360 * Time.deltaTime);
            }
        }
    }
}
