using DDS_protocol;
using Rti.Dds.Publication;
using Rti.Types.Dynamic;
using UnityEngine;

public class MoveRobot_PUB : MonoBehaviour
{
    public Transform robot;
    public float distance;

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
               .Create();

            Writer = DDSHandler.SetupDataWriter("robotTrackTopic", robotTrackTopic);
            sample = new DynamicData(robotTrackTopic);
        }

        sample.SetValue("xValue", robot.localPosition.x);
        sample.SetValue("distance", distance);
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
