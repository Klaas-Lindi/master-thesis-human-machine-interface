using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class PMVehicle {

    // External dependencies
    protected UavState currentUavState;
    protected OperatorState operatorState;
    protected PMModel pmModelWrapper;

    // Vehicle Settings
    private PMVehicleSettings pmVehicleSettings;

    public PMVehicle()
    {
        this.currentUavState = null;
        this.operatorState = null;
        this.pmModelWrapper = null;
    }

    public PMVehicle(UavState vehicleState, OperatorState operatorState, PMModel pmModelWrapper)
    {
        this.currentUavState = vehicleState;
        this.operatorState = operatorState;
        this.pmModelWrapper = pmModelWrapper;
    }

    /// <summary>
    /// Set the properties of the curent vehile
    /// </summary>
    /// <param name="obj">obj containing the parameter variables</param>
    virtual public void SetVehicleProperties(object obj)
    { 
        if (obj == typeof(PMVehicleSettings))
        {
            this.pmVehicleSettings = (PMVehicleSettings)obj; 
        }
    }

    /// <summary>
    /// Get the properties of the curent vehile
    /// </summary>
    /// <returns>Returns object of property class. null if there is no property</returns>
    virtual public object GetVehicleProperties()
    {
        if (pmVehicleSettings != null)
        {
            return pmVehicleSettings;
        }
        return null;
    }

    /// <summary>
    /// This function handles all automatism of the uav and set the current operator pose
    /// </summary>
    virtual public void HandleAutomatism()
    {
        pmModelWrapper.SetCurrentOperatorPose(operatorState.OperatorPose);
    }

    /// <summary>
    /// Reset the vehicle settings and preknowledge
    /// </summary>
    virtual public void Reset()
    {

    }
}

public class PMVehicleSettings
{
    public float waypointAcceptanceRange;

    public PMVehicleSettings()
    {
        this.waypointAcceptanceRange = 1;
    }

    public PMVehicleSettings(float waypointAcceptanceRange)
    {
        this.waypointAcceptanceRange = waypointAcceptanceRange;
    }
}

