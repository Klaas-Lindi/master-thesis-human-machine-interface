#if COMPILE_ROSSharp

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using std_msgs = RosSharp.RosBridgeClient.Messages.Standard;
using System;
using System.Threading;
//using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;
using RosSharp.RosBridgeClient.Messages.Standard;

namespace RosSharp.RosBridgeClient
{
    public class RosInterfaceSaint : MonoBehaviour
    {
        [Header("Connection Settings")]
        public int Timeout = 10;


        public RosSocket RosSocket { get; private set; }
        public global::System.String RosBridgeServerUrlLab { get => rosBridgeServerUrlLab; set => rosBridgeServerUrlLab = value; }

        public enum Protocols { WebSocketSharp, WebSocketNET };
        public RosBridgeClient.RosSocket.SerializerEnum Serializer;
        public Protocols Protocol;
        string RosBridgeServerUrl = "ws://10.162.234.156:9090"; // "ws://192.168.1.102:9090"; // "ws://192.168.1.102:9090"; //  //"ws://192.168.0.115:9090";  //Niklas' ROS Server IP:        192.168.0.115
        string rosBridgeServerUrlLab = "ws://10.162.234.156:9090";  //Laboratory AM:            10.162.234.156
        string RosBridgeServerUrlFac = "ws://192.168.0.115:9090";  //Facility:                  10.157.11.115       

        [Header("Messages to User")]
        public MessageToUser messageToUser;

        [Header("Operator Settings")]
        public OperatorArmState operatorArm;

        [Header("Robot Settings")]
        public RoboticArmState armState;
        public RobotControlSAINT robotControl;

        [Header("Point Cloud Settings")]
        public PointCloudViewer pointCloudViewer;

        [Header("Image Settings")]
        public VideoStreamROS imageViewer;

        [Header("Longshot Image Settings")]
        public VideoStreamROSLongshot imageViewerLongshot;

        [Header("Semi Autonomous Handler")]
        public SemiautonomousHandler semiAutonomousHandler;

        [Header("Information Settings")]
        public UISaintWorkspaceHandler workspaceViewer;
        public UISaintSequenceViewerHandler sequenceViewer;
        public UISaintSequencePlannerHandler sequencePlanner;
        public UISaintActionViewer actionViewer;

        //RosSocket rosSocket;
        string endEffector_id;
        string endEffectorPoseStamp_id;
        string item_marking_string_id;
        string endEffectorTwistStamp_id;
        string jointState_id;
        string pointCloud_id;
        string image_id;
        string image_id_longshot;
        string operator_command_id;
        string operator_telemetry_id;
        string task_space_pose_id;
        string publication_id;
        string subscription_id;
        string operator_sequence_id;
        string operator_messages_id;
        string operator_callback_id;
        string waypoint_marking_id;
        // Information topics
        string workspace_id;
        string sequence_id;
        string current_actions_id;
        string actions_id;
        // Trajectory
        string trajectory_id;

        private static Pose old_pose;
        private float old_time;

        private bool tmpFlag = false;
        private GameObject helperGameObject;

        private IEnumerator coroutine;
        private bool coroutine_ready = true;
        private bool pclReceived = false;
        private bool tlemetryReceived = false;
        private static string telemetry_message;
        private static string operator_message = "";
        private static string workspace_message = "";
        private static string sequence_message = "";
        private static string current_actions_message = "";
        private static string actions_message = "";
        private static Messages.Geometry.PoseStamped task_space_pose = new Messages.Geometry.PoseStamped();
        private static Vector3 task_space_pose_position;
        private static Quaternion task_space_pose_orientation;

        //Debugging
        int sequence_index = 0;

        // Trajectory
        public LineRenderer RosTrajectory;

		public Vector3[] positionArray;

        private bool isMessageReceived;
		public int numberOfPoses;
		public Messages.PoseArray message;

        // Start is called before the first frame update
        void Start()
        {
            //SubscribeAndAdvertise();

            old_pose = operatorArm.EndEffector;

            helperGameObject = new GameObject();
        }

