using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudViewer : MonoBehaviour
{
    ParticleSystem.Particle[] cloud;
    bool bPointsUpdated = false;
    ParticleSystem particles;

    public float pointSize = 0.1f;
    public uint filter = 2;
    public Color32 pointColor = Color.blue;

    public bool fakePose;
    public GameObject poseObject;


    void Start()
    {
        particles = this.GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (bPointsUpdated)
        {
            //Debug.Log(cloud[1].position);
            particles.SetParticles(cloud, cloud.Length);
            bPointsUpdated = false;
        }

        if(fakePose)
        {
            gameObject.transform.position = poseObject.transform.position;
            gameObject.transform.rotation = poseObject.transform.rotation;
            gameObject.transform.Rotate(new Vector3(0, -90, 0));
            gameObject.transform.localPosition += new Vector3(0.05f, 0f,-0.055f);
        }
    }

    public void SetPoints(Vector3[] positions)
    {
        cloud = new ParticleSystem.Particle[(uint)(positions.Length / filter)];
        for (uint ii = 0; ii < positions.Length - filter; ii += filter)
        {
            cloud[(uint)(ii / filter)].position = positions[ii];
            cloud[(uint)(ii / filter)].startColor = pointColor;
            cloud[(uint)(ii / filter)].startSize = pointSize;
            //Debug.Log(cloud[ii].position);
        }

        bPointsUpdated = true;
    }

    public void SetPoints(Vector3[] positions, Color32[] colors)
    {
        
        cloud = new ParticleSystem.Particle[(uint)(positions.Length/ filter)];
        for (uint ii = 0; ii < positions.Length - filter; ii += filter)
        {
            cloud[(uint)(ii / filter)].position = positions[ii];
            cloud[(uint)(ii / filter)].startColor = colors[ii];
            cloud[(uint)(ii / filter)].startSize = pointSize;
            //Debug.Log(cloud[ii].position);
        }


        bPointsUpdated = true;
    }
}
