using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System;
using System.Text;
using System.Threading;

/// <summary>
/// In this script all the telemetry data and telecommands of the UAV
/// will be processed and handled. This module sends and receive all data.
/// Furhtermore this script shares the state to all other modules 
/// </summary>
public class TelemetryAndTelecommand : MonoBehaviour {

    // Animator and visualization class
    //private UavPoseAnimator dronePoseAnimator;
    private GNC gnc;
    // Current state of current uav
    private UavState currentUavState;

    //Network Settings
    [Tooltip("Activate online or offline mode")]
    public bool onlineMode = false;

    [Tooltip("IP-Address of the target uav/vehicle. Not used in offline mode")]
    public string uavIp = "192.168.0.12";    

    // Network connection for guidance
    private UdpClient clientGuidance = new UdpClient();

    // Netwokr connection for the TeleoperatorControl
    private TcpClient clientTeleoperatorControl = new TcpClient();
    private NetworkStream streamTeleoperatorControl;
    private StreamReader inputTeleoperatorControl;
    private StreamWriter outputTeleoperatorControl;
    private static bool connecting = false;
    private bool internIsConnected = false;
    private static bool connectFlag = false;
    private bool reconnect = false;

    // Network telemetry data
    //private byte[] readData = new byte[24];
    Quaternion tmpRotation;
    Vector3 tmpPosition;

    // Network teleoperator command
    private byte[] readControl = new byte[1024];
    string tmpMsg = "";


    //Connection handling
    private float CYCLE_TIME = 0.030f; // ms
    private float cycle_time = 0.0f;

    private int callback_counter_receive_control = 0;
    private int callback_counter_recieve_guidance = 0;
    private int callback_limit = 5;

    // Use this for uav initialization
    private void Init()
    {
        // Preset initial states
        currentUavState.Condition = UavState.UavCondition.LANDED;
        currentUavState.OperationState = UavState.UavOperationState.IDLE;
        currentUavState.Mode = UavState.UavMode.WAITING;
    }

    // Use this for unity initialization
    void Start()
    {
        // Get current uav state
        currentUavState = this.GetComponent<UavState>();
        //Get gnc state control
        gnc = this.GetComponent<GNC>();

        // Initial UAV 
        this.Init();
    }