        // Update is called once per frame
        void Update()
        {
            // Trajectory
            if (isMessageReceived)
                ProcessMessage();
    
            if (!robotControl.saintState.IsConnected)
                return;

            //if (Input.GetKeyDown(KeyCode.S))            //press S key every time you want to send messages to ROS
            //{
            //    std_msgs.String message = new std_msgs.String
            //    {
            //        data = "Message sent from unity"    //UNITY message for ROS
            //    };
            //    RosSocket.Publish(publication_id, message);
            //    Debug.Log("A message was sent to ROS");
            //}

            if (Input.GetKeyDown(KeyCode.T))            //press T key for Testing ROS message simulation with request
            {
                // 1	string	 'true', 'false', 'unknown'	itemDetected
                /*workspace_message = "{ workspace: [" +
                    "{\"number\": 1, \"name\": \"itemDetected\", \"value\": \"true\",  \"options\": ['true', 'false', 'unknown'] }, " +
                    "{\"number\": 2, \"name\": \"graspClosed\", \"value\": \"false\",  \"options\": ['true', 'false', 'unknown'] }, " +
                    "{\"number\": 3, \"name\": \"graspOpen\", \"value\": \"true\", \"options\": ['true', 'false', 'unknown'] }, " +
                    "]}";*/
               sequence_message = "{ currentSequence : " + sequence_index++ + ", sequences: [" +
                    "{\"number\": 1, \"name\": \"detectItem\", \"parameter\": ['0', '1', '10'] }, " +
                    "{\"number\": 2, \"name\": \"closeGripper\", \"parameter\": ['4', '5', '4.5'] }, " +
                    "{\"number\": 3, \"name\": \"openGripper\", \"parameter\": ['true', 'false', 'false'] }, " +
                    "{\"number\": 4, \"name\": \"detectItem\", \"parameter\": ['0', '1', '2'] }, " +
                    "{\"number\": 5, \"name\": \"closeGripper\", \"parameter\": ['4', '5', '4.2'] }, " +
                    "{\"number\": 6, \"name\": \"openGripper\", \"parameter\": ['true', 'false', 'true'] }, " +
                    "]}";

                string actionSet1 = "{ actions: [" +
        "{\"number\": 1, \"label\": \"Detect Item\",   \"cmd\": \"detectItem\",    \"parameter\": [{ label: \"Grip Force\", parameterName: gripForce , currentValue: 10, min: 0, max: 100 }]}," +
        "]}";
                string actionSet5 = "{ actions: [" +
                    "{\"number\": 1, \"action\": \"detectItem\",   \"parameter\": [{}] }, " +
                    "{\"number\": 2, \"action\": \"grasp\",        \"parameter\": [{ name: gripForce, currentValue: 10, min: 0, max: 100 }]}," +
                    "{\"number\": 3, \"action\": \"closeGripper\"}, " +
                    "]}";
                string actionSet2 = "{ actions: [" +
                    "{\"number\": 1, \"action\": \"detectItem\",   \"parameter\": [{}] }, " +
                    "{\"number\": 3, \"action\": \"closeGripper\"}, " +
                    "]}";
                string actionSet3 = "{ actions: []}";
                string actionSet4 = "{ actions: [" +
                    "{\"number\": 1, \"action\": \"detectItem\",   \"parameter\": [{}] }, " +
                    "{\"number\": 2, \"action\": \"grasp\",        \"parameter\": [{ name: gripForce, currentValue: 10, min: 0, max: 100 }]}," +
                    "{\"number\": 3, \"action\": \"objectRecognize\", \"parameter\": [{ name: posx, currentValue: 10, min: 0, max: 100 },{ name: posy, currentValue: 10, min: 0, max: 100 }]}," +
                    "]}";

                string[] actionSet = { actionSet1, actionSet2, actionSet3, actionSet4 };

                actions_message = actionSet[sequence_index % 4];
            }

            if (robotControl.Command.Length > 0)
            {
                //print(robotControl.Command);
                std_msgs.String message = new std_msgs.String
                {
                    data = robotControl.Command    //UNITY message for ROS
                };
                RosSocket.Publish(operator_command_id, message);

                robotControl.Command = "";
            }

            if (robotControl.WaypointMarking.sqrMagnitude > 0)
            {
                Vector3 waypoint_postion = robotControl.WaypointMarking.Unity2Ros();
                //Messages.PoseArray[] waypoint_message = new Messages.PoseArray[1];
                Messages.Geometry.Point waypoint_message = new Messages.Geometry.Point();
                waypoint_message.x = robotControl.WaypointMarking.x;
                waypoint_message.y = robotControl.WaypointMarking.y;
                waypoint_message.z = robotControl.WaypointMarking.z;

                RosSocket.Publish(waypoint_marking_id, waypoint_message);

                robotControl.WaypointMarking = Vector3.zero;
            }

            if (robotControl.Callback.Length > 0)
            {
                //print(robotControl.Command);
                std_msgs.String message = new std_msgs.String
                {
                    data = robotControl.Callback    //UNITY message for ROS
                };
                RosSocket.Publish(operator_callback_id, message);
                semiAutonomousHandler.Callback = robotControl.Callback;
                robotControl.Callback = "";
            }

            if (robotControl.Sequence.Length > 0)
            {
                //print(robotControl.Command);
                std_msgs.String message = new std_msgs.String
                {
                    data = robotControl.Sequence    //UNITY message for ROS
                };
                RosSocket.Publish(operator_sequence_id, message);

                robotControl.Sequence = "";
            }

            if (robotControl.ItemSelect.Length > 0)
            {
                //print(robotControl.ItemSelect);
                std_msgs.String message = new std_msgs.String
                {
                    data = robotControl.ItemSelect    //UNITY message for ROS
                };
                RosSocket.Publish(item_marking_string_id, message);
                semiAutonomousHandler.Coordinates = robotControl.ItemSelect;
                robotControl.ItemSelect = "";
            }

            if (operatorArm.EndEffector.position != null)
            {
                Vector3 transfer_postion = operatorArm.EndEffector.position.Unity2Ros();
                Quaternion transfer_rotation = operatorArm.EndEffector.rotation.Unity2Ros();

                // Time stamp pose
                Messages.Geometry.PoseStamped message_pose = new Messages.Geometry.PoseStamped();
                message_pose.header.stamp.secs = (uint)Math.Round(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000);
                message_pose.header.stamp.nsecs = (uint)DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds * 1000;
                message_pose.pose.position.x = transfer_postion.x;
                message_pose.pose.position.y = transfer_postion.y;
                message_pose.pose.position.z = transfer_postion.z;

                // stamped roation
                Messages.Geometry.Quaternion tmp_angle = new Messages.Geometry.Quaternion();
                tmp_angle.w = transfer_rotation.w;
                tmp_angle.x = transfer_rotation.x;
                tmp_angle.y = transfer_rotation.y;
                tmp_angle.z = transfer_rotation.z;
                message_pose.pose.orientation = tmp_angle;
                RosSocket.Publish(endEffectorPoseStamp_id, message_pose);

                // Save changes
                old_pose = new Pose();
                old_pose.position = transfer_postion;
                old_pose.rotation = transfer_rotation;
                old_time = UnityEngine.Time.realtimeSinceStartup;
            }

            if (pclReceived)
            {
                StartCoroutine(coroutine);
                pclReceived = false;
                Debug.Log("Start Coroutine");
            }

            if (workspace_message.Length > 0)
            {
                workspaceViewer.setContent(workspace_message);
                workspace_message = "";
            }

            if (sequence_message.Length > 0)
            {
                sequenceViewer.setContent(sequence_message);
                // sequencePlanner.setContent(sequence_message);
                sequence_message = "";
            }

            if (current_actions_message.Length > 0)
            {
                sequencePlanner.setContent(current_actions_message);
                current_actions_message = "";
            }

            if (actions_message.Length > 0)
            {
                actionViewer.setActions(actions_message);
                actions_message = "";
            }

            if (operator_message.Length > 0)
            {
                if (operator_message == "Coordinates received, please wait")
                {
                    semiAutonomousHandler.CoordinatesReceived();
                }
                messageToUser.setMessage(operator_message);
                string[] words = operator_message.Split(' ');
                if (words[0] == "close_window")
                {
                    semiAutonomousHandler.DatabaseInput(words[1]);
                }
                operator_message = "";
            }

            if (telemetry_message.Length > 0)
            {
                string[] words = telemetry_message.Split(' ');
                if (words[0] == "check_box_empty" || words[0] == "check_gripper" || words[0] == "detect_obstacle" || words[0] == "manual_barcode_scan")            // words[1] = number of buttons(1-3); words[2] = type of camera wanted ("longshot" or "ego"); words[3] = text button 1; words[4] = text button 2; words[5] = text button 3
                {
                    semiAutonomousHandler.StartMiniApp(telemetry_message);
                }
                if (words[0] == "stop_check_box_empty" || words[0] == "stop_check_gripper" || words[0] == "stop_detect_obstacle" || words[0] == "stop_manual_barcode_scan")
                {
                    semiAutonomousHandler.StopMiniApp(telemetry_message);
                }
                if (words[0] == "received") semiAutonomousHandler.CallbackReceived();

            }

            robotControl.Msg = telemetry_message;
            //if (telemetry_message != (""))
            //{
            //    System.Threading.Thread.Sleep(100);
            //}
            telemetry_message = "";

            robotControl.TaskSpacePosePosition = task_space_pose_position;
            robotControl.TaskSpacePoseOrientation = task_space_pose_orientation;

            /* if (Input.GetKeyDown(KeyCode.C))            //press C key to close the connection
             {
                 RosSocket.Close();
                 Debug.Log("Closed connection");
             }*/
        }

        private void SubscribeAndAdvertise()
        {
            waypoint_marking_id = RosSocket.Advertise<Messages.Geometry.Point>("waypoint_marking");
            operator_callback_id = RosSocket.Advertise<std_msgs.String>("operator_callback");
            operator_command_id = RosSocket.Advertise<std_msgs.String>("operator_commands");
            //endEffector_id = RosSocket.Advertise<Messages.Geometry.Pose>("end_effector");
            endEffectorPoseStamp_id = RosSocket.Advertise<Messages.Geometry.PoseStamped>("taskSpaceGoal");
            //endEffectorTwistStamp_id = RosSocket.Advertise<TwistStamp>("taskSpaceVelocity");
            item_marking_string_id = RosSocket.Advertise<std_msgs.String>("item_marking");
            operator_sequence_id = RosSocket.Advertise<std_msgs.String>("operator_sequence");

            //subscription_id = RosSocket.Subscribe<std_msgs.String>("/subscription_test", SubscriptionHandler);
            jointState_id = RosSocket.Subscribe<Messages.Sensor.JointState>("/joint_states", SubscriptionArmState);
            //pointCloud_id = RosSocket.Subscribe<Messages.Sensor.PointCloud2>("/camera/depth/color/points", SubscriptionPointCloudColor);
            //pointCloud_id = RosSocket.Subscribe<Messages.Sensor.PointCloud2>("/camera/depth_registered/points", SubscriptionPointCloudColor);
            pointCloud_id = RosSocket.Subscribe<Messages.Sensor.PointCloud2>("/saint/vision/resampled_cloud", SubscriptionPointCloud);
            image_id = RosSocket.Subscribe<Messages.Sensor.CompressedImage>("/camera/color/image_raw/compressed", SubscriptionImage);
            image_id_longshot = RosSocket.Subscribe<Messages.Sensor.CompressedImage>("/environment/color/image_raw/compressed", SubscriptionImageLongshot);
            operator_messages_id = RosSocket.Subscribe<std_msgs.String>("operator_messages", SubscriptionOperatorMessages);
            operator_telemetry_id = RosSocket.Subscribe<std_msgs.String>("operator_telemetry", SubscriptionTelemetry);
            task_space_pose_id = RosSocket.Subscribe<Messages.Geometry.PoseStamped>("taskSpacePose", SubscriptionPose);


            // Informationsubscriber
            workspace_id = RosSocket.Subscribe<std_msgs.String>("/workspace_message", SubscriptionWorkspace);
            sequence_id = RosSocket.Subscribe<std_msgs.String>("/sequence_message", SubscriptionSequence);
            current_actions_id = RosSocket.Subscribe<std_msgs.String>("/current_actions", SubscriptionCurrentActions);
            actions_id = RosSocket.Subscribe<std_msgs.String>("/available_actions_message", SubscriptionActions);

            // Trajectory
            trajectory_id = RosSocket.Subscribe<Messages.PoseArray>("/path_waypoints", SubscriptionTrajectory);
        }

