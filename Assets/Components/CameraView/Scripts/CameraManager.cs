using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CameraManager))]
public class CameraManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraManager myScript = (CameraManager)target;
        if (GUILayout.Button("Add Image"))
        {
            myScript.AddImage();
        }
        if (GUILayout.Button("Add Stereo"))
        {
            myScript.AddStereo();
        }
        if (GUILayout.Button("Track All"))
        {
            myScript.TrackAll();
        }
        if (GUILayout.Button("Clear All"))
        {
            myScript.ClearAll();
        }
        if (GUILayout.Button("Serialize"))
        {
            Debug.Log(myScript.Serialize());
        }
        if (GUILayout.Button("Deserialize"))
        {
            myScript.Deserialize(myScript.sData);
            // myScript.Deserialize("{\"data\":[\"{\"position\":{\"x\":0.4613938629627228,\"y\":0.7743633985519409,\"z\":-0.016683220863342286},\"rotation\":{\"x\":0.0,\"y\":0.0,\"z\":0.0,\"w\":1.0},\"scale\":{\"x\":0.010000000707805157,\"y\":0.009999999776482582,\"z\":0.010000000707805157},\"topicName\":\"\",\"tracking\":false,\"flip\":false,\"stereo\":false}\"]}");
        }
    }
}




#endif

[System.Serializable]
public struct ImageManagerData
{
    public string[] data;
}

public class CameraManager : MonoBehaviour
{
    public GameObject menu;
    public GameObject imagePrefab;
    public GameObject stereoPrefab;
    public TMPro.TextMeshProUGUI Count;
    public Sprite untracked;
    public Sprite tracked;
    private List<GameObject> imgs;
    private ROSConnection ros;
    private bool _allTracking = false;
    private Image _icon;

    public string sData;

    private void Start() {
        ros = ROSConnection.GetOrCreateInstance();
        menu.SetActive(false);
        imgs = new List<GameObject>();
        Count.text = imgs.Count.ToString();
        _icon = menu.transform.Find("Track/Image/Image").GetComponent<Image>();

        if (PlayerPrefs.HasKey("layout"))
        {
            Deserialize(PlayerPrefs.GetString("layout"));
        }

    }

    public void Remove(GameObject img) 
    {
        imgs.Remove(img);
        Count.text = imgs.Count.ToString();
        Destroy(img);
    }

    public void ToggleMenu()
    {
        menu.SetActive(!menu.activeSelf);
    }

    public void AddImage()
    {
        GameObject img = Instantiate(imagePrefab, transform.position + (transform.right * 0.5f), Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up));
        img.GetComponent<ImageView>().manager = this;
        img.GetComponent<ImageView>().ToggleTrack(_allTracking ? 1: 0);
        imgs.Add(img);
        Count.text = imgs.Count.ToString();
    }

    public void AddStereo()
    {
        GameObject img = Instantiate(stereoPrefab, transform.position + (transform.right * 0.5f), Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up));
        img.GetComponent<StereoStreamer>().manager = this;
        img.GetComponent<StereoStreamer>().ToggleTrack(_allTracking ? 1: 0);
        imgs.Add(img);
        Count.text = imgs.Count.ToString();
    }

    public void TrackAll()
    {
        _allTracking = !_allTracking;
        foreach (GameObject img in imgs)
        {
            img.GetComponent<ImageView>().ToggleTrack(_allTracking ? 1: 0);
        }
        _icon.sprite = _allTracking ? tracked : untracked;
    }

    public void ClearAll()
    {
        foreach (GameObject img in imgs)
        {
            Destroy(img);
        }
        imgs.Clear();
        Count.text = imgs.Count.ToString();
    }

    void OnApplicationQuit()
    {
        string layout = Serialize();
        PlayerPrefs.SetString("layout", layout);
        PlayerPrefs.Save();
    }

    public string Serialize()
    {
        ImageManagerData data = new ImageManagerData();
        data.data = new string[imgs.Count];

        for (int i = 0; i < imgs.Count; i++)
        {
            data.data[i] = imgs[i].GetComponent<ImageView>().Serialize();
        }

        sData = JsonUtility.ToJson(data);

        return JsonUtility.ToJson(data);
    }

    public void Deserialize(string data)
    {

        ClearAll();

        ImageManagerData imgData = JsonUtility.FromJson<ImageManagerData>(data);

        foreach (string d in imgData.data)
        {
            ImageData img = JsonUtility.FromJson<ImageData>(d);
            if (img.stereo)
            {
                GameObject stereo = Instantiate(stereoPrefab, img.position, img.rotation);
                stereo.transform.localScale = img.scale;
                stereo.GetComponent<StereoStreamer>().Deserialize(d);
                stereo.GetComponent<StereoStreamer>().manager = this;
                // stereo.GetComponent<StereoStreamer>()._tracking = _allTracking;
                imgs.Add(stereo);
            } else {
                GameObject image = Instantiate(imagePrefab, img.position, img.rotation);
                image.transform.localScale = img.scale;
                image.GetComponent<ImageView>().Deserialize(d);
                image.GetComponent<ImageView>().manager = this;
                // image.GetComponent<ImageView>()._tracking = _allTracking;
                imgs.Add(image);
            }                
        }
        Count.text = imgs.Count.ToString();
    }
}

