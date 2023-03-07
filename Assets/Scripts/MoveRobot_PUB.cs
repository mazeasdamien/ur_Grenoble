using DDS_protocol;
using Rti.Dds.Publication;
using Rti.Types.Dynamic;
using UnityEngine;

public class MoveRobot_PUB : MonoBehaviour
{
    public Transform robot;
    public float distance;
    public Transform IK;
    public UR_Joints_Publisher uR_Joints_Publisher;
    public CollisionManager collisionManager;

    public XRPushButton right;
    public XRPushButton left;

    private protected DataWriter<DynamicData> Writer { get; private set; }
    private DynamicData sample = null;
    private bool init = false;

    void Update()
    {
        if (!init)
        {
            init = true;

            var typeFactory = DynamicTypeFactory.Instance;

            var robotTrackTopic = typeFactory.BuildStruct()
               .WithName("robotTrack")
               .AddMember(new StructMember("xValue", typeFactory.GetPrimitiveType<float>()))
               .AddMember(new StructMember("distance", typeFactory.GetPrimitiveType<float>()))
               .AddMember(new StructMember("ikx", typeFactory.GetPrimitiveType<float>()))
               .AddMember(new StructMember("iky", typeFactory.GetPrimitiveType<float>()))
               .AddMember(new StructMember("ikz", typeFactory.GetPrimitiveType<float>()))
               .AddMember(new StructMember("color", typeFactory.GetPrimitiveType<int>()))
               .AddMember(new StructMember("collisionNumbers", typeFactory.GetPrimitiveType<int>()))
               .Create();

            Writer = DDSHandler.SetupDataWriter("robotTrackTopic", robotTrackTopic);
            sample = new DynamicData(robotTrackTopic);
        }

        sample.SetValue("xValue", robot.localPosition.x);
        sample.SetValue("distance", distance);
        sample.SetValue("ikx", IK.localPosition.x);
        sample.SetValue("iky", IK.localPosition.y);
        sample.SetValue("ikz", IK.localPosition.z);
        sample.SetValue("color", uR_Joints_Publisher.robotstate);
        sample.SetValue("collisionNumbers", collisionManager.count);
        Writer.Write(sample);

        if (right.IsPushed)
        {
            if (robot.localPosition.x <= -3)
            {
                robot.localPosition += new Vector3(1*Time.deltaTime, 0, 0);
            }
        }
        if (left.IsPushed)
        {
            if (robot.localPosition.x >= -18)
            {
                robot.localPosition += new Vector3(-1*Time.deltaTime, 0, 0);
            }
        }
    }
}