        // Trajectory
        private void SubscriptionTrajectory(Messages.PoseArray message)
        {
            numberOfPoses = message.poses.Length;
			positionArray = new Vector3[numberOfPoses];
			
			for(int i=0; i < numberOfPoses; i++)
			{
				positionArray[i] = GetPosition(message, i).Ros2Unity();
			}

            isMessageReceived = true;
        }

        private void ProcessMessage()
        {
			RosTrajectory.positionCount = numberOfPoses;
	
			if(numberOfPoses != 0)
			{
				for(int j=0; j < numberOfPoses; j++)
				{
					RosTrajectory.SetPosition(j, positionArray[j]);
				}
			}
        }

        private Vector3 GetPosition(Messages.PoseArray message, int poseCounter)
        {
            return new Vector3(
                message.poses[poseCounter].position.x,
                message.poses[poseCounter].position.y,
                message.poses[poseCounter].position.z);
        }

        private static Vector3 GetPositionPoseStamped(Messages.Geometry.PoseStamped message)
        {
            return new Vector3(
                message.pose.position.x,
                message.pose.position.y,
                message.pose.position.z);
        }

        private static Quaternion GetOrientationPoseStamped(Messages.Geometry.PoseStamped message)
        {
            return new Quaternion(
                message.pose.orientation.x,
                message.pose.orientation.y,
                message.pose.orientation.z,
                message.pose.orientation.w);
        }

