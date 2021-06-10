using UnityEngine;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Inspector changes for PMAnaylse for testing the path prediction, predictive collision detection and automatism handling
/// </summary>
[CustomEditor(typeof(PMHandler))]
public class PMHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw everything else
        DrawDefaultInspector();

        //Get Script 
        PMHandler myScript = (PMHandler)target;

        // Set GUI elements for PID model
        if(myScript.model == PMHandler.Model.PID)
        {
            PMPIDProperties properties = (PMPIDProperties)myScript.GetModelProperties();

            if (properties == null)
                properties = new PMPIDProperties();

            GUILayout.Label("PID Position:");

            // X-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position.X.P = EditorGUILayout.FloatField(properties.Position.X.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position.X.I = EditorGUILayout.FloatField(properties.Position.X.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position.X.D = EditorGUILayout.FloatField(properties.Position.X.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Y-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Y-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position.Y.P = EditorGUILayout.FloatField(properties.Position.Y.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position.Y.I = EditorGUILayout.FloatField(properties.Position.Y.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position.Y.D = EditorGUILayout.FloatField(properties.Position.Y.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Z-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Z-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position.Z.P = EditorGUILayout.FloatField(properties.Position.Z.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position.Z.I = EditorGUILayout.FloatField(properties.Position.Z.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position.Z.D = EditorGUILayout.FloatField(properties.Position.Z.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Velocity Limits
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Maximum Velocity Height");
            GUI.enabled = false;
            properties.MaxVelocityHeight = EditorGUILayout.FloatField(properties.MaxVelocityHeight);
            GUI.enabled = true;
            GUILayout.Label("Maximum Velocity Plane");
            GUI.enabled = false;
            properties.MaxVelocityPlane = EditorGUILayout.FloatField(properties.MaxVelocityPlane);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("PID Rotation:");

            // X-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Rotation.X.P = EditorGUILayout.FloatField(properties.Rotation.X.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Rotation.X.I = EditorGUILayout.FloatField(properties.Rotation.X.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Rotation.X.D = EditorGUILayout.FloatField(properties.Rotation.X.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Y-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Y-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Rotation.Y.P = EditorGUILayout.FloatField(properties.Rotation.Y.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Rotation.Y.I = EditorGUILayout.FloatField(properties.Rotation.Y.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Rotation.Y.D = EditorGUILayout.FloatField(properties.Rotation.Y.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Z-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Z-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Rotation.Z.P = EditorGUILayout.FloatField(properties.Rotation.Z.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Rotation.Z.I = EditorGUILayout.FloatField(properties.Rotation.Z.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Rotation.Z.D = EditorGUILayout.FloatField(properties.Rotation.Z.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Limits
            Vector3 limits = new Vector3();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Limits:");
            GUILayout.Label("X");
            GUI.enabled = false;
            limits.x = EditorGUILayout.FloatField(properties.MaxVelocityRotation.x);
            GUI.enabled = true;
            GUILayout.Label("Y");
            GUI.enabled = false;
            limits.y = EditorGUILayout.FloatField(properties.MaxVelocityRotation.y);
            GUI.enabled = true;
            GUILayout.Label("Z");
            GUI.enabled = false;
            limits.z = EditorGUILayout.FloatField(properties.MaxVelocityRotation.z);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            properties.MaxVelocityRotation = limits;


            EditorGUILayout.BeginHorizontal();
            // Delay
            GUILayout.Label("PID Delay");
            GUI.enabled = false;
            properties.Delay = EditorGUILayout.FloatField(properties.Delay);
            GUI.enabled = true;
            // Waypoints
            GUILayout.Label("Waypoint Acceptance Range");
            GUI.enabled = false;
            properties.WaypointRangeOfAcceptance = EditorGUILayout.FloatField(properties.WaypointRangeOfAcceptance);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            //myScript.SetModelProperties((object)properties);
        }

        // Set GUI elements for AdvPID model
        if (myScript.model == PMHandler.Model.AdvPID)
        {
            PMAdvPIDProperties properties = (PMAdvPIDProperties)myScript.GetModelProperties();

            if (properties == null)
                properties = new PMAdvPIDProperties();

            GUILayout.Label("PID Position:");

            // X-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position.X.P = EditorGUILayout.FloatField(properties.Position.X.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position.X.I = EditorGUILayout.FloatField(properties.Position.X.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position.X.D = EditorGUILayout.FloatField(properties.Position.X.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Y-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Y-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position.Y.P = EditorGUILayout.FloatField(properties.Position.Y.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position.Y.I = EditorGUILayout.FloatField(properties.Position.Y.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position.Y.D = EditorGUILayout.FloatField(properties.Position.Y.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Z-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Z-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position.Z.P = EditorGUILayout.FloatField(properties.Position.Z.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position.Z.I = EditorGUILayout.FloatField(properties.Position.Z.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position.Z.D = EditorGUILayout.FloatField(properties.Position.Z.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("PID Rotation:");
            // X-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Rotation.X.P = EditorGUILayout.FloatField(properties.Rotation.X.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Rotation.X.I = EditorGUILayout.FloatField(properties.Rotation.X.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Rotation.X.D = EditorGUILayout.FloatField(properties.Rotation.X.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Y-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Y-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Rotation.Y.P = EditorGUILayout.FloatField(properties.Rotation.Y.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Rotation.Y.I = EditorGUILayout.FloatField(properties.Rotation.Y.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Rotation.Y.D = EditorGUILayout.FloatField(properties.Rotation.Y.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Z-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Z-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Rotation.Z.P = EditorGUILayout.FloatField(properties.Rotation.Z.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Rotation.Z.I = EditorGUILayout.FloatField(properties.Rotation.Z.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Rotation.Z.D = EditorGUILayout.FloatField(properties.Rotation.Z.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Limits:");
            // Velocity Limits
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Maximum Velocity height");
            GUI.enabled = false;
            properties.MaxVelocityHeight = EditorGUILayout.FloatField(properties.MaxVelocityHeight);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Maximum velocity plane");
            GUI.enabled = false;
            properties.MaxVelocityPlane = EditorGUILayout.FloatField(properties.MaxVelocityPlane);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            // Limits
            Vector3 limits = new Vector3();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Maximum rotation speed:");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            GUI.enabled = false;
            limits.x = EditorGUILayout.FloatField(properties.MaxVelocityRotation.x);
            GUI.enabled = true;
            GUILayout.Label("Y");
            GUI.enabled = false;
            limits.y = EditorGUILayout.FloatField(properties.MaxVelocityRotation.y);
            GUI.enabled = true;
            GUILayout.Label("Z");
            GUI.enabled = false;
            limits.z = EditorGUILayout.FloatField(properties.MaxVelocityRotation.z);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            properties.MaxVelocityRotation = limits;

            Vector3 acc = new Vector3();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Maximum acceleration position:");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            GUI.enabled = false;
            acc.x = EditorGUILayout.FloatField(properties.Acc.x);
            GUI.enabled = true;
            GUILayout.Label("Y");
            GUI.enabled = false;
            acc.y = EditorGUILayout.FloatField(properties.Acc.y);
            GUI.enabled = true;
            GUILayout.Label("Z");
            GUI.enabled = false;
            acc.z = EditorGUILayout.FloatField(properties.Acc.z);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            properties.Acc = acc;

            GUILayout.Label("Bias PID:");
            // X-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position_bias.X.P = EditorGUILayout.FloatField(properties.Position_bias.X.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position_bias.X.I = EditorGUILayout.FloatField(properties.Position_bias.X.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position_bias.X.D = EditorGUILayout.FloatField(properties.Position_bias.X.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Y-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Y-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position_bias.Y.P = EditorGUILayout.FloatField(properties.Position_bias.Y.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position_bias.Y.I = EditorGUILayout.FloatField(properties.Position_bias.Y.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position_bias.Y.D = EditorGUILayout.FloatField(properties.Position_bias.Y.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Z-Axis
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Z-Axis:");
            GUILayout.Label("P");
            GUI.enabled = false;
            properties.Position_bias.Z.P = EditorGUILayout.FloatField(properties.Position_bias.Z.P);
            GUI.enabled = true;
            GUILayout.Label("I");
            GUI.enabled = false;
            properties.Position_bias.Z.I = EditorGUILayout.FloatField(properties.Position_bias.Z.I);
            GUI.enabled = true;
            GUILayout.Label("D");
            GUI.enabled = false;
            properties.Position_bias.Z.D = EditorGUILayout.FloatField(properties.Position_bias.Z.D);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Velocity height bias");
            GUI.enabled = false;
            properties.MaxVelocityHeight_bias = EditorGUILayout.FloatField(properties.MaxVelocityHeight_bias);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            // Delay
            GUILayout.Label("PID Delay");
            GUI.enabled = false;
            properties.Delay = EditorGUILayout.FloatField(properties.Delay);
            GUI.enabled = true;
            // Waypoints
            GUILayout.Label("Waypoint Acceptance Range");
            GUI.enabled = false;
            properties.WaypointRangeOfAcceptance = EditorGUILayout.FloatField(properties.WaypointRangeOfAcceptance);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            //myScript.SetModelProperties((object)properties);
        }

        // Set GUI elements for APG model
        if (myScript.model == PMHandler.Model.APG)
        {
            PMAPGProperties properties = (PMAPGProperties)myScript.GetModelProperties();

            if (properties == null)
                properties = new PMAPGProperties();

            GUILayout.Label("Base Settings:");

            // Mass
            Vector3 m = new Vector3();
            GUILayout.Label("Mass:");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            GUI.enabled = false;
            m.x = EditorGUILayout.FloatField(properties.M.x);
            GUI.enabled = true;
            GUILayout.Label("Y");
            GUI.enabled = false;
            m.y = EditorGUILayout.FloatField(properties.M.y);
            GUI.enabled = true;
            GUILayout.Label("Z");
            GUI.enabled = false;
            m.z = EditorGUILayout.FloatField(properties.M.z);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            properties.M = m;

            // Spring coefficient
            Vector3 k = new Vector3();
            GUILayout.Label("Spring constant:");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            GUI.enabled = false;
            k.x = EditorGUILayout.FloatField(properties.K.x);
            GUI.enabled = true;
            GUILayout.Label("Y");
            GUI.enabled = false;
            k.y = EditorGUILayout.FloatField(properties.K.y);
            GUI.enabled = true;
            GUILayout.Label("Z");
            GUI.enabled = false;
            k.z = EditorGUILayout.FloatField(properties.K.z);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            properties.K = k;

            // Damping coefficient
            Vector3 d = new Vector3();
            GUILayout.Label("Damping constant:");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            GUI.enabled = false;
            d.x = EditorGUILayout.FloatField(properties.D.x);
            GUI.enabled = true;
            GUILayout.Label("Y");
            GUI.enabled = false;
            d.y = EditorGUILayout.FloatField(properties.D.y);
            GUI.enabled = true;
            GUILayout.Label("Z");
            GUI.enabled = false;
            d.z = EditorGUILayout.FloatField(properties.D.z);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            properties.D = d;


            GUILayout.Label("Logical Settings:");

            // Offset
            Vector3 offset = new Vector3();
            GUILayout.Label("Target value offset:");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            GUI.enabled = false;
            offset.x = EditorGUILayout.FloatField(properties.Limit_offset.x);
            GUI.enabled = true;
            GUILayout.Label("Y");
            GUI.enabled = false;
            offset.y = EditorGUILayout.FloatField(properties.Limit_offset.y);
            GUI.enabled = true;
            GUILayout.Label("Z");
            GUI.enabled = false;
            offset.z = EditorGUILayout.FloatField(properties.Limit_offset.z);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            properties.Limit_offset = offset;

            // Waypoints
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Waypoint Acceptance Range");
            GUI.enabled = false;
            properties.WaypointRangeOfAcceptance = EditorGUILayout.FloatField(properties.WaypointRangeOfAcceptance);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            //myScript.SetModelProperties((object)properties);

            GUILayout.Label("Help Variables:");

            // di
            plotComplexLine("Damping Ratio:", new string[] { "X", "Y", "Z" }, properties.Di);
            // w_0
            plotComplexLine("Undamped Angular Frequency:", new string[] { "X", "Y", "Z" }, properties.W_0);
            // di
            plotComplexLine("Angular Frequency:", new string[] { "X", "Y", "Z" }, properties.W_d);
        }

        // Set GUI elements for LSTM model
        if (myScript.model == PMHandler.Model.LSTMSingleOutput)
        {
            PMLSTMProperties properties = (PMLSTMProperties)myScript.GetModelProperties();

            if (properties == null)
                properties = new PMLSTMProperties();

            GUILayout.Label("Model Settings:");
            
            // Model path
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model Path:                 ");
            GUI.enabled = false;
            properties.Model_path = EditorGUILayout.TextField(properties.Model_path);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            // Sample steps
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model samples:           ");
            GUI.enabled = false;
            properties.Model_samples = EditorGUILayout.IntField(properties.Model_samples);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Sample steps
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model predicted step:");
            GUI.enabled = false;
            properties.Predicted_step = EditorGUILayout.IntField(properties.Predicted_step);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

        }

        // Set GUI elements for LSTMv2 model
        if (myScript.model == PMHandler.Model.LSTMSingleOutputLC)
        {
            PMLSTMv2Properties properties = (PMLSTMv2Properties)myScript.GetModelProperties();

            if (properties == null)
                properties = new PMLSTMv2Properties();

            GUILayout.Label("Model Settings:");

            // Model path
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model Path:                 ");
            GUI.enabled = false;
            properties.Model_path = EditorGUILayout.TextField(properties.Model_path);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Sample steps
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model samples:           ");
            GUI.enabled = false;
            properties.Model_samples = EditorGUILayout.IntField(properties.Model_samples);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Sample steps
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model predicted step:");
            GUI.enabled = false;
            properties.Predicted_step = EditorGUILayout.IntField(properties.Predicted_step);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

        }

        // Set GUI elements for LSTMv2 model
        if (myScript.model == PMHandler.Model.LSTMMultiOutputLC)
        {
            PMLSTMMultiProperties properties = (PMLSTMMultiProperties)myScript.GetModelProperties();

            if (properties == null)
                properties = new PMLSTMMultiProperties();

            GUILayout.Label("Model Settings:");

            // Model path
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model Path:                 ");
            GUI.enabled = false;
            properties.Model_path = EditorGUILayout.TextField(properties.Model_path);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Sample steps
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model samples:           ");
            GUI.enabled = false;
            properties.Model_samples = EditorGUILayout.IntField(properties.Model_samples);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Sample steps
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Model predicted step:");
            GUI.enabled = false;
            properties.Predicted_step = EditorGUILayout.IntField(properties.Predicted_step);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Time steps
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Trained time step:     ");
            GUI.enabled = false;
            properties.Trained_time_step = EditorGUILayout.FloatField(properties.Trained_time_step);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

        }


        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

    }

    void plotComplexLine(string description, string[] components_description, PMAPGVector3Complex complex_vector)
    {
        string format = "0.00";

        GUILayout.Label(description);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(components_description[0]);
        GUI.enabled = false;
        EditorGUILayout.TextField(complex_vector.x.Real.ToString(format) + " + i*" + complex_vector.x.Imaginary.ToString(format));
        GUI.enabled = true;
        GUILayout.Label(components_description[1]);
        GUI.enabled = false;
        EditorGUILayout.TextField(complex_vector.y.Real.ToString(format) + " + i*" + complex_vector.y.Imaginary.ToString(format));
        GUI.enabled = true;
        GUILayout.Label(components_description[2]);
        GUI.enabled = false;
        EditorGUILayout.TextField(complex_vector.z.Real.ToString(format) + " + i*" + complex_vector.z.Imaginary.ToString(format));
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }
         
}

// Custom serializable class
[Serializable]
public class LSTMSettings
{
    public string model_path;
    public int model_samples = 5;
    public int predicted_steps = 5;
}

#endif