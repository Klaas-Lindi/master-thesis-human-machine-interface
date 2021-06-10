using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System;

public class FaceToPlayer : MonoBehaviour {

    public GameObject FPCamera;
    public Boolean gimbalEnabled;
    private Vector3 offsetRot = new Vector3(-90, 0, 0); //new Vector3(90, 180, 0);
    private Vector3 offsetPos = new Vector3(0, 0.98f, 0);
    //Queue<Vector3> targetQueue = new Queue<Vector3>();
    Quaternion tmpRotation;
    private byte[] readData = new byte[12];

    // Network connection
    TcpClient client = new TcpClient();
    NetworkStream stream;
    StreamReader input;
    StreamWriter output;

    // Use this for initialization
    void Start () {
        // Initialize Connection to client
        if (gimbalEnabled)
        {
            client.Connect(new IPEndPoint(IPAddress.Parse("192.168.128.201"), 9050));
            stream = client.GetStream();
            tmpRotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }
	
	// Update is called once per frame
	void Update () {
        // Send current position
        if (gimbalEnabled)
        {
            // Send current position
            Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            float[] array = new float[3];
            if (rotation.eulerAngles.x < 90)
                array[0] = -rotation.eulerAngles.x;
            else
                array[0] = -(rotation.eulerAngles.x - 360);
            array[1] = rotation.eulerAngles.y;
            array[2] = rotation.eulerAngles.z; //For simplification

            int width = sizeof(float);
            byte[] data = new byte[array.Length * width];
            for (int i = 0; i < array.Length; ++i)
            {
                byte[] converted = BitConverter.GetBytes(array[i]);

                //if (BitConverter.IsLittleEndian)
                //{
                //    Array.Reverse(converted);
                //}

                for (int j = 0; j < width; ++j)
                {
                    data[i * width + j] = converted[j];
                }

            }
            
            stream.BeginWrite(data, 0, data.Length, new AsyncCallback(writeCallback), stream);

            stream.BeginRead(readData, 0, data.Length, new AsyncCallback(readCallback), stream);
            /* stream.Read(data, 0, data.Length);


             array[0] = BitConverter.ToSingle(data, 0);
             array[1] = BitConverter.ToSingle(data, 4);
             array[2] = BitConverter.ToSingle(data, 8);

             // Convert back to unity angles
             if (array[0] < 0)
                 array[0] *= -1;
             else
                 array[0] = 360 - array[0];

             this.transform.rotation = Quaternion.Euler(array[0], array[1], array[2]);*/
            this.transform.rotation = tmpRotation;

        }
        else
        {
            //this.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            this.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }

        // Calculate Sphere position
        Vector3 tmp = this.transform.rotation * Vector3.forward;    
        this.transform.position = tmp * 50f;
        
        // Add offset to rotation 
        this.transform.rotation *= Quaternion.Euler(offsetRot);      

        // Add position from player
        this.transform.position += FPCamera.gameObject.transform.position - offsetPos;
    }

    private void writeCallback(IAsyncResult result)
    {
        System.IO.Stream selectedStream = (System.IO.Stream)result.AsyncState;

        // EndWrite() must always be called if BeginWrite() was used!
        selectedStream.EndWrite(result);
    }

    private void readCallback(IAsyncResult result)
    {
        float[] array = new float[3];
        //int width = sizeof(float);
        //byte[] data = new byte[array.Length * width];
        System.IO.Stream selectedStream = (System.IO.Stream)result.AsyncState;

        // EndRead() must always be called if BeginWrite() was used!
        if (result.IsCompleted)
        {

            selectedStream.EndRead(result);

            array[0] = BitConverter.ToSingle(readData, 0);
            array[1] = BitConverter.ToSingle(readData, 4);
            array[2] = BitConverter.ToSingle(readData, 8);

            // Convert back to unity angles
            if (array[0] < 0)
                array[0] *= -1;
            else
                array[0] = 360 - array[0];

            this.tmpRotation = Quaternion.Euler(array[0], array[1], array[2]);
        }
    }
}
