#if COMPILE_ROSSharp
/*
This message class is generated automatically with 'SimpleMessageGenerator' of ROS#
*/ 

using Newtonsoft.Json;
using RosSharp.RosBridgeClient.Messages.Geometry;
using RosSharp.RosBridgeClient.Messages.Navigation;
using RosSharp.RosBridgeClient.Messages.Sensor;
using RosSharp.RosBridgeClient.Messages.Standard;
using RosSharp.RosBridgeClient.Messages.Actionlib;

namespace RosSharp.RosBridgeClient.Messages
{
public class PoseArray : Message
{
[JsonIgnore]
public const string RosMessageName = "geometry_msgs/PoseArray";

public Header header;
public Pose[] poses;

public PoseArray()
{
header = new Header();
poses = new Pose[0];
}
}
}
#endif
