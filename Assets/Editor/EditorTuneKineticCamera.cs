using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

public class EditorTuneKineticCamera : EditorWindow
{
    [MenuItem("Tools/Tune Kinetic Camera")]
    public static void TuneCamera()
    {
        KineticPlayerController player = FindObjectOfType<KineticPlayerController>();
        if (player == null)
        {
            Debug.LogError("Could not find KineticPlayerController in the scene.");
            return;
        }

        // Try to find the active Cinemachine Virtual Camera or FreeLook via Reflection
        Type vcamType = Type.GetType("Unity.Cinemachine.CinemachineVirtualCamera, Unity.Cinemachine") 
                     ?? Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
        
        Type freeLookType = Type.GetType("Unity.Cinemachine.CinemachineFreeLook, Unity.Cinemachine") 
                         ?? Type.GetType("Cinemachine.CinemachineFreeLook, Cinemachine");

        Component camComp = null;
        if (vcamType != null) camComp = FindObjectOfType(vcamType) as Component;
        if (camComp == null && freeLookType != null) camComp = FindObjectOfType(freeLookType) as Component;

        if (camComp == null)
        {
            Debug.LogWarning("Cinemachine Camera not found. Please create one manually.");
            return;
        }

        // Assign Follow and LookAt using Reflection
        PropertyInfo followProp = camComp.GetType().GetProperty("Follow");
        PropertyInfo lookAtProp = camComp.GetType().GetProperty("LookAt");
        
        if (followProp != null) followProp.SetValue(camComp, player.transform);
        if (lookAtProp != null) lookAtProp.SetValue(camComp, player.transform);

        // We use SerializedObject to attempt tuning the Damping values safely
        // Note: Cinemachine architecture varies heavily between v2, v3, and Unity 6, 
        // so we attempt to find the common serialized properties.
        SerializedObject so = new SerializedObject(camComp);
        
        // Typical FreeLook parameters
        SerializedProperty orbits = so.FindProperty("m_Orbits");
        if (orbits != null && orbits.arraySize >= 3)
        {
            // Just basic orbit adjustments for 3rd person
            orbits.GetArrayElementAtIndex(1).FindPropertyRelative("m_Radius").floatValue = 6f; // Middle Rig
            so.ApplyModifiedProperties();
        }

        Debug.Log("Success! The Player was automatically assigned to " + camComp.gameObject.name + ". Check the console for manual tuning instructions.");
        Selection.activeGameObject = camComp.gameObject;
    }
}
