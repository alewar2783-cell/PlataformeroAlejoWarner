using UnityEngine;
using UnityEditor;

public class EditorFixVFX : EditorWindow
{
    [MenuItem("Tools/Fix VFX Velocity Bug")]
    public static void FixVFX()
    {
        ParticleSystem[] systems = FindObjectsOfType<ParticleSystem>();
        ParticleSystem ps = null;
        
        foreach (var system in systems)
        {
            if (system.gameObject.name == "WindSpeedVFX")
            {
                ps = system;
                break;
            }
        }

        if (ps == null)
        {
            Debug.LogError("Could not find WindSpeedVFX Particle System in the scene.");
            return;
        }

        // The bug is caused because Unity's ParticleSystem.Emit() throws an error 
        // if X, Y, and Z curves in modules like VelocityOverLifetime use different modes.
        var velocityModule = ps.velocityOverLifetime;
        if (velocityModule.enabled)
        {
            // We set Z to RandomBetweenTwoConstants (-40, -20) previously.
            // X and Y defaulted to a flat Constant(0).
            // Fix: Force X and Y to strictly use RandomBetweenTwoConstants as well (even if it's 0 to 0).
            
            var minMaxX = new ParticleSystem.MinMaxCurve(0f, 0f);
            minMaxX.mode = ParticleSystemCurveMode.TwoConstants;
            
            var minMaxY = new ParticleSystem.MinMaxCurve(0f, 0f);
            minMaxY.mode = ParticleSystemCurveMode.TwoConstants;
            
            var minMaxZ = new ParticleSystem.MinMaxCurve(-40f, -20f);
            minMaxZ.mode = ParticleSystemCurveMode.TwoConstants;

            velocityModule.x = minMaxX;
            velocityModule.y = minMaxY;
            velocityModule.z = minMaxZ;
            
            Debug.Log("Fixed Velocity over Lifetime axes to uniform modes (TwoConstants). The Emit() bug should be resolved.");
        }
        else
        {
            Debug.LogWarning("Velocity over Lifetime module was disabled, so no conflict was found there.");
        }
        
        Selection.activeGameObject = ps.gameObject;
    }
}
