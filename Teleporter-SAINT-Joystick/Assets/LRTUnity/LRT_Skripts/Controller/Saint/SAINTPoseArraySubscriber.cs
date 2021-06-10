
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class SAINTPoseArraySubscriber : Subscriber<Messages.PoseArray>
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
