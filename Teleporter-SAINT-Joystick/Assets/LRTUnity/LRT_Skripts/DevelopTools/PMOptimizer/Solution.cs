using System.Collections.Generic;
using UnityEngine;

public class Solution
{
    public PMPIDProperties Properties { get; set; }

    public List<Vector3> Path { get; set; }
    public List<Vector3> Path_interp { get; set; }

    public List<Vector3> Errors { get; set; }
    public Vector3 Error_total { get; set; }

    public List<Vector3> Errors_deriv { get; set; }
    public Vector3 Error_deriv_total { get; set; }


}