    // Update is called once per frame
    void Update() {

        if (onlineMode)
        {
            if (!internIsConnected)
            {
                if(reconnect)
                {
                    this.Init();
                    reconnect = false;
                }

                // Connect to teleoperator control
                try
                {
                    this.connectTeleoperator();

                    // Initial Requests
                    if (internIsConnected)
                    {
                        gnc.Command = "T:START_GUIDANCE";
                    }
                    //this.currentUavState.IsConnected = true;
                }
                catch (SocketException e)
                {
                    this.currentUavState.IsConnected = false;
                    print(e.Message + "\n Pleas verify connection and Restart Programm");
                }
            }
        }
        else
        {
            // Debug or Simulation mode
            this.currentUavState.IsConnected = true;
            this.currentUavState.Battery = 100f;
            this.currentUavState.ControlState = UavState.UavControlState.TELEOPERATION;
        }

        // Handle TMTC when connected
        if (this.currentUavState.IsConnected && onlineMode && currentUavState.Mode != UavState.UavMode.REBOOT)
        {
            if (Time.realtimeSinceStartup > cycle_time)
            {   
                // -------------- Handle Control Commands --------------------
                if (gnc.Command.Length != 0)
                {
                    if(!gnc.Command.Contains("O:LM"))
                        print(gnc.Command + "->Teleoperator");
                    try
                    {
                        streamTeleoperatorControl.Write(Encoding.ASCII.GetBytes(gnc.Command + "\n"), 0, gnc.Command.Length + 1);
                    }
                    catch(IOException e)
                    {
                        this.currentUavState.IsConnected = internIsConnected = connectFlag = connecting = false;
                        reconnect = true;
                        this.closeTeleoperator();
                        return;
                    }
                    gnc.Command = "";
                }

                if (callback_counter_receive_control < callback_limit)
                {
                    streamTeleoperatorControl.BeginRead(readControl, 0, readControl.Length, new AsyncCallback(readTeleoperatorControlCallback), streamTeleoperatorControl);
                    callback_counter_receive_control++;
                }

                if (currentUavState.Mode == UavState.UavMode.GUIDANCE_ACTIVE)
                {
                    // -------------- Handle Guidance Commands -------------------
                    float[] array = new float[8];
                    array[0] = -999999; //0x00FF0F0F;
                    // Send current position

                    // Send current orientation in NED Coordinates
                    Quaternion rotation = gnc.GNCPose.rotation;
                    // pitch
                    if (rotation.eulerAngles.x < 90)
                        array[1] = rotation.eulerAngles.x;
                    else
                        array[1] = (rotation.eulerAngles.x - 360);
                    // yaw
                    array[2] = (360 - rotation.eulerAngles.y) % 360;
                    // roo
                    array[3] = rotation.eulerAngles.z; //For simplification

                    // Send in NED coordinates
                    Vector3 position = gnc.GNCPose.position;
                    array[4] = position.z; // x 
                    array[5] = position.x;  // y
                    array[6] = position.y;  // z

                    array[7] = 999999;//0xFF00F0F0;
                    int width = sizeof(float);
                    byte[] data = new byte[array.Length * width];// + 2];
                    //data[0] = 0xF0; // Beginn byte
                    for (int i = 0; i < array.Length; ++i)
                    {
                        byte[] converted = BitConverter.GetBytes(array[i]);

                        for (int j = 0; j < width; ++j)
                        {
                            data[i * width + j] = converted[j];
                        }

                    }

                    try
                    {
                        clientGuidance.BeginSend(data, data.Length, new AsyncCallback(writeGuidanceCallback), clientGuidance);
                        
                        if (callback_counter_recieve_guidance < callback_limit)
                        {
                            clientGuidance.BeginReceive(new AsyncCallback(readGuidanceCallback), clientGuidance);
                            callback_counter_recieve_guidance++;
                        }
                    }
                    catch (SocketException e)
                    {
                        if (currentUavState.Mode != UavState.UavMode.GUIDANCE_STOP)
                        {
                            print(e.Message);
                        }
                    }
                }
                //float delta_time = (Time.realtimeSinceStartup - control_time);
                //print("Time: " + delta_time + " s ( " + (1 / delta_time) + " fps )");
                cycle_time = Time.realtimeSinceStartup + CYCLE_TIME;
                //control_time = Time.realtimeSinceStartup;          
            }
            // Set values for gimbal
            currentUavState.CameraPose.rotation = tmpRotation;

            // Set values for uav
            currentUavState.SetVehiclePosition(tmpPosition);
        }

        // Save if control message was received 
        if (tmpMsg.Length > 0)
        {
            currentUavState.Msg = tmpMsg;
            tmpMsg = "";
        }
    }

    public void OnApplicationQuit()
    {
        //this.closeTeleoperator();
        tmpMsg = "";
    }

    public void initializeGuidanceConnection()
    {
        // Initialize Connection to client
        if (onlineMode)
        {
            // Connect to guidance
            clientGuidance = new UdpClient();
            clientGuidance.Connect(new IPEndPoint(IPAddress.Parse(uavIp), 9050));
            tmpRotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

    public void closeGuidanceConnection()
    {
        // Initialize Connection to client
        if (onlineMode)
        {
            // Close to guidance
            clientGuidance.Close();
        }
    }

    public void closeTeleoperator()
    {
        this.closeGuidanceConnection();

        outputTeleoperatorControl.Close();
        inputTeleoperatorControl.Close();
        streamTeleoperatorControl.Close();
        clientTeleoperatorControl.Close();
    }

    public void connectTeleoperator()
    {
        if (!connecting)
        {
            connectFlag = false;
            clientTeleoperatorControl = new TcpClient();
            clientTeleoperatorControl.BeginConnect(IPAddress.Parse(this.uavIp), 9051, new AsyncCallback(connectTeleoperatorCallback), clientTeleoperatorControl);
            connecting = true;
        }
        // Connection established
        if (connectFlag)
        {
            streamTeleoperatorControl = clientTeleoperatorControl.GetStream();
            inputTeleoperatorControl = new StreamReader(clientTeleoperatorControl.GetStream());
            outputTeleoperatorControl = new StreamWriter(clientTeleoperatorControl.GetStream());
            internIsConnected = true;
            this.currentUavState.IsConnected = true;
        }

        // Scynchron
        //clientTeleoperatorControl = new TcpClient();
        //clientTeleoperatorControl.Connect(new IPEndPoint(IPAddress.Parse(this.uavIp), 9051));
        //streamTeleoperatorControl = clientTeleoperatorControl.GetStream();
        //inputTeleoperatorControl = new StreamReader(clientTeleoperatorControl.GetStream());
        //outputTeleoperatorControl = new StreamWriter(clientTeleoperatorControl.GetStream());
    }

    private static void connectTeleoperatorCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            TcpClient client = (TcpClient)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            if(client.Connected)
            {
                connectFlag = true;
            }
            else
            {
                connecting = false;
            }

            
        }
        catch (Exception e)
        {
            connecting = false;
        }
    }

