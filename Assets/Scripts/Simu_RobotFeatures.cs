using DDS_protocol;
using Rti.Dds.Publication;
using Rti.Types.Dynamic;
using UnityEngine;

public class Simu_RobotFeatures : MonoBehaviour
{
    public Transform robot;
    public float speed = 0.1f;
    public bool light = false;

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
               .AddMember(new StructMember("light", typeFactory.GetPrimitiveType<bool>()))
               .Create();

            Writer = DDSHandler.SetupDataWriter("robotTrackTopic", robotTrackTopic);
            sample = new DynamicData(robotTrackTopic);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (robot.localPosition.x > -18.8)
            {
                robot.Translate(Vector3.left * speed * Time.deltaTime);
            }
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            if (robot.localPosition.x < -2.4)
            {
                robot.Translate(Vector3.right * speed * Time.deltaTime);
            }
        }
        sample.SetValue("xValue", robot.localPosition.x);
        sample.SetValue("light", light);
        Writer.Write(sample);
    }
}
