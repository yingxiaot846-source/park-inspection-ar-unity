using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using MvpArSubmitter;

public static class S5Setup
{
    public static void Apply()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.companyName = "MvpDemo";
        PlayerSettings.productName = "AR巡检上报";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mvp.arinspect");
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
        {
            GraphicsDeviceType.OpenGLES3,
        });
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        Debug.Log("[S5Setup] 玩家设置完成");

        Object[] settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (settingsAssets != null && settingsAssets.Length > 0)
        {
            SerializedObject so = new SerializedObject(settingsAssets[0]);
            SerializedProperty prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue != 2)
            {
                prop.intValue = 2;
                so.ApplyModifiedProperties();
                Debug.Log("[S5Setup] activeInputHandler 已改为 2 (Both)");
            }
            else
            {
                Debug.Log("[S5Setup] activeInputHandler 当前=" + (prop != null ? prop.intValue.ToString() : "未知"));
            }
        }

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        bool okA = EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
        bool okB = EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");
        Debug.Log("[S5Setup] 菜单命令返回 okA=" + okA + " okB=" + okB);

        if (Object.FindObjectOfType<ARSession>() == null)
        {
            GameObject sessionGo = new GameObject("AR Session");
            sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();
            Debug.Log("[S5Setup] 兜底分支：手工创建 AR Session");
        }

        if (Object.FindObjectOfType<XROrigin>() == null)
        {
            GameObject originGo = new GameObject("XR Origin (Mobile AR)");
            XROrigin origin = originGo.AddComponent<XROrigin>();

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.SetParent(originGo.transform, false);
            camGo.AddComponent<ARCameraManager>();
            camGo.AddComponent<ARCameraBackground>();
            camGo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();

            origin.Camera = cam;
            originGo.AddComponent<ARPlaneManager>();
            originGo.AddComponent<ARRaycastManager>();
            Debug.Log("[S5Setup] 兜底分支：手工创建 XR Origin（相机/平面/射线）");
        }

        XROrigin originForManagers = Object.FindObjectOfType<XROrigin>();
        if (originForManagers != null)
        {
            if (Object.FindObjectOfType<ARPlaneManager>() == null)
            {
                originForManagers.gameObject.AddComponent<ARPlaneManager>();
                Debug.Log("[S5Setup] 补挂 ARPlaneManager（菜单版缺失）");
            }
            if (Object.FindObjectOfType<ARRaycastManager>() == null)
            {
                originForManagers.gameObject.AddComponent<ARRaycastManager>();
                Debug.Log("[S5Setup] 补挂 ARRaycastManager（菜单版缺失）");
            }

            Debug.Log("[S5Setup] 管理器数量 plane=" + Object.FindObjectsOfType<ARPlaneManager>().Length + " raycast=" + Object.FindObjectsOfType<ARRaycastManager>().Length);

            Camera originCam = originForManagers.Camera;
            Debug.Log("[S5Setup] 相机清点 name=" + (originCam != null ? originCam.name : "null") +
                      " ARCameraManager=" + (originCam != null && originCam.GetComponent<ARCameraManager>() != null) +
                      " ARCameraBackground=" + (originCam != null && originCam.GetComponent<ARCameraBackground>() != null));
        }
        else
        {
            Debug.Log("[S5Setup] 警告：找不到 XROrigin，无法补挂管理器");
        }
        GameObject hud = new GameObject("ARDebugHud");
        hud.AddComponent<ARDebugHud>();

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
        };
        AssetDatabase.SaveAssets();

        Debug.Log("[S5Setup] 校验 session=" + (Object.FindObjectOfType<ARSession>() != null) +
                  " xrorigin=" + (Object.FindObjectOfType<XROrigin>() != null) +
                  " planeMgr=" + (Object.FindObjectOfType<ARPlaneManager>() != null) +
                  " hud=" + (Object.FindObjectOfType<ARDebugHud>() != null));
        Debug.Log("[S5Setup] 完成：Assets/Scenes/Main.unity 已创建并加入 Build Settings");
    }
}