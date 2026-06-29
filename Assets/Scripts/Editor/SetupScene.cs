#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InfiniteRunner.Core;
using InfiniteRunner.Player;
using InfiniteRunner.Track;
using InfiniteRunner.UI;
using InfiniteRunner.InputSystem;
using InfiniteRunner.Obstacle;
using InfiniteRunner.Difficulty;


namespace InfiniteRunner.EditorTools
{
    public class SetupScene : EditorWindow
    {
        [MenuItem("Infinite Runner/Setup Complete Scene")]
        public static void BuildScene()
        {
            Debug.Log("Starting Infinite Runner Auto-Setup...");

            // 0. Register custom tags required by the obstacle system
            RegisterTag("Obstacle");

            // 1. Create Folder Structure
            CreateDirectories();

            // 2. Create Materials
            Material playerMat = CreateStandardMaterial("Assets/Materials/Mat_Player.mat", Color.black);
            Material laneDarkMat = CreateStandardMaterial("Assets/Materials/Mat_LaneDark.mat", new Color(0.12f, 0.12f, 0.12f));
            Material laneLightMat = CreateStandardMaterial("Assets/Materials/Mat_LaneLight.mat", new Color(0.18f, 0.18f, 0.18f));
            Material dividerMat = CreateStandardMaterial("Assets/Materials/Mat_Divider.mat", Color.white);

            // 3. Create Track Segment Prefab
            GameObject prefab = CreateTrackSegmentPrefab(laneDarkMat, laneLightMat, dividerMat);
            if (prefab == null)
            {
                Debug.LogError("Failed to create Track Segment Prefab.");
                return;
            }

            // 4. Create and Setup the Scene
            UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = "InfiniteRunnerScene";

            // 5. Configure Main Camera
            SetupCamera();

            // 6. Create GameManager
            GameObject gameManagerGo = new GameObject("GameManager");
            GameManager gameManager = gameManagerGo.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gameManagerGo, "Create GameManager");

            // 6.5. Create DifficultyManager
            GameObject difficultyManagerGo = new GameObject("DifficultyManager");
            difficultyManagerGo.AddComponent<DifficultyManager>();
            Undo.RegisterCreatedObjectUndo(difficultyManagerGo, "Create DifficultyManager");

            // 7. Create Player
            GameObject playerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerGo.name = "Player";
            playerGo.tag = "Player"; // Required for obstacle collision detection
            playerGo.transform.position = new Vector3(0f, 0.5f, 0f);
            playerGo.GetComponent<Renderer>().sharedMaterial = playerMat;
            
            // Add Rigidbody for trigger collisions
            Rigidbody playerRb = playerGo.AddComponent<Rigidbody>();
            playerRb.isKinematic = true;
            playerRb.useGravity = false;

            PlayerController playerController = playerGo.AddComponent<PlayerController>();
            
            // Access fields via Reflection or serialized properties to ensure settings are applied
            SerializedObject playerSerialized = new SerializedObject(playerController);
            playerSerialized.FindProperty("laneDistance").floatValue = 3.0f;
            playerSerialized.FindProperty("laneChangeSpeed").floatValue = 10.0f;
            playerSerialized.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(playerGo, "Create Player");

            // 8. Create InputManager
            GameObject inputManagerGo = new GameObject("InputManager");
            InputManager inputManager = inputManagerGo.AddComponent<InputManager>();
            
            SerializedObject inputSerialized = new SerializedObject(inputManager);
            inputSerialized.FindProperty("playerController").objectReferenceValue = playerController;
            inputSerialized.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(inputManagerGo, "Create InputManager");

            // 9. Create TrackManager
            GameObject trackManagerGo = new GameObject("TrackManager");
            TrackManager trackManager = trackManagerGo.AddComponent<TrackManager>();
            
            SerializedObject trackSerialized = new SerializedObject(trackManager);
            trackSerialized.FindProperty("segmentPrefab").objectReferenceValue = prefab;
            trackSerialized.FindProperty("trackSpeed").floatValue = 15.0f;
            trackSerialized.FindProperty("segmentLength").floatValue = 30.0f;
            trackSerialized.FindProperty("segmentCount").intValue = 5;
            trackSerialized.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(trackManagerGo, "Create TrackManager");

            // 9.5. Create ObstacleSpawner
            GameObject obstacleSpawnerGo = new GameObject("ObstacleSpawner");
            ObstacleSpawner obstacleSpawner = obstacleSpawnerGo.AddComponent<ObstacleSpawner>();

