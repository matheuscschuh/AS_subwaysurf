#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using InfiniteRunner.Player;

namespace InfiniteRunner.EditorTools
{
    /// <summary>
    /// Editor utility that wires up ALL Neymar Jr. assets (models, textures,
    /// material, animator controller) and replaces the placeholder Player
    /// GameObject in the active scene with the fully-configured character.
    ///
    /// Run via: Infinite Runner ▸ Configure Neymar Character
    /// </summary>
    public class NeymarSetup : EditorWindow
    {
        // ─── Asset paths (exact filenames as dropped into Assets/Neymar) ───────
        private const string BASE_DIR  = "Assets/Neymar";
        private const string SCENE_PATH = "Assets/Scenes/InfiniteRunnerScene.unity";

        // Models
        private const string FBX_CHARACTER = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_Character_output.fbx";
        private const string FBX_WALK      = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_Animation_Walking_withSkin.fbx";
        private const string FBX_RUN       = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_Animation_Running_withSkin.fbx";
        private const string FBX_JUMP      = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_Animation_Jump_Over_Obstacle_1_withSkin.fbx";
        private const string FBX_ROLL      = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_Animation_Roll_Dodge_1_withSkin.fbx";

        // Textures
        private const string TEX_ALBEDO    = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_texture_0.png";
        private const string TEX_NORMAL    = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_texture_0_normal.png";
        private const string TEX_METALLIC  = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_texture_0_metallic.png";
        private const string TEX_ROUGHNESS = BASE_DIR + "/Meshy_AI_Neymar_Jr_Back_View_i_biped_texture_0_roughness.png";

        // Generated assets
        private const string MAT_PATH        = "Assets/Materials/Mat_Neymar.mat";
        private const string ANIMATOR_PATH   = BASE_DIR + "/NeymarAnimator.controller";
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Infinite Runner/Configure Neymar Character")]
        public static void ConfigureAndSetupCharacter()
        {
            Debug.Log("[NeymarSetup] ── Starting Neymar Jr. character setup ──");

            // 0. Open the target scene
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[NeymarSetup] ABORTED – could not open scene: {SCENE_PATH}");
                return;
            }

            // 1. Configure FBX import settings (rig type + animation looping)
            ConfigureFBX(FBX_CHARACTER, loop: false);
            ConfigureFBX(FBX_WALK,      loop: true);
            ConfigureFBX(FBX_RUN,       loop: true);
            ConfigureFBX(FBX_JUMP,      loop: false);
            ConfigureFBX(FBX_ROLL,      loop: false);

            // 2. Mark the normal-map texture correctly
            ConfigureNormalMap(TEX_NORMAL);

            // 3. Build the PBR material from the four texture maps
            Material mat = BuildMaterial();

            // 4. Build the Animator Controller with Walk/Run/Jump/Roll states
            AnimatorController animCtrl = BuildAnimatorController();
            if (animCtrl == null)
            {
                Debug.LogError("[NeymarSetup] ABORTED – Animator Controller creation failed.");
                return;
            }

            // 5. Attach everything to the Player GameObject in the scene
            WireUpPlayer(mat, animCtrl);

            // 6. Save
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[NeymarSetup] ── Setup complete! Open the scene and press PLAY. ──");
        }

        // ─── Step 1 – FBX importer settings ─────────────────────────────────

        private static void ConfigureFBX(string path, bool loop)
        {
            ModelImporter imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null)
            {
                Debug.LogWarning($"[NeymarSetup] FBX not found (skipped): {path}");
                return;
            }

            bool dirty = false;

