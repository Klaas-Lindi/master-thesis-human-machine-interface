using System.Collections.Generic;
using UnityEngine;

public class StaticRegion
{
    public int start;
    public int end;
    public int size;

    public List<Vector3> uavPath;
    public Vector3 movement; // difference between highest and lowest point in the path
    public Vector3 movement_abs;

    public StaticRegion(int startIndex, int endIndex, int size)
    {
        this.start = startIndex;
        this.end = endIndex;
        this.size = size;
        this.uavPath = new List<Vector3>();
        this.movement = new Vector3();
        this.movement_abs = new Vector3();
    }

    public void CalculateMovement()
    {
        this.movement = new Vector3(this.uavPath[this.uavPath.Count - 1].x - this.uavPath[0].x,
                                    this.uavPath[this.uavPath.Count - 1].y - this.uavPath[0].y,
                                    this.uavPath[this.uavPath.Count - 1].z - this.uavPath[0].z);
        this.movement_abs = new Vector3(Mathf.Abs(this.movement.x),
                                        Mathf.Abs(this.movement.y),
                                        Mathf.Abs(this.movement.z));
    }


    //// Check for jumps in the uavPath
    //public void CheckSmoothness(float jumpThreshold)
    //{
    //    // Jump is detected if one step is bigger than the threshold times the total movement
    //    for (int i = 0; i < uavPath.Count - 1; i++)
    //    {
    //        if (Mathf.Abs(uavPath[i + 1].x - uavPath[i].x) > jumpThreshold * movement_abs.x)
    //        {
    //            smooth[0] = false;
    //            break;
    //        }
    //        else
    //        {
    //            smooth[0] = true;
    //        }

    //        if (Mathf.Abs(uavPath[i + 1].y - uavPath[i].y) > jumpThreshold * movement_abs.y)
    //        {
    //            smooth[1] = false;
    //            break;
    //        }
    //        else
    //        {
    //            smooth[1] = true;
    //        }

    //        if (Mathf.Abs(uavPath[i + 1].z - uavPath[i].z) > jumpThreshold * movement_abs.z)
    //        {
    //            smooth[2] = false;
    //            break;
    //        }
    //        else
    //        {
    //            smooth[2] = true;
    //        }
    //    }

    //}

    public override string ToString()
    {
        return "(" + start + ", " + end + ")\t\tSize: " + size + "\tMovement(X,Y,Z): " + movement; // + "\t" +
            //"Smooth(X,Y,Z): " + smooth[0] + "," + smooth[1] + "," + smooth[2];
    }
}