using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.NerfTeleoperation;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(NerfRender))]
public class NerfRenderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NerfRender nerfRender = (NerfRender)target;
        if(GUILayout.Button("Render"))
        {
            nerfRender.Render();
        }
    }
}
#endif

public class NerfRender : MonoBehaviour
{
    public enum RenderMode: byte
    {
        Continuous = 1,
        Single = 0,
        VR = 3
    }
    ROSConnection ros;
    public string renderTopicName = "/nerf_render";
    public string baseFrame = "odom";

    public TextMeshProUGUI stepText;
    public TextMeshProUGUI resText;
    public TextMeshProUGUI lossText;
    public TextMeshProUGUI resolutionText;

    public Transform rootFrame;

    public float fovFactor = 2f;
    public float aspectRatio = 2f;

    public int ID;
    public bool trackProgress = true;

    public float resolution = 0.5f;
    private Material _material;
    private byte _mode;
    public RenderMode mode;


    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<NerfRenderRequestActionFeedback>(renderTopicName+"/feedback", RenderFbCallback);
        ros.Subscribe<NerfRenderRequestActionResult>(renderTopicName+"/result", RenderCallback);
        if(trackProgress)
        {
            ros.Subscribe<UInt16Msg>("nerf_step", NerfStepCallback);
            ros.Subscribe<Float32Msg>("nerf_loss", NerfLossCallback);
        }
        ros.RegisterPublisher<NerfRenderRequestActionGoal>(renderTopicName+"/goal");

        ID = gameObject.GetInstanceID();

        _material = GetComponent<Renderer>().material;
    }

    void NerfStepCallback(UInt16Msg step){
        stepText.SetText("Step: " + step.data);
    }

    void NerfLossCallback(Float32Msg loss){

        lossText.SetText("Loss: " + loss.data.ToString("0.000"));
    }

    void RenderFbCallback(NerfRenderRequestActionFeedback fb){
        if(fb.feedback.client_id != ID) return;
        

        if (resText != null)
        {
            float res = fb.feedback.resolution;
            resText.SetText((100*res).ToString("0.00") + "%");
        }
        // get rgb image
        CompressedImageMsg rgbImage = fb.feedback.rgb_image;
        Texture2D rgbTexture = _material.GetTexture("_RGB") as Texture2D ?? new Texture2D(1, 1, TextureFormat.BGRA32, false);
        
        if(_material.GetTexture("_RGB") == null ){
            _material.SetTexture("_RGB", rgbTexture);
        }
        ImageConversion.LoadImage(rgbTexture, rgbImage.data);
        

        // get depth image
        ImageMsg depthImage = fb.feedback.depth_image;

        int depthWidth = (int) depthImage.width;
        int depthHeight = (int) depthImage.height;

        Texture2D depthTexture = _material.GetTexture("_Depth") as Texture2D ?? new Texture2D(depthWidth, depthHeight, TextureFormat.RFloat, false);

        if(_material.GetTexture("_Depth") == null || depthTexture.width != depthWidth || depthTexture.height != depthHeight){
            _material.SetTexture("_Depth", depthTexture);
        } 
        depthTexture.LoadRawTextureData(depthImage.data);
        depthTexture.Apply();
    }

    void RenderCallback(NerfRenderRequestActionResult res){
        if(res.result.client_id != ID) return;

        CompressedImageMsg rgbImage = res.result.rgb_image;
        Texture2D rgbTexture = _material.GetTexture("_RGB") as Texture2D ?? new Texture2D(1, 1, TextureFormat.BGRA32, false);

        if(_material.GetTexture("_RGB") == null ){
            _material.SetTexture("_RGB", rgbTexture);
        }
        ImageConversion.LoadImage(rgbTexture, rgbImage.data);

        ImageMsg depthImage = res.result.depth_image;

        int depthWidth = (int) depthImage.width;
        int depthHeight = (int) depthImage.height;

        Texture2D depthTexture = _material.GetTexture("_Depth") as Texture2D ?? new Texture2D(depthWidth, depthHeight, TextureFormat.RFloat, false);

        if(_material.GetTexture("_Depth") == null || depthTexture.width != depthWidth || depthTexture.height != depthHeight){
            _material.SetTexture("_Depth", depthTexture);
        }

        depthTexture.LoadRawTextureData(depthImage.data);
        depthTexture.Apply();

    }

    // Update is called once per frame
    public void Render()
    {
        NerfRenderRequestGoal goal = new NerfRenderRequestGoal();
        PoseMsg cameraPose = new PoseMsg();

        int pow = mode == RenderMode.VR ? 9 : 8;

        goal.height = (short) Mathf.RoundToInt(Mathf.Pow(2,pow) * resolution);
        goal.width = (short) Mathf.RoundToInt(goal.height * aspectRatio);
        goal.mode = (byte) mode;
        goal.resolution = 1;
        goal.fov_factor = fovFactor;
        goal.frame_id = baseFrame;
        goal.box_size = 0f;
        goal.client_id = ID;

        Vector3 cameraPosition = transform.position - rootFrame.position;

        cameraPose.position = cameraPosition.To<FLU>();

        Quaternion cameraRotation = transform.rotation * Quaternion.Inverse(rootFrame.rotation);
        cameraRotation *= Quaternion.Euler(0,-90,-90);
        cameraPose.orientation = cameraRotation.To<FLU>();

        goal.pose = cameraPose;

        NerfRenderRequestActionGoal action = new NerfRenderRequestActionGoal();
        action.goal = goal;

        ros.Publish(renderTopicName+"/goal", action);
        
    }

    public void UpdateResolution(float res)
    {
        resolution = res;
        if(resolutionText != null)
        resolutionText.SetText((100*res).ToString("0.00") + "%");
    }

    public void ToggleContinuous(bool value)
    {
        // mode = (byte) (value ? 1 : 0);
        Debug.Log("Continuous rendering currently disabled" );
    }
}