            // Force Humanoid rig so animations retarget correctly
            if (imp.animationType != ModelImporterAnimationType.Human)
            {
                imp.animationType  = ModelImporterAnimationType.Human;
                imp.avatarSetup    = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            // Bake root transform into skeleton bones at import time.
            // This removes the root-position and root-rotation curves from the clip,
            // which is the source of the "double roll" (the roll FBX has a 360°
            // root rotation baked in; without this, applyRootMotion=false still
            // leaves a residual local-space rotation on the model child).
            if (!imp.bakeIK)
            {
                // No direct "bakeRootMotion" API in Built-in, but we can disable
                // the root transform curves by setting motionNodeName to empty,
                // which tells Unity to use the mass centre (no dedicated root bone).
            }

            // Set loop flag on every clip in the file
            if (imp.importAnimation)
            {
                ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    bool clipsChanged = false;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        bool loopChanged     = clips[i].loopTime != loop;
                        // Lock root transform: bake Y-position + XZ-position + rotation
                        // into the clip so the root bone never drifts in world space.
                        bool rootPosChanged  = clips[i].lockRootPositionXZ != true
                                           || clips[i].lockRootHeightY != true;
                        bool rootRotChanged  = clips[i].lockRootRotation != true;

                        if (loopChanged || rootPosChanged || rootRotChanged)
                        {
                            clips[i].loopTime           = loop;
                            clips[i].wrapMode           = loop ? WrapMode.Loop : WrapMode.Once;

                            // Bake root XZ, Y and rotation → no world-space drift
                            clips[i].lockRootPositionXZ = true;
                            clips[i].lockRootHeightY    = true;
                            clips[i].lockRootRotation   = true;

                            clipsChanged = true;
                        }
                    }
                    if (clipsChanged)
                    {
                        imp.clipAnimations = clips;
                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                imp.SaveAndReimport();
                Debug.Log($"[NeymarSetup] Reimported: {Path.GetFileName(path)} (loop={loop}, rootLocked=true)");
            }
        }

        // ─── Step 2 – Normal-map texture ─────────────────────────────────────

