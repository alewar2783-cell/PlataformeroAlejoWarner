using UnityEngine;
using UnityEditor;

public class EditorApplyDashFix : EditorWindow
{
    [MenuItem("Tools/Apply Dash Update to Player")]
    public static void ApplyUpdate()
    {
        KineticPlayerController player = FindObjectOfType<KineticPlayerController>();
        
        if (player != null)
        {
            SerializedObject so = new SerializedObject(player);
            
            // Set sensible defaults requested by the user
            so.FindProperty("dashDuration").floatValue = 0.25f;
            so.FindProperty("dashSpeed").floatValue = 45f;
            so.FindProperty("maxAirSpeed").floatValue = 12f;
            so.FindProperty("airAcceleration").floatValue = 15f;
            
            so.ApplyModifiedProperties();
            Selection.activeGameObject = player.gameObject;
            
            Debug.Log("Dash fix applied successfully! Inspector values have been reset to default balanced values.");
        }
        else
        {
            Debug.LogError("Could not find an object with KineticPlayerController in the scene.");
        }
    }
}