        private static void SubscriptionPose(Messages.Geometry.PoseStamped message)
        {
            // Debug.Log(message);                    //print ROS' message to Unity 
            task_space_pose_position = GetPositionPoseStamped(message).Ros2Unity();
            task_space_pose_orientation = GetOrientationPoseStamped(message).Ros2Unity();
        }

        private static void SubscriptionHandler(std_msgs.String message)
        {
            Debug.Log(message.data);                    //print ROS' message to unity
        }

        private static void SubscriptionTelemetry(std_msgs.String message)
        {
            telemetry_message = (string)message.data;
        }

        private static void SubscriptionOperatorMessages(std_msgs.String message)
        {
            operator_message = (string)message.data;
        }

        private static void SubscriptionWorkspace(std_msgs.String message)
        {
            workspace_message = (string)message.data;
        }

        private static void SubscriptionSequence(std_msgs.String message)
        {
            sequence_message = (string)message.data;
        }

        private static void SubscriptionCurrentActions(std_msgs.String message)
        {
            current_actions_message = (string)message.data;
        }

        private static void SubscriptionActions(std_msgs.String message)
        {
            actions_message = (string)message.data;
        }

        private void SubscriptionArmState(Messages.Sensor.JointState message)
        {
            for (int i = 0; i < message.position.Length; i++)
                this.armState.JointPosition[i] = (float)message.position[i];


            //Debug.Log(message.position[0]);                    //print ROS's message to unity
        }

        private void SubscriptionPointCloudColor(Messages.Sensor.PointCloud2 message)
        {
            Vector3[] pcl = new Vector3[message.width*message.height];
            Color32[] color = new Color32[message.width * message.height];
            //for (int i = 0; i < message.position.Length; i++)
            //    this.armState.JointPosition[i] = (float)message.position[i];

            try

            {
               
                int index = 0;
                for (uint j = 0; j < message.height; j++)
                {
                    for (uint i = 0; i < message.row_step; i += message.point_step)
                    {
                        float x = BitConverter.ToSingle(message.data, (int)(i + message.fields[0].offset + j * message.row_step));
                        float z = BitConverter.ToSingle(message.data, (int)(i + message.fields[1].offset + j * message.row_step));
                        float y = BitConverter.ToSingle(message.data, (int)(i + message.fields[2].offset + j * message.row_step));
                        byte r = message.data[(int)(i + message.fields[3].offset + j * message.row_step + 0)];
                        byte g = message.data[(int)(i + message.fields[3].offset + j * message.row_step + 1)];
                        byte b = message.data[(int)(i + message.fields[3].offset + j * message.row_step + 2)];
                        byte a = 255; // No alpha is transmitted //message.data[(int)(i + message.fields[3].offset + j * message.row_step + 3)];

                        pcl[index] = new Vector3(x, y, z);
                        //byte[] color_byte = BitConverter.GetBytes(rgb);
                        color[index++] = new Color32(b,g,r, a); 
                        //Debug.Log(pcl[index-1]);

                    }
                }
            }
            catch(Exception e)
            {
                Debug.Log(e.Message);
            }
            //    this.armState.JointPosition[i] = (float)message.position[i];


            //Debug.Log(pcl[1]);                    //print ROS's message to unity
            //Debug.Log(message.data.Length);                    //print ROS's message to unity
            //print(color[0].ToString());

            pointCloudViewer.SetPoints(pcl, color);
        }

