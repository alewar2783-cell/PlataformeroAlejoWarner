using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class EditorUpgradeStaminaUI : EditorWindow
{
    [MenuItem("Tools/Upgrade Stamina UI")]
    public static void UpgradeUI()
    {
        WorldSpaceStaminaUI uiScript = FindObjectOfType<WorldSpaceStaminaUI>();
        if (uiScript == null)
        {
            Debug.LogError("Could not find WorldSpaceStaminaUI in the scene.");
            return;
        }

        GameObject canvasObj = uiScript.gameObject;
        
        // Find existing Fill and Background
        Transform fillTransform = canvasObj.transform.Find("Fill");
        Transform bgTransform = canvasObj.transform.Find("Background");
        
        if (fillTransform == null)
        {
            Debug.LogError("Could not find the 'Fill' object under the Canvas.");
            return;
        }

        // Duplicate Fill to create Ghost Fill if it doesn't exist yet
        Transform ghostTransform = canvasObj.transform.Find("Ghost Fill");
        if (ghostTransform == null)
        {
            GameObject ghostObj = Instantiate(fillTransform.gameObject, canvasObj.transform);
            ghostObj.name = "Ghost Fill";
            ghostTransform = ghostObj.transform;
            
            // Re-parent so Ghost Fill is between Background and Fill (so it renders behind the main fill)
            if (bgTransform != null)
            {
                ghostTransform.SetSiblingIndex(bgTransform.GetSiblingIndex() + 1);
            }
            else
            {
                ghostTransform.SetSiblingIndex(0);
                fillTransform.SetSiblingIndex(1);
            }
            
            Image ghostImage = ghostObj.GetComponent<Image>();
            if (ghostImage != null)
            {
                ghostImage.color = new Color(1f, 1f, 1f, 0.9f); // Solid White ghost bar
                ghostImage.type = Image.Type.Filled;
                ghostImage.fillMethod = Image.FillMethod.Horizontal;
            }
        }

        // Update the references in the script using SerializedObject so we don't lose them
        SerializedObject so = new SerializedObject(uiScript);
        so.FindProperty("mainFillImage").objectReferenceValue = fillTransform.GetComponent<Image>();
        so.FindProperty("ghostFillImage").objectReferenceValue = ghostTransform.GetComponent<Image>();
        
        // Apply default sensible parameters
        so.FindProperty("ghostShrinkDelay").floatValue = 0.4f;
        so.FindProperty("ghostShrinkSpeed").floatValue = 3f;
        so.FindProperty("mainFillSpeed").floatValue = 15f;

        so.ApplyModifiedProperties();

        Selection.activeGameObject = canvasObj;
        Debug.Log("Stamina UI Upgraded successfully! Ghost bar added. Please configure the Color Gradient manually in the inspector.");
    }
}