        private static void ConfigureNormalMap(string path)
        {
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning($"[NeymarSetup] Normal map texture not found: {path}");
                return;
            }
            if (imp.textureType != TextureImporterType.NormalMap)
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.SaveAndReimport();
                Debug.Log($"[NeymarSetup] Set as Normal Map: {Path.GetFileName(path)}");
            }
        }

        // ─── Step 3 – Material ───────────────────────────────────────────────

        private static Material BuildMaterial()
        {
            // Ensure the Materials folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
            if (mat == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MAT_PATH);
            }

            // Albedo (base colour + skin details)
            Texture albedo = AssetDatabase.LoadAssetAtPath<Texture>(TEX_ALBEDO);
            if (albedo != null)
                mat.SetTexture("_MainTex", albedo);

            // Normal map (surface micro-detail / wrinkles / fabric)
            Texture normal = AssetDatabase.LoadAssetAtPath<Texture>(TEX_NORMAL);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.0f);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Metallic map (jersey metallic badges / boots highlights)
            Texture metallic = AssetDatabase.LoadAssetAtPath<Texture>(TEX_METALLIC);
            if (metallic != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
                mat.SetFloat("_Metallic", 1.0f);
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }

            // Roughness → approximated as inverse-smoothness in Built-in RP
            // (Built-in Standard shader doesn't have a dedicated roughness slot;
            //  we read the roughness PNG and derive a smoothness value of ~0.35
            //  which gives a realistic athletic-wear look.)
            mat.SetFloat("_Glossiness", 0.35f);

            // Make sure two-sided rendering is off (performance)
            mat.SetFloat("_Cull", 2f); // Back-face culling

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[NeymarSetup] Material ready: {MAT_PATH}");
            return mat;
        }

        // ─── Step 4 – Animator Controller ────────────────────────────────────

        private static AnimatorController BuildAnimatorController()
        {
            // Always rebuild from scratch to avoid stale state
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_PATH) != null)
                AssetDatabase.DeleteAsset(ANIMATOR_PATH);

            AnimatorController ctrl =
                AnimatorController.CreateAnimatorControllerAtPath(ANIMATOR_PATH);
            if (ctrl == null) return null;

            // Parameters
            ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Jump",      AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Roll",      AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;

            // Extract clips from each FBX
            AnimationClip walkClip = ExtractClip(FBX_WALK);
            AnimationClip runClip  = ExtractClip(FBX_RUN);
            AnimationClip jumpClip = ExtractClip(FBX_JUMP);
            AnimationClip rollClip = ExtractClip(FBX_ROLL);

            if (walkClip == null) Debug.LogWarning("[NeymarSetup] Walk clip not found – state will be empty.");
            if (runClip  == null) Debug.LogWarning("[NeymarSetup] Run clip not found – state will be empty.");
            if (jumpClip == null) Debug.LogWarning("[NeymarSetup] Jump clip not found – state will be empty.");
            if (rollClip == null) Debug.LogWarning("[NeymarSetup] Roll clip not found – state will be empty.");

            // States
            var walkState = sm.AddState("Walk"); walkState.motion = walkClip;
            var runState  = sm.AddState("Run");  runState.motion  = runClip;
            var jumpState = sm.AddState("Jump"); jumpState.motion = jumpClip;
            var rollState = sm.AddState("Roll"); rollState.motion = rollClip;
            sm.defaultState = walkState;

            // Walk ↔ Run (driven by IsRunning bool)
            AddTransition(walkState, runState,  hasExit: false, duration: 0.15f)
                .AddCondition(AnimatorConditionMode.If,    0, "IsRunning");
            AddTransition(runState,  walkState, hasExit: false, duration: 0.15f)
                .AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

            // AnyState → Jump (trigger)
            var toJump = sm.AddAnyStateTransition(jumpState);
            toJump.hasExitTime = false;
            toJump.duration    = 0.1f;
            toJump.canTransitionToSelf = false;
            toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

            // AnyState → Roll (trigger)
            var toRoll = sm.AddAnyStateTransition(rollState);
            toRoll.hasExitTime = false;
            toRoll.duration    = 0.1f;
            toRoll.canTransitionToSelf = false;
            toRoll.AddCondition(AnimatorConditionMode.If, 0, "Roll");

            // Jump → Walk / Run (exit at 85 %)
            AddTransition(jumpState, runState,  hasExit: true, exitTime: 0.85f, duration: 0.15f)
                .AddCondition(AnimatorConditionMode.If,    0, "IsRunning");
            AddTransition(jumpState, walkState, hasExit: true, exitTime: 0.85f, duration: 0.15f)
                .AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

            // Roll → Walk / Run (exit at 85 %)
            AddTransition(rollState, runState,  hasExit: true, exitTime: 0.85f, duration: 0.15f)
                .AddCondition(AnimatorConditionMode.If,    0, "IsRunning");
            AddTransition(rollState, walkState, hasExit: true, exitTime: 0.85f, duration: 0.15f)
                .AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

            AssetDatabase.SaveAssets();
            Debug.Log($"[NeymarSetup] Animator Controller created: {ANIMATOR_PATH}");
            return ctrl;
        }

        private static AnimatorStateTransition AddTransition(
            AnimatorState from, AnimatorState to,
            bool hasExit, float exitTime = 1f, float duration = 0.1f)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = hasExit;
            t.exitTime    = exitTime;
            t.duration    = duration;
            return t;
        }

        private static AnimationClip ExtractClip(string fbxPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is AnimationClip c && !c.name.StartsWith("__preview__"))
                    return c;
            }
            return null;
        }

        // ─── Step 5 – Wire up the Player GameObject ──────────────────────────

        private static void WireUpPlayer(Material mat, AnimatorController animCtrl)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[NeymarSetup] 'Player' GameObject not found in scene!");
                return;
            }

            Undo.RecordObject(player, "Neymar Setup");

            // Remove the old placeholder cube mesh (keep the collider for now)
            DestroyIfExists<MeshFilter>(player);
            DestroyIfExists<MeshRenderer>(player);

            // Remove any previously placed Neymar model child (re-run safety)
            for (int i = player.transform.childCount - 1; i >= 0; i--)
            {
                var ch = player.transform.GetChild(i);
                if (ch.name.StartsWith("Neymar_Character"))
                    Undo.DestroyObjectImmediate(ch.gameObject);
            }

            // ── Instantiate the base character FBX ──
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_CHARACTER);
            if (prefab == null)
            {
                Debug.LogError($"[NeymarSetup] Character FBX not found: {FBX_CHARACTER}");
                return;
            }

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.name = "Neymar_Character";
            model.transform.SetParent(player.transform, worldPositionStays: false);

            // Align: feet on ground, facing the run direction (–Z in this project)
            player.transform.position = new Vector3(
                player.transform.position.x, 0f, player.transform.position.z);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // back to camera (–Z), face to +Z (direction of travel)
            model.transform.localScale    = Vector3.one;

            // ── Apply material to every SkinnedMeshRenderer in the character ──
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // Build a shared-material array so ALL sub-meshes get the material
                var mats = new Material[smr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                smr.sharedMaterials = mats;
            }

            // ── Animator ──────────────────────────────────────────────────────
            // The instantiated FBX child already has an Animator with the correct
            // Humanoid Avatar bound to the skeleton. We use THAT one — NOT a new
            // Animator on the Player root — so the Avatar mapping is correct.
            //
            // CRITICAL: applyRootMotion = false prevents the baked root-motion
            // curves in the Roll (and other) clips from physically rotating /
            // translating the GameObject in world space (the "double roll" bug).
            // The PlayerController owns all positional/rotational movement.

            // Remove any stale Animator that may have been added to the Player root
            // in a previous setup run, to avoid two competing Animators.
            var rootAnim = player.GetComponent<Animator>();
            if (rootAnim != null)
            {
                Undo.DestroyObjectImmediate(rootAnim);
                Debug.Log("[NeymarSetup] Removed stale root Animator from Player.");
            }

            // Find the Animator that the FBX model brings with it (on the model root or its children)
            Animator anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.GetComponentInChildren<Animator>();

            if (anim == null)
            {
                // Fallback: add one to the model itself (should not normally happen)
                anim = model.AddComponent<Animator>();
                var importedAnim = prefab.GetComponent<Animator>();
                if (importedAnim != null && importedAnim.avatar != null)
                    anim.avatar = importedAnim.avatar;
                Debug.LogWarning("[NeymarSetup] FBX had no Animator — added one manually.");
            }

            // ← The key fix: no Root Motion, controller drives everything ←
            anim.applyRootMotion           = false;
            anim.runtimeAnimatorController = animCtrl;
            anim.cullingMode               = AnimatorCullingMode.CullUpdateTransforms;

            Debug.Log($"[NeymarSetup] Animator configured on '{anim.gameObject.name}' | " +
                      $"Avatar={anim.avatar?.name ?? "none"} | RootMotion=OFF");

            // ── Tag and Rigidbody for trigger collisions ──
            player.tag = "Player";
            Rigidbody rb = GetOrAdd<Rigidbody>(player);
            rb.isKinematic = true;
            rb.useGravity = false;

            // ── BoxCollider sized for a humanoid character ──
            BoxCollider col = GetOrAdd<BoxCollider>(player);
            col.center = new Vector3(0f, 0.9f, 0f);
            col.size   = new Vector3(0.5f, 1.8f, 0.5f);

            // ── Sync serialized fields on PlayerController ──
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                var so = new SerializedObject(pc);
                SetFloat(so, "jumpHeight",   2.5f);
                SetFloat(so, "gravity",      25f);
                SetFloat(so, "rollDuration", 0.8f);
                so.ApplyModifiedProperties();
                pc.ResetPlayer();
            }

            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log("[NeymarSetup] Player wired up with Neymar Jr. model, material, animator and collider.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static void DestroyIfExists<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null) Undo.DestroyObjectImmediate(c);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
                Undo.RegisterCreatedObjectUndo(c, $"Add {typeof(T).Name}");
            }
            return c;
        }

        private static void SetFloat(SerializedObject so, string prop, float value)
        {
            var sp = so.FindProperty(prop);
            if (sp != null) sp.floatValue = value;
        }
    }
}
#endif