            SerializedObject obstacleSerialized = new SerializedObject(obstacleSpawner);
            obstacleSerialized.FindProperty("spawnInterval").floatValue = 3.0f;
            obstacleSerialized.FindProperty("initialDelay").floatValue = 2.0f;
            obstacleSerialized.FindProperty("spawnDistance").floatValue = 40.0f;
            obstacleSerialized.FindProperty("obstacleSpeed").floatValue = 15.0f;
            obstacleSerialized.FindProperty("laneDistance").floatValue = 3.0f;
            obstacleSerialized.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(obstacleSpawnerGo, "Create ObstacleSpawner");

            // 10. Create UI Canvas & Buttons
            SetupUI(gameManagerGo, out Button startButton, out Button stopButton, out Button restartButton);

            // Save the scene
            string scenePath = "Assets/Scenes/InfiniteRunnerScene.unity";
            bool saveResult = EditorSceneManager.SaveScene(newScene, scenePath);
            
            if (saveResult)
            {
                Debug.Log($"Scene saved successfully at: {scenePath}");
                // Add to Build Settings if not already present
                AddSceneToBuildSettings(scenePath);
            }
            else
            {
                Debug.LogError("Failed to save the new scene.");
            }

            Debug.Log("Infinite Runner setup completed successfully! Press PLAY to test.");
        }