    private void writeGuidanceCallback(IAsyncResult result)
    {
        //callback_counter_send_guidance--;
        System.IO.Stream selectedStream = (System.IO.Stream)result.AsyncState;

        // EndWrite() must always be called if BeginWrite() was used!
        selectedStream.EndWrite(result);
    }

    private void readGuidanceCallback(IAsyncResult result)
    {
        float[] array = new float[8];
        //int width = sizeof(float);
        //byte[] data = new byte[array.Length * width];
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 9050);
        byte[] dataudp = clientGuidance.EndReceive(result, ref RemoteIpEndPoint);

        //print(dataudp[0]);
        if (result.IsCompleted)
        {
            
            array[0] = BitConverter.ToSingle(dataudp, 0); 
            array[1] = BitConverter.ToSingle(dataudp, 4); 
            array[2] = BitConverter.ToSingle(dataudp, 8); 
            array[3] = BitConverter.ToSingle(dataudp, 12); 
            array[4] = BitConverter.ToSingle(dataudp, 16); 
            array[5] = BitConverter.ToSingle(dataudp, 20); 
            array[6] = BitConverter.ToSingle(dataudp, 24); 
            array[7] = BitConverter.ToSingle(dataudp, 28);

            //print(receive_counter++);
            //print(array[0] + "| " + array[1] + "; " + array[2] + "; " + array[3] + " | " + array[4] + "; " + array[5] + "; " + array[6] + "| " + array[7]);
            // Corect message?
            if (array[0] == -999999 && array[7] == 999999)
            {
                // Convert back to unity angles ToDo: also on TMTC there is a transformation. Is this needed?
                if (array[1] > 0)
                    array[1] = -360 + array[1];

                //print(array[1] + "; " + array[2] + "; " + array[3] + " | " + array[4] + "; " + array[5] + "; " + array[6]);
                //print(array[1]);
                //print(array[2]);

                if (array[2] < 0)
                    array[2] = -array[2] % -360;
                else
                    array[2] = (360 - (array[2] % 360)) % 360;

                // Transform back from NED to Unity coordinates
                this.tmpRotation = Quaternion.Euler(array[1], array[2], array[3]);
                this.tmpPosition = new Vector3(array[5], array[6], array[4]);
            }

        }

        callback_counter_recieve_guidance--;
    }

    private void readTeleoperatorControlCallback(IAsyncResult result)
    {
        System.IO.Stream selectedStream = (System.IO.Stream)result.AsyncState;

        if (result.IsCompleted)
        {
            int numberOfBytes = selectedStream.EndRead(result);
            tmpMsg = new string(Encoding.ASCII.GetChars(readControl, 0, numberOfBytes));
            if (tmpMsg.Length > 0)
            {
                foreach (string msg in tmpMsg.Split('\n'))
                {
                    if (msg.Contains("T:LM"))
                        break;
                    print("Teleoperator->" + msg);
                }
            }
            //readControl = new byte[1024];
        }
        callback_counter_receive_control--;
    }
}
