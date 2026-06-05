using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class EditorSetupThirdPerson : EditorWindow
{
    [MenuItem("Tools/Setup Third Person & UI")]
    public static void SetupThirdPerson()
    {
        KineticPlayerController player = FindObjectOfType<KineticPlayerController>();
        if (player == null)
        {
            Debug.LogError("Could not find KineticPlayerController in scene.");
            return;
        }

        // 1. Create World Space UI Canvas
        GameObject canvasObj = new GameObject("WorldSpaceStaminaBar");
        canvasObj.transform.parent = player.transform;
        canvasObj.transform.localPosition = new Vector3(0.6f, 2.0f, 0f); // Default over right shoulder
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 15);
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        canvasObj.AddComponent<CanvasScaler>();

        // Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.parent = canvasObj.transform;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgRect.localScale = Vector3.one;
        bgRect.localPosition = Vector3.zero;

        // Fill Image (Slider)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.parent = canvasObj.transform;
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 1f, 0.4f, 1f); // Bright kinetic green
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.localScale = Vector3.one;
        fillRect.localPosition = Vector3.zero;

        // Add Stamina Script
        WorldSpaceStaminaUI uiScript = canvasObj.AddComponent<WorldSpaceStaminaUI>();
        SerializedObject soUI = new SerializedObject(uiScript);
        soUI.FindProperty("player").objectReferenceValue = player;
        soUI.FindProperty("fillImage").objectReferenceValue = fillImage;
        soUI.ApplyModifiedProperties();

        // 2. Try Create Cinemachine VCam safely using Reflection
        GameObject vcamObj = new GameObject("Kinetic_VirtualCamera");
        
        System.Type vcamType = System.Type.GetType("Unity.Cinemachine.CinemachineVirtualCamera, Unity.Cinemachine") 
                            ?? System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
        
        Component vcamComp = null;
        if (vcamType != null)
        {
            vcamComp = vcamObj.AddComponent(vcamType);
            
            // Set Follow & LookAt
            System.Reflection.PropertyInfo followProp = vcamType.GetProperty("Follow");
            System.Reflection.PropertyInfo lookAtProp = vcamType.GetProperty("LookAt");
            if (followProp != null) followProp.SetValue(vcamComp, player.transform);
            if (lookAtProp != null) lookAtProp.SetValue(vcamComp, player.transform);
            
            Debug.Log("Cinemachine Virtual Camera automatically created and configured!");
        }
        else
        {
            Debug.LogWarning("Cinemachine not detected. A generic object was created for the VCam. Please install Cinemachine, attach a Virtual Camera to this object, and drop it into the script reference.");
        }

        // Add Dynamic Camera script
        ThirdPersonKineticCamera dynCam = vcamObj.AddComponent<ThirdPersonKineticCamera>();
        SerializedObject soCam = new SerializedObject(dynCam);
        soCam.FindProperty("player").objectReferenceValue = player;
        if (vcamComp != null) soCam.FindProperty("virtualCamera").objectReferenceValue = vcamComp;
        soCam.ApplyModifiedProperties();

        // 3. Remove old 1st person camera constraints
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // Remove old 1st-person PlayerLook if it exists using Reflection to avoid hard dependency errors
            Component oldLook = mainCam.GetComponent("PlayerLook");
            if (oldLook != null) DestroyImmediate(oldLook);
            
            // Try adding CinemachineBrain
            System.Type brainType = System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine") 
                                 ?? System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
            if (brainType != null && mainCam.GetComponent(brainType) == null)
            {
                mainCam.gameObject.AddComponent(brainType);
            }
            
            // Unparent main camera if it was a child of the player
            if (mainCam.transform.parent == player.transform)
            {
                mainCam.transform.parent = null;
            }
        }

        Selection.activeGameObject = canvasObj;
        Debug.Log("3rd Person & World Space UI Setup Complete! Check the instructions for manual tweaks.");
    }
}