        private static void CreateDirectories()
        {
            string[] folders = { "Assets/Materials", "Assets/Prefabs", "Assets/Scenes" };
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    AssetDatabase.ImportAsset(folder);
                }
            }
        }

        private static Material CreateStandardMaterial(string path, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                // Standard shader for Built-in render pipeline
                Shader standardShader = Shader.Find("Standard");
                if (standardShader == null)
                {
                    // Fallback to older diffuse if standard is not found
                    standardShader = Shader.Find("Diffuse");
                }
                
                mat = new Material(standardShader);
                mat.color = color;
                
                // Adjust smoothness and specular/metallic highlights for cleaner visuals
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
                
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static GameObject CreateTrackSegmentPrefab(Material darkMat, Material lightMat, Material dividerMat)
        {
            string prefabPath = "Assets/Prefabs/TrackSegment.prefab";
            
            // Create temporary segment assembly
            GameObject root = new GameObject("TrackSegment");
            
            // Dimensions
            float length = 30.0f;
            float laneWidth = 2.9f;
            float roadHeight = 0.1f;

            // Lane Left
            GameObject laneLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laneLeft.name = "Lane_Left";
            laneLeft.transform.SetParent(root.transform);
            laneLeft.transform.localPosition = new Vector3(-3f, -roadHeight / 2f, length / 2f);
            laneLeft.transform.localScale = new Vector3(laneWidth, roadHeight, length);
            laneLeft.GetComponent<Renderer>().sharedMaterial = darkMat;

            // Lane Center
            GameObject laneCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laneCenter.name = "Lane_Center";
            laneCenter.transform.SetParent(root.transform);
            laneCenter.transform.localPosition = new Vector3(0f, -roadHeight / 2f, length / 2f);
            laneCenter.transform.localScale = new Vector3(laneWidth, roadHeight, length);
            laneCenter.GetComponent<Renderer>().sharedMaterial = lightMat;

            // Lane Right
            GameObject laneRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laneRight.name = "Lane_Right";
            laneRight.transform.SetParent(root.transform);
            laneRight.transform.localPosition = new Vector3(3f, -roadHeight / 2f, length / 2f);
            laneRight.transform.localScale = new Vector3(laneWidth, roadHeight, length);
            laneRight.GetComponent<Renderer>().sharedMaterial = darkMat;

            // Left Divider Line
            GameObject dividerLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerLeft.name = "Divider_Left";
            dividerLeft.transform.SetParent(root.transform);
            dividerLeft.transform.localPosition = new Vector3(-1.5f, -roadHeight / 4f, length / 2f);
            dividerLeft.transform.localScale = new Vector3(0.1f, roadHeight * 1.5f, length);
            dividerLeft.GetComponent<Renderer>().sharedMaterial = dividerMat;
            DestroyImmediate(dividerLeft.GetComponent<Collider>()); // No need for colliders on visual dividers

            // Right Divider Line
            GameObject dividerRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerRight.name = "Divider_Right";
            dividerRight.transform.SetParent(root.transform);
            dividerRight.transform.localPosition = new Vector3(1.5f, -roadHeight / 4f, length / 2f);
            dividerRight.transform.localScale = new Vector3(0.1f, roadHeight * 1.5f, length);
            dividerRight.GetComponent<Renderer>().sharedMaterial = dividerMat;
            DestroyImmediate(dividerRight.GetComponent<Collider>());

            // Save as prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            
            // Clean up temporary object
            DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return savedPrefab;
        }

        private static void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 4f, -7f);
                mainCam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.15f, 0.18f, 0.22f); // Sleek modern background color
                Undo.RecordObject(mainCam.gameObject, "Configure Main Camera");
            }
        }

        private static void SetupUI(GameObject gameManagerGo, out Button startButton, out Button stopButton, out Button restartButton)
        {
            // Create Canvas
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create UI Canvas");

            // Create EventSystem if missing
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            }

            // UI Resources for standard elements
            DefaultControls.Resources uiResources = new DefaultControls.Resources();

            // Create UI Panel Container for alignment (sleek header bar)
            GameObject panelGo = DefaultControls.CreatePanel(uiResources);
            panelGo.name = "ButtonPanel";
            panelGo.transform.SetParent(canvasGo.transform, false);
            
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f); // Top center
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -20f);
            panelRect.sizeDelta = new Vector2(500f, 80f);

            // Add clean dark glass panel background style
            Image panelImage = panelGo.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            }

            // Create Start Button
            GameObject startBtnGo = DefaultControls.CreateButton(uiResources);
            startBtnGo.name = "Button_Start";
            startBtnGo.transform.SetParent(panelGo.transform, false);
            startButton = startBtnGo.GetComponent<Button>();
            ConfigureButtonUI(startBtnGo, "INICIAR", new Vector2(-150f, 0f), new Color(0.18f, 0.65f, 0.35f));

            // Create Stop Button
            GameObject stopBtnGo = DefaultControls.CreateButton(uiResources);
            stopBtnGo.name = "Button_Stop";
            stopBtnGo.transform.SetParent(panelGo.transform, false);
            stopButton = stopBtnGo.GetComponent<Button>();
            ConfigureButtonUI(stopBtnGo, "PARAR", new Vector2(-150f, 0f), new Color(0.85f, 0.25f, 0.25f));

            // Create Restart Button
            GameObject restartBtnGo = DefaultControls.CreateButton(uiResources);
            restartBtnGo.name = "Button_Restart";
            restartBtnGo.transform.SetParent(panelGo.transform, false);
            restartButton = restartBtnGo.GetComponent<Button>();
            ConfigureButtonUI(restartBtnGo, "REINICIAR", new Vector2(100f, 0f), new Color(0.2f, 0.5f, 0.85f));

            // Attach and configure UIManager
            UIManager uiManager = canvasGo.AddComponent<UIManager>();
            SerializedObject uiSerialized = new SerializedObject(uiManager);
            uiSerialized.FindProperty("startButton").objectReferenceValue = startButton;
            uiSerialized.FindProperty("stopButton").objectReferenceValue = stopButton;
            uiSerialized.FindProperty("restartButton").objectReferenceValue = restartButton;
            uiSerialized.ApplyModifiedProperties();
        }

        private static void ConfigureButtonUI(GameObject btnGo, string textVal, Vector2 localPos, Color color)
        {
            RectTransform rect = btnGo.GetComponent<RectTransform>();
            rect.anchoredPosition = localPos;
            rect.sizeDelta = new Vector2(180f, 50f);

            Image img = btnGo.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
            }

            Text txt = btnGo.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = textVal;
                txt.color = Color.white;
                txt.fontSize = 18;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            bool exists = false;
            foreach (var scene in originalScenes)
            {
                if (scene.path == scenePath)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                System.Array.Copy(originalScenes, newScenes, originalScenes.Length);
                newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"Added scene to build settings: {scenePath}");
            }
        }

        /// <summary>
        /// Registers a custom tag in the TagManager if it does not already exist.
        /// </summary>
        private static void RegisterTag(string tagName)
        {
            // Unity stores tags in ProjectSettings/TagManager.asset
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Check if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                    return; // Already registered
            }

            // Add new tag
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"Registered custom tag: {tagName}");
        }
    }
}
#endif