        private void SubscriptionPointCloudColorCoroutine(Messages.Sensor.PointCloud2 message)
        {
            Debug.Log(message.data.Length);
            try
            {
                if (coroutine_ready)
                {
                    coroutine = loadPclData(message);
                    pclReceived = true;
                    coroutine_ready = false;
                    
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }

        private IEnumerator loadPclData(Messages.Sensor.PointCloud2 message)
        {
            Vector3[] pcl = new Vector3[message.width * message.height];
            Color32[] color = new Color32[message.width * message.height];
            //for (int i = 0; i < message.position.Length; i++)
            //    this.armState.JointPosition[i] = (float)message.position[i];

            int index = 0;
            for (uint j = 0; j < message.height - 1; j++)
            {
                for (uint i = 0; i < message.row_step; i += message.point_step)
                {
                    float x = BitConverter.ToSingle(message.data, (int)(i + message.fields[0].offset + j * message.row_step));
                    float z = BitConverter.ToSingle(message.data, (int)(i + message.fields[1].offset + j * message.row_step));
                    float y = BitConverter.ToSingle(message.data, (int)(i + message.fields[2].offset + j * message.row_step));
                    byte r = message.data[(int)(i + message.fields[3].offset + j * message.row_step + 0)];
                    byte g = message.data[(int)(i + message.fields[3].offset + j * message.row_step + 1)];
                    byte b = message.data[(int)(i + message.fields[3].offset + j * message.row_step + 2)];
                    byte a = 255;

                    pcl[index] = new Vector3(x, y, z);
                    //byte[] color_byte = BitConverter.GetBytes(rgb);
                    color[index++] = new Color32(r, g, b, a);
                    //Debug.Log(pcl[index-1]);

                }
                if(j%30 == 0)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            pointCloudViewer.SetPoints(pcl, color);
            coroutine_ready = true;
        }

        private void SubscriptionPointCloud(Messages.Sensor.PointCloud2 message)
        {
            Vector3[] pcl = new Vector3[message.width * message.height];
            try
            {

                int index = 0;
                for (uint j = 0; j < message.height; j++)
                {
                    for (uint i = 0; i < message.row_step; i += message.point_step)
                    {
                        float x = BitConverter.ToSingle(message.data, (int)(i + message.fields[0].offset + j * message.row_step));
                        float z = BitConverter.ToSingle(message.data, (int)(i + message.fields[1].offset + j * message.row_step));
                        float y = BitConverter.ToSingle(message.data, (int)(i + message.fields[2].offset + j * message.row_step));
                    
                        pcl[index++] = new Vector3(x, y, z);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            pointCloudViewer.SetPoints(pcl);
        }

        private void SubscriptionImage(Messages.Sensor.CompressedImage message)
        {
            //Texture2D texture = new Texture2D(1920, 1080);
            //texture.LoadImage(message.data);
            //texture.Apply();
            //    this.armState.JointPosition[i] = (float)message.position[i];


            //Debug.Log(pcl[1]);                    //print ROS's message to unity
            // Debug.Log(message.data.Length);                    //print ROS's message to unity
            imageViewer.SetImage(message.data);
        }

        private void SubscriptionImageLongshot(Messages.Sensor.CompressedImage message)
        {
            imageViewerLongshot.SetImage(message.data);
            // Debug.Log(message.data.Length);                    //print ROS's message to unity
        }

#region Ros Client Methods
        private ManualResetEvent isConnected = new ManualResetEvent(false);

        public void Awake()
        {
            new Thread(ConnectAndWait).Start();
        }

        private void ConnectAndWait()
        {
            Thread.Sleep(100);
            RosSocket = ConnectToRos(Protocol, RosBridgeServerUrl, OnConnected, OnClosed, Serializer);

            if (!isConnected.WaitOne(Timeout * 1000))
                Debug.LogWarning("Failed to connect to RosBridge at: " + RosBridgeServerUrl);
            else
                SubscribeAndAdvertise();
        }

        public static RosSocket ConnectToRos(Protocols protocolType, string serverUrl, EventHandler onConnected = null, EventHandler onClosed = null, RosSocket.SerializerEnum serializer = RosSocket.SerializerEnum.JSON)
        {
            RosBridgeClient.Protocols.IProtocol protocol = GetProtocol(protocolType, serverUrl);
            protocol.OnConnected += onConnected;
            protocol.OnClosed += onClosed;

            return new RosSocket(protocol, serializer);
        }

        private static RosBridgeClient.Protocols.IProtocol GetProtocol(Protocols protocol, string rosBridgeServerUrl)
        {
            switch (protocol)
            {
                case Protocols.WebSocketSharp:
                    return new RosBridgeClient.Protocols.WebSocketSharpProtocol(rosBridgeServerUrl);
                case Protocols.WebSocketNET:
                    return new RosBridgeClient.Protocols.WebSocketNetProtocol(rosBridgeServerUrl);
                default:
                    return null;
            }
        }

        private void OnApplicationQuit()
        {
            robotControl.saintState.IsConnected = false;
            RosSocket.Close();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            isConnected.Set();
            Debug.Log("Connected to RosBridge: " + RosBridgeServerUrl);
            robotControl.saintState.IsConnected = true;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            isConnected.Reset();
            Debug.Log("Disconnected from RosBridge: " + RosBridgeServerUrl);
            robotControl.saintState.IsConnected = false;
        }
#endregion

        public void changeRoboterAddress(int roboterNumber)
        {
            string[] ipAddress = { RosBridgeServerUrl , RosBridgeServerUrlLab, RosBridgeServerUrlFac};
            if (roboterNumber < 3)
            {
                // Close existing connection
                RosSocket.Close();
                robotControl.saintState.IsConnected = false;
                // Open new connection
                RosBridgeServerUrl = ipAddress[roboterNumber];
                new Thread(ConnectAndWait).Start();
            }
        }
    }

    public class TwistStamp : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "geometry_msgs/TwistStamped";
        public std_msgs.Header header;
        public Messages.Geometry.Twist twist;

        //public TwistStamp();
    }

    public class SAINTPoseArraySubscriber2 : Subscriber<Messages.PoseArray>
    {
        public LineRenderer RosTrajectory;

        public Vector3[] positionArray;

        private bool isMessageReceived;
		public int numberOfPoses;
		public int test;
		public Messages.PoseArray message;


		protected /*override*/ void Start()
        {
			base.Start();			
		}
		

        private void Update()
        {
			//numberOfPoses = RosSocket.Subscribe<Messages.PoseArray>("/path_waypoints", subscriptionHandler);
			if (isMessageReceived)
                ProcessMessage();
        }


        protected override void ReceiveMessage(Messages.PoseArray message)
        {
			
			numberOfPoses = message.poses.Length;
			positionArray = new Vector3[numberOfPoses];
			
			for(int i=0; i < numberOfPoses; i++)
			{
				positionArray[i] = GetPosition(message, i).Ros2Unity();
			}

			isMessageReceived = true;        
		}


		private void ProcessMessage()
        {
			RosTrajectory.positionCount = numberOfPoses;
	
			if(numberOfPoses != 0)
			{
				for(int j=0; j < numberOfPoses; j++)
				{
					RosTrajectory.SetPosition(j, positionArray[j]);
				}
			}
        }


        private Vector3 GetPosition(Messages.PoseArray message, int poseCounter)
        {
            return new Vector3(
                message.poses[poseCounter].position.x,
                message.poses[poseCounter].position.y,
                message.poses[poseCounter].position.z);
        }

    }
}
#endif