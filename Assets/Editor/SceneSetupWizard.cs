using UnityEngine;
using UnityEditor;

public class SceneSetupWizard : EditorWindow
{
    [MenuItem("Tools/Setup Kinetic Platformer Scene")]
    public static void SetupScene()
    {
        // 1. Create Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(10f, 1f, 10f);
        
        // 2. Create Wall-running test walls
        GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall1.name = "Wall_Right";
        wall1.transform.position = new Vector3(8f, 5f, 15f);
        wall1.transform.localScale = new Vector3(1f, 10f, 40f);

        GameObject wall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall2.name = "Wall_Left";
        wall2.transform.position = new Vector3(-8f, 5f, 35f);
        wall2.transform.localScale = new Vector3(1f, 10f, 40f);

        // 3. Setup Player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "KineticPlayer";
        player.transform.position = new Vector3(0f, 2f, 0f);

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        KineticPlayerController controller = player.AddComponent<KineticPlayerController>();

        // Create Orientation object for the player
        GameObject orientation = new GameObject("Orientation");
        orientation.transform.parent = player.transform;
        orientation.transform.localPosition = Vector3.zero;

        // 4. Camera Setup
        GameObject mainCamera = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
        if (mainCamera.GetComponent<Camera>() == null) mainCamera.AddComponent<Camera>();
        mainCamera.name = "PlayerCamera";
        
        mainCamera.transform.parent = player.transform;
        mainCamera.transform.localPosition = new Vector3(0f, 0.6f, 0f); // Eye level
        mainCamera.transform.localRotation = Quaternion.identity;

        // Link references via SerializedObject to automate the inspector work
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("playerCamera").objectReferenceValue = mainCamera.GetComponent<Camera>();
        controllerSO.FindProperty("orientation").objectReferenceValue = orientation.transform;
        controllerSO.ApplyModifiedProperties();

        Selection.activeGameObject = player;

        Debug.Log("Kinetic Platformer Scene Setup Complete! Please check the manual steps.");
    }
}
