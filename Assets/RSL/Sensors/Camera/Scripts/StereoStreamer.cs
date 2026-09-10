using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using UvgRos;

namespace RSL.Sensors.Camera
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(StereoStreamer))]
    public class StereoStreamerEditor : ImageViewEditor
    { }
    #endif


    public class StereoStreamer : ImageView
    {

        private Texture2D _leftTexture2D;
        private Texture2D _rightTexture2D;

        // Off-thread JPEG decode, one worker per eye -- see JpegDecodeWorker's
        // own docstring and ImageView.OnCompressed for why this exists.
        [SerializeField] private bool _flipDecodedJpeg = true;
        private JpegDecodeWorker _leftDecodeWorker;
        private JpegDecodeWorker _rightDecodeWorker;

        /// <summary>
        /// Updates the list of the available topics in the dropdown menu
        /// </summary>
        /// <param name="topics">Topics from the ROS TCP function</param>
        protected override void UpdateTopics(Dictionary<string, UvgRos.TopicListEntry> topics)
        {
            List<string> options = new List<string>();
            options.Add("None");
            foreach (var topic in topics)
            {
                if (!IsViewable(topic.Value)) continue; // e.g. a video-transport topic (encoded_video)
                if (topic.Value.MsgType != "sensor_msgs/Image" && topic.Value.MsgType != "sensor_msgs/CompressedImage") continue;

                // issue with depth images at the moment
                if (topic.Key.ToLowerInvariant().Contains("left"))
                    options.Add(topic.Key);
            }

            if (options.Count == 1)
            {
                Debug.LogWarning("No image topics found!");
                return;
            }
            topicDropdown.ClearOptions();
            topicDropdown.AddOptions(options);
            topicDropdown.value = Mathf.Min(_lastSelected, options.Count - 1);

        }

        /// <summary>
        /// Flips the image horizontally, NOTE: Not yet implemented for stereo images.... (might need to swap left and right as well...)
        /// </summary>
        public void Flip()
        {
            Debug.Log("Flip not yet implemented");
        }


        public override void OnSelect(int value)
        {
            if (value == _lastSelected) return;

            _lastSelected = value;

            // OnTopicChange itself now releases whatever topicName was
            // previously subscribed -- no need to duplicate that here.
            string selectedTopic = topicDropdown.options[value].text;

            if (selectedTopic == "None")
                selectedTopic = null;

            OnTopicChange(selectedTopic);
        }

        public override void OnTopicChange(string topic)
        {
            nameText.text = topic;

            // See ImageView.OnTopicChange's identical call for why -- a
            // decode already in flight for the previous topic must not be
            // allowed to surface as this topic's first frame.
            _leftDecodeWorker?.Reset();
            _rightDecodeWorker?.Reset();

            // Unconditionally, here rather than in OnSelect only -- see
            // ImageView.OnTopicChange's identical call for why (this method
            // is also reachable from Deserialize, which never unsubscribed
            // the previous topic itself either).
            if (topicName != null)
            {
                _ros.Unsubscribe(topicName);
                _ros.Unsubscribe(topicName.Replace("left", "right"));
            }

            if (string.IsNullOrEmpty(topic))
            {
                topicName = null;
                // set texture to grey
                _leftTexture2D = new Texture2D(3, 2, TextureFormat.RGBA32, false);
                material.SetTexture("_LeftTex", _leftTexture2D);

                _rightTexture2D = new Texture2D(3, 2, TextureFormat.RGBA32, false);
                material.SetTexture("_RightTex", _rightTexture2D);

                topicDropdown.gameObject.SetActive(false);
                topMenu.SetActive(false);
                return;
            }

            topicName = topic;

            if (topicName.EndsWith("compressed"))
            {
                _ros.Subscribe<CompressedImageMsg>(topicName, OnCompressedLeft, mainThread: true);
                _ros.Subscribe<CompressedImageMsg>(topicName.Replace("left", "right"), OnCompressedRight, mainThread: true);
            }
            else
            {
                Debug.LogError("Only compressed images are supported at the moment");
                // ros.Subscribe<ImageMsg>(topicName, OnImage);
            }
            topicDropdown.gameObject.SetActive(false);
            topMenu.SetActive(false);
        }

        /// <summary>
        /// Sets up the texture for the image or reallocates if the size has changed
        /// </summary>
        /// <param name="width">Target width for the new image</param>
        /// <param name="height">Target height for the new image</param>
        /// <param name="left">Toggle for whether to operate on the left or right texture</param>
        private void SetupTex(int width = 2, int height = 2, bool left = true)
        {
            // RGB24, not the original RGBA32: LoadRawTextureData (the
            // decoded-JPEG path in OnCompressedLeft/Right) needs the
            // texture's format to actually match the buffer it's fed --
            // ImageConversion.LoadImage (the fallback path) replaces format
            // and dimensions on its own regardless of what's pre-allocated
            // here, so this doesn't affect that path. Format is checked
            // alongside width/height so a texture last resized by the
            // fallback path at a coincidentally-matching size still gets
            // recreated here rather than silently staying whatever format it
            // was left in.
            if (left)
            {
                if (_leftTexture2D == null || _leftTexture2D.width != width || _leftTexture2D.height != height ||
                    _leftTexture2D.format != TextureFormat.RGB24)
                {
                    if (_leftTexture2D != null)
                        Destroy(_leftTexture2D);
                    _leftTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
                    _leftTexture2D.wrapMode = TextureWrapMode.Clamp;
                    _leftTexture2D.filterMode = FilterMode.Bilinear;
                    material.SetTexture("_LeftTex", _leftTexture2D);
                }
            }
            else
            {
                if (_rightTexture2D == null || _rightTexture2D.width != width || _rightTexture2D.height != height ||
                    _rightTexture2D.format != TextureFormat.RGB24)
                {
                    if (_rightTexture2D != null)
                        Destroy(_rightTexture2D);
                    _rightTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
                    _rightTexture2D.wrapMode = TextureWrapMode.Clamp;
                    _rightTexture2D.filterMode = FilterMode.Bilinear;
                    material.SetTexture("_RightTex", _rightTexture2D);
                }
            }
        }

        /// <summary>
        /// Resize the image object to match the aspect ratio of the image
        /// </summary>
        private void Resize()
        {
            if (_leftTexture2D == null) return;
            float aspectRatio = (float)_leftTexture2D.width / (float)_leftTexture2D.height;

            float width = _Img.transform.localScale.x;
            float height = width / aspectRatio;

            _Img.localScale = new Vector3(width, 1, height);
        }

        /// <summary>
        /// Callback for for left compressed image
        /// </summary>
        /// <param name="msg"></param>
        void OnCompressedLeft(CompressedImageMsg msg)
        {
            ParseHeader(msg.header);

            if (_leftDecodeWorker == null)
                _leftDecodeWorker = new JpegDecodeWorker("StereoStreamer-Left-JpegDecode");

            bool haveResult = _leftDecodeWorker.TryTakeResult(
                out byte[] decodedRgb, out int decodedW, out int decodedH, out byte[] fallbackBytes);
            _leftDecodeWorker.Submit(msg.data);
            if (!haveResult) return;

            try
            {
                if (fallbackBytes != null)
                {
                    SetupTex(2, 2, true);
                    ImageConversion.LoadImage(_leftTexture2D, fallbackBytes);
                    _leftTexture2D.Apply();
                }
                else
                {
                    SetupTex(decodedW, decodedH, true);
                    byte[] final = _flipDecodedJpeg
                        ? JpegDecodeWorker.FlipRows(decodedRgb, decodedW, decodedH, 3) : decodedRgb;
                    _leftTexture2D.LoadRawTextureData(final);
                    _leftTexture2D.Apply();
                }
                Resize();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Callback for for right compressed image
        /// </summary>
        /// <param name="msg"></param>
        void OnCompressedRight(CompressedImageMsg msg)
        {
            ParseHeader(msg.header);

            if (_rightDecodeWorker == null)
                _rightDecodeWorker = new JpegDecodeWorker("StereoStreamer-Right-JpegDecode");

            bool haveResult = _rightDecodeWorker.TryTakeResult(
                out byte[] decodedRgb, out int decodedW, out int decodedH, out byte[] fallbackBytes);
            _rightDecodeWorker.Submit(msg.data);
            if (!haveResult) return;

            try
            {
                if (fallbackBytes != null)
                {
                    SetupTex(2, 2, false);
                    ImageConversion.LoadImage(_rightTexture2D, fallbackBytes);
                    _rightTexture2D.Apply();
                }
                else
                {
                    SetupTex(decodedW, decodedH, false);
                    byte[] final = _flipDecodedJpeg
                        ? JpegDecodeWorker.FlipRows(decodedRgb, decodedW, decodedH, 3) : decodedRgb;
                    _rightTexture2D.LoadRawTextureData(final);
                    _rightTexture2D.Apply();
                }
                Resize();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        void OnDestroy()
        {
            if (topicName != null)
            {
                _ros.Unsubscribe(topicName);
                _ros.Unsubscribe(topicName.Replace("left", "right"));
            }
            _leftDecodeWorker?.Stop();
            _rightDecodeWorker?.Stop();
        }

        public override void Deserialize(string data)
        {
            try
            {
                ImageData imgData = JsonUtility.FromJson<ImageData>(data);

                transform.position = imgData.position;
                transform.rotation = imgData.rotation;
                transform.localScale = imgData.scale;
                _trackingState = imgData.trackingState;

                // Route through OnTopicChange rather than setting topicName
                // and subscribing here directly -- OnTopicChange is what
                // unsubscribes whatever topic was previously active before
                // subscribing the new one; setting the topicName field
                // first (the old code here did) leaves nothing for that
                // unsubscribe-old-topic step to read, since by the time it
                // ran topicName already equaled the new topic -- the exact
                // bug that showed up as two routes staying open at once.
                OnTopicChange(imgData.topicName);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("Error deserializing image data! Most likely old data format, clearing prefs");
                PlayerPrefs.DeleteKey("layout");
                PlayerPrefs.Save();
            }

        }
        public override string Serialize()
        {
            ImageData imgData = JsonUtility.FromJson<ImageData>(base.Serialize());
            imgData.stereo = true;

            return JsonUtility.ToJson(imgData);
        }
    }
}


