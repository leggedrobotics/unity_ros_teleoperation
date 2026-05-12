using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using LookingGlass.Demos;
using LookingGlass.Mobile;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LookingGlass.Demos.Editor {
    public class iOSSetupWindow : EditorWindow {
        private int currentStep = 0;
        //private Canvas displayPairCanvas;
        //private DemoIOSUIController calibrationPreloader;
        [SerializeField] private GameObject dmaPrefab;
        [SerializeField] private GameObject hologramCamPrefab;
        [SerializeField] private GameObject displayPairCanvasPrefab;
        [SerializeField] private GameObject repairButtonPrefab;
        [SerializeField] private GameObject testPopupPrefab;
        [SerializeField] private GameObject calibratePopupPrefab;
        [SerializeField] private GameObject popUpsPrefab;


        private Canvas mainCanvas;
        private GameObject mainScene;
        private GameObject targetObject;
        private GameObject prefab1;
        private GameObject prefab2;

        private const string COMPONENTNAME = "Display Pairing Canvas";

        [MenuItem("LookingGlass/iOS Setup Wizard")]
        public static void ShowWindow() {
            GetWindow<iOSSetupWindow>("iOS Setup Wizard");
        }

        private void OnGUI() {
            //GUILayout.Label("iOS Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            switch (currentStep) {
                case 0:
                    DrawStepAddMobileDMA();
                    break;
                case 1:
                    DrawStepHologramCamera();
                    break;
                case 2:
                    DrawStepAddCanvas();
                    break;
                case 3:
                    DrawStepAddPopupCanvas();
                    break;
                case 4:
                    DrawStepSetupMainCanvas();
                    break;
                case 5:
                    DrawStepAddPrefabs();
                    break;
                case 6:
                    DrawStepPlayMode();
                    break;

                default:
                    break;
            }

            //AdjustWindowSize();

            GUILayout.Space(20);
        }

        private void AdjustWindowSize() {
            // Repaint ensures the UI updates dynamically
            //Repaint();

            // Get the current height of the GUI content
            float newHeight = GUILayoutUtility.GetLastRect().yMax + 20; // Add padding

            // Set minSize and maxSize to force the window to resize dynamically
            minSize = new Vector2(300, newHeight);
            maxSize = new Vector2(600, newHeight);
        }


        private void DrawStepAddMobileDMA() {
            GUILayout.Label($"Step {currentStep + 1}: Add a {nameof(MobileDMAController)}", EditorStyles.boldLabel);
            GUILayout.Label($"A {nameof(MobileDMAController)} is required for handling dual window on iOS.", EditorStyles.wordWrappedLabel);

            // look for canvas
            var mobileDMAController = FindObjectOfType<MobileDMAController>(true);

            if (mobileDMAController == null) {
                if (GUILayout.Button($"Instantiate {nameof(MobileDMAController)} to the scene")) {                    
                    GameObject dmaObj = (GameObject)PrefabUtility.InstantiatePrefab(dmaPrefab);
                    Selection.activeGameObject = dmaObj;
                }
            } else {

                Selection.activeGameObject = mobileDMAController.gameObject;

                GUILayout.Label($"✔ {nameof(MobileDMAController)} detected in the scene.", EditorStyles.helpBox);
                if (GUILayout.Button("Next")) {
                    currentStep++;
                } 
                else if (GUILayout.Button("Back")) {
                    currentStep--;
                }
            }
        }

        private void DrawStepHologramCamera() {
            GUILayout.Label($"Step {currentStep + 1}: Add a {nameof(HologramCamera)}", EditorStyles.boldLabel);
            GUILayout.Label($"A {nameof(HologramCamera)} is required for dynamic rendering on Looking Glass.", EditorStyles.wordWrappedLabel);

            // look for canvas
            var hologramCamera = FindObjectOfType<HologramCamera>(true);
 
            if (hologramCamera == null) {
                if (GUILayout.Button($"Instantiate {nameof(HologramCamera)} to the scene")) {
                    GameObject camObj = (GameObject)PrefabUtility.InstantiatePrefab(hologramCamPrefab);
                    Selection.activeGameObject = camObj;
                }
            } else {

                Selection.activeGameObject = hologramCamera.gameObject;

                GUILayout.Label($"✔ {nameof(HologramCamera)} detected in the scene.", EditorStyles.helpBox);
                if (GUILayout.Button("Next")) {
                    currentStep++;
                } else if (GUILayout.Button("Back")) {
                    currentStep--;
                }
            }
        }

        private void DrawStepAddCanvas() {
            GUILayout.Label($"Step {currentStep + 1}: Add a {COMPONENTNAME}", EditorStyles.boldLabel);
            GUILayout.Label($"A Canvas is required for UI elements.", EditorStyles.wordWrappedLabel);

            // look for canvas
            var calibrationPreloader = FindObjectOfType<DemoIOSUIController>(true);
            //canvas = FindObjectOfType<DemoIOSUIController>();

            if (calibrationPreloader == null) {
                if (GUILayout.Button($"Instantiate {COMPONENTNAME} to the scene")) {
                    GameObject canvasObj = (GameObject)PrefabUtility.InstantiatePrefab(displayPairCanvasPrefab);
                    Selection.activeGameObject = canvasObj;
                }
            } else {
                GUILayout.Label($"✔ {COMPONENTNAME} detected in the scene.", EditorStyles.helpBox);
                if (GUILayout.Button("Next")) {
                    currentStep++;
                } else if (GUILayout.Button("Back")) {
                    currentStep--;
                }
            }
        }

        private void DrawStepAddPopupCanvas() {
            GUILayout.Label($"Step {currentStep + 1}: Add {nameof(PopupManager)} for pop-ups", EditorStyles.boldLabel);
            GUILayout.Label($"A Canvas will be added for pop-up UI elements.", EditorStyles.wordWrappedLabel);

            var popups = FindObjectOfType<PopupManager>(true);
            bool isSetupCompletely = false;

            if (popups != null) {
                GUILayout.Label($"✔ {nameof(PopupManager)} detected in the scene.", EditorStyles.helpBox);

                if (!popups.GetComponentInChildren<PopupPairHandler>(true)) {
                    GUILayout.Label($"Warning {nameof(PopupPairHandler)} is missing under {nameof(PopupManager)}.", EditorStyles.helpBox);
                }
               else if (!popups.GetComponentInChildren<PopupTestHandler>(true)) {
                    GUILayout.Label($"Warning {nameof(PopupTestHandler)} is missing under {nameof(PopupManager)}.", EditorStyles.helpBox);
                }
                else {
                    isSetupCompletely = true;
                    if (GUILayout.Button("Next")) {
                        currentStep++;
                    } 
                }
            }
            
            if (!isSetupCompletely && GUILayout.Button("Instantiate Canvas for Pair & Test pop-ups")) {
                //mainCanvas.sortingOrder
                // Create a new empty GameObject as the parent
                GameObject parentObject = (GameObject)PrefabUtility.InstantiatePrefab(popUpsPrefab);
                Selection.activeGameObject = parentObject;
            }
            GUILayout.Label("Those pop-ups will allow users to pair their Looking Glass device and test.", EditorStyles.helpBox);
            
            if (GUILayout.Button("Back")) {
                currentStep--;
            }
        }

        private void DrawStepSetupMainCanvas() {
            var calibrationPreloader = FindObjectOfType<DemoIOSUIController>(true);
            if (calibrationPreloader == null) {
                GUILayout.Label($"x {nameof(DemoIOSUIController)} is missing. Please click back to instantiate one.", EditorStyles.helpBox);
            }
            DrawStepAssignObject(ref calibrationPreloader.mainCanvas, ref mainCanvas, "main canvas");

            if (GUILayout.Button("Back")) {
                currentStep--;
            }
        }

        private void DrawStepAddPrefabs() {
            var calibrationPreloader = FindObjectOfType<DemoIOSUIController>(true);
            if (calibrationPreloader == null) {
                GUILayout.Label($"x {nameof(DemoIOSUIController)} is missing. Please click back to instantiate one.", EditorStyles.helpBox);
            }
            DrawStepAssignObject(ref calibrationPreloader.mainScene, ref mainScene, "main scene");

            if (GUILayout.Button("Back")) {
                currentStep--;
            }
        }

        private void DrawStepAssignObject<T>(ref T assignedObject, ref T newAssignedObject, string label) where T : UnityEngine.Object {
            GUILayout.Label($"Step {currentStep + 1}: Assign {label}", EditorStyles.boldLabel);
            GUILayout.Label($"Select the {label} to be used in the scene. Assigning this ensures that the correct elements are loaded and visible when needed.", EditorStyles.wordWrappedLabel);

            var calibrationPreloader = FindObjectOfType<DemoIOSUIController>(true);
            
            T currentAssigned = assignedObject;

            if (currentAssigned != null) {
                EditorGUI.BeginDisabledGroup(true);
                currentAssigned = (T)EditorGUILayout.ObjectField($"Current {label}", currentAssigned, typeof(T), true);
                EditorGUI.EndDisabledGroup();
            }

            // Create ObjectField with change detection
            EditorGUI.BeginChangeCheck();
            newAssignedObject = (T)EditorGUILayout.ObjectField($"New {label}", newAssignedObject, typeof(T), true);

            if (newAssignedObject != null) {
                GUILayout.Label($"✔ New {label} assigned.", EditorStyles.helpBox);
                if (currentAssigned == null || !currentAssigned.Equals(newAssignedObject)) {
                    if (GUILayout.Button($"Assign {label} to the script")) {
                        if (EditorGUI.EndChangeCheck()) // Detect if user changed the value
                        {
                            Undo.RecordObject(calibrationPreloader, $"Assign {label}"); // Allow undo
                            //onAssign?.Invoke(assignedObject); // Apply change via callback
                            assignedObject = newAssignedObject;
                            EditorUtility.SetDirty(calibrationPreloader); // Mark the object as changed
                        }

                        Selection.activeGameObject = calibrationPreloader.gameObject;
                        currentStep++;
                    }
                } else if (GUILayout.Button($"Proceed with current {label}")) {
                    Selection.activeGameObject = calibrationPreloader.gameObject;
                    currentStep++;
                }
            } else {
                if (currentAssigned != null && GUILayout.Button($"Proceed with current {label}")) {
                    Selection.activeGameObject = calibrationPreloader.gameObject;
                    currentStep++;
                }
                if (GUILayout.Button($"Proceed without {label}")) {
                    if (EditorGUI.EndChangeCheck()) // Detect if user changed the value
                        {
                        Undo.RecordObject(calibrationPreloader, $"Assign {label}"); // Allow undo                                                
                        assignedObject = null;
                        EditorUtility.SetDirty(calibrationPreloader); // Mark the object as changed
                    }
                    Selection.activeGameObject = calibrationPreloader.gameObject;
                    currentStep++;
                }
            }
        }

        //private void DrawStepSetupGameView() {
        //    GUILayout.Label($"Step {currentStep + 1}: Open Game Views for UI and the scene", EditorStyles.boldLabel);
        //    //GUILayout.Label($"Setup is complete! Save the scene. Make sure the {COMPONENTNAME} is active. Press Play to test the scene.", EditorStyles.wordWrappedLabel);

        //    if (GUILayout.Button("Open Game Views for UI and the scene")) {

                
        //    }

        //    if (GUILayout.Button("Back")) {
        //        currentStep--;
        //    }
        //}

        private void DrawStepPlayMode() {
            GUIStyle richTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel) {
                richText = true
            };


            GUILayout.Label($"Step {currentStep + 1}: Enter Play Mode", EditorStyles.boldLabel);
            GUILayout.Label("Setup is complete! 🎉\n" +
    "<b>Save your scene</b> to keep the changes.\n" +
    $"Ensure <b>{COMPONENTNAME}</b> is active. \n" +
    $"Ensure <b>all the Canvas</b> target Display 1.\n" +
    "Connect your <b>Looking Glass</b> display.\n" +
    "Toggle the <b>Preview Window</b> and press <b>Play</b> to test your setup.", richTextStyle);
            GUILayout.Label($"To make both UI and scene viewable in gameviews, in Editor, canvas need to target Display 1 while hologram camera will auto-target Display 2 via {nameof(MobileDMAController)}.", EditorStyles.helpBox);

            if(LKGDisplaySystem.LKGDisplayCount > 0) {
                GUILayout.Label($"You can use <b>Cast to Looking Glass </b> button on Hologram Camera to open a preview window.", richTextStyle);
                if(Selection.activeGameObject == HologramCamera.Instance.gameObject) {
                    GUILayout.Label($"Hologram Camera is selected. Please find the button in the inspector.", EditorStyles.helpBox);
                }
                else if (GUILayout.Button("Guide me to Hologram Camera")) {
                    Selection.activeGameObject = HologramCamera.Instance.gameObject;
                }
            } else {
                GUILayout.Label($"Connect your Looking Glass display for better experience.", EditorStyles.helpBox);
            }


            if (GUILayout.Button("Save the scene and play")) {

                Scene activeScene = SceneManager.GetActiveScene();  

                if (activeScene.isDirty) // Check if the scene has unsaved changes
                {
                    EditorSceneManager.SaveScene(activeScene);
                    Debug.Log("Scene saved: " + activeScene.name);
                } else {
                    Debug.Log("No changes detected. Scene was not saved.");
                }

                // Use delayCall to ensure saving completes before switching to Play mode
                EditorApplication.delayCall += () => {
                    EditorApplication.isPlaying = true;
                };
            }

            if (GUILayout.Button("Back")) {
                currentStep--;
            }
        }
    }
}
