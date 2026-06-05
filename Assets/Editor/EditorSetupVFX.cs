using UnityEngine;
using UnityEditor;

public class EditorSetupVFX : EditorWindow
{
    [MenuItem("Tools/Setup Kinetic VFX")]
    public static void SetupVFX()
    {
        KineticPlayerController player = FindObjectOfType<KineticPlayerController>();
        if (player == null)
        {
            Debug.LogError("Could not find KineticPlayerController in the scene.");
            return;
        }

        // 1. Create Particle System GameObject
        GameObject vfxObj = new GameObject("WindSpeedVFX");
        vfxObj.transform.parent = player.transform;
        vfxObj.transform.localPosition = Vector3.zero;
        vfxObj.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = vfxObj.AddComponent<ParticleSystem>();
        
        // Main Module
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f; 
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startSize3D = true;
        main.startSizeX = 0.05f; // Thin
        main.startSizeY = 0.05f;
        main.startSizeZ = 1f;    // Stretched
        main.startColor = new Color(1f, 1f, 1f, 0.7f); // Slightly transparent white

        // Velocity over Lifetime (Particles fly backward to simulate player running forward)
        var velocityModule = ps.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.z = new ParticleSystem.MinMaxCurve(-40f, -20f); 

        // Shape Module (A cone/cylinder enveloping the player, emitting backwards)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = 2.5f;
        shape.radiusThickness = 0.1f; // Emits only from the outer edge
        shape.rotation = new Vector3(0, 180, 0); // Point cone backwards

        // Color over Lifetime (Fade in and out smoothly)
        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorModule.color = new ParticleSystem.MinMaxGradient(grad);

        // Emission Module
        var emission = ps.emission;
        emission.rateOverTime = 0f; // We disable this because KineticVFXManager handles it dynamically

        // Renderer Module
        ParticleSystemRenderer renderer = vfxObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; // Stretched Billboard
        renderer.lengthScale = 2f;
        renderer.velocityScale = 0.1f;

        // 2. Add Manager Script to Player
        KineticVFXManager manager = player.gameObject.GetComponent<KineticVFXManager>();
        if (manager == null) manager = player.gameObject.AddComponent<KineticVFXManager>();

        SerializedObject soManager = new SerializedObject(manager);
        soManager.FindProperty("playerController").objectReferenceValue = player;
        soManager.FindProperty("speedLinesParticles").objectReferenceValue = ps;
        soManager.ApplyModifiedProperties();

        // 3. Wire Manager into Player Controller
        SerializedObject soPlayer = new SerializedObject(player);
        soPlayer.FindProperty("vfxManager").objectReferenceValue = manager;
        soPlayer.ApplyModifiedProperties();

        Selection.activeGameObject = vfxObj;
        Debug.Log("Dynamic Anime Speed VFX generated and wired up! Please assign a Default Particle Material manually.");
    }
}
