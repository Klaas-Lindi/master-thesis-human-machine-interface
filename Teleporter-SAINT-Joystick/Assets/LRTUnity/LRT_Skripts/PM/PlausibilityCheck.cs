using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class checks the input of the operator with the preknown data of the 
/// environement and checks collision with the predictive path. If there is a discrepancy the command should not be 
/// executed
/// </summary>
public class PlausibilityCheck : MonoBehaviour {

    // Contains the mesh of the environment
    public MeshHandler meshHandler;

    // UAV porperties
    public float radiusUAV = 1;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// Check collision with environment and return results
    /// </summary>
    /// <param name="predictedPath">The waypoints of the predicted path</param>
    /// <returns>Returns the collision state and the position of intersection. If no collision occurs None will be returned</returns>
    internal PredictedCollision Check(List<Vector3> predictedPath)
    {
        // Initialize result with status none
        PredictedCollision checkCollision = new PredictedCollision();

        // Go through all waypoints
        foreach(Vector3 point in predictedPath)
        {
            // Calculate Collision with sphere
            Collider[] collisions = Physics.OverlapSphere(point, this.radiusUAV);

            // Go through collisons and define 
            foreach (Collider col in collisions)
            {
                if(col.gameObject.name == "DOM40")
                {
                    checkCollision.Position = point;
                    checkCollision.Collision = PredictedCollision.CollisionType.DOM;
                    return checkCollision;
                }
            }
        }

        return checkCollision;
    }
}

/// <summary>
/// This class contaions states of Collision and where they occur
/// </summary>
internal class PredictedCollision
{
    /// <summary>
    /// CollisionType: None, Reconstruction, DOM, MapBox, Geofence
    /// </summary>
    public enum CollisionType { None, Reconstruction, DOM, MapBox, Geofence}

    // Internal variables
    private Vector3 position;
    private CollisionType collisionType;

    /// <summary>
    /// Get or set the position of collision
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return position;
        }

        set
        {
            position = value;
        }
    }

    /// <summary>
    /// Get or Set collsion type
    /// </summary>
    internal CollisionType Collision
    {
        get
        {
            return collisionType;
        }

        set
        {
            collisionType = value;
        }
    }

    /// <summary>
    /// Initialize variable with position in origin and type none
    /// </summary>
    public PredictedCollision()
    {
        this.position = new Vector3();
        this.Collision = new CollisionType();
    }
}