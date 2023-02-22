using DDS_protocol;
using K4os.Compression.LZ4;
using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;


public class SIMU_DirectTeleop_SUB : MonoBehaviour
{
    private protected DataReader<DynamicData> Reader { get; private set; }
    private bool init = false;

    public Transform target;
    public float speed;
    public float speedR;
    public Transform robot;

    private void Update()
    {
        if (!init)
        {
            init = true;

            var typeFactory = DynamicTypeFactory.Instance;
            var Teleop = typeFactory.BuildStruct()
                .WithName("DirectTeleop")
                .AddMember(new StructMember("Rleft", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Lleft", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Left", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Right", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Forward", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("Backward", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("zoomin", typeFactory.GetPrimitiveType<bool>()))
                .AddMember(new StructMember("zoomout", typeFactory.GetPrimitiveType<bool>()))
                .Create();

            Reader = DDSHandler.SetupDataReader("DirectTeleopTopic", Teleop);
        }

        ProcessData(Reader);
    }

    void ProcessData(AnyDataReader anyReader)
    {
        var reader = (DataReader<DynamicData>)anyReader;
        using var samples = reader.Take();

        foreach (var sample in samples)
        {
            if (sample.Info.ValidData)
            {
                DynamicData data = sample.Data;

                if (data.GetValue<bool>("Rleft"))
                {
                    if (robot.localPosition.x <= -3)
                    {
                        robot.localPosition += new Vector3(speedR * Time.deltaTime, 0, 0);
                    }
                }
                if (data.GetValue<bool>("Lleft"))
                {
                    if (robot.localPosition.x >= -18)
                    {
                        robot.localPosition += new Vector3(-speedR * Time.deltaTime, 0, 0);
                    }
                }
                if (data.GetValue<bool>("Left"))
                {
                    target.localPosition += new Vector3(speed * Time.deltaTime, 0, 0);
                }
                if (data.GetValue<bool>("Right"))
                {
                    target.localPosition += new Vector3(-speed * Time.deltaTime, 0, 0);
                }
                if (data.GetValue<bool>("Forward"))
                {
                    target.localPosition += new Vector3(0,speed * Time.deltaTime, 0);
                }
                if (data.GetValue<bool>("Backward"))
                {
                    target.localPosition += new Vector3(0,-speed * Time.deltaTime, 0);
                }
                if (data.GetValue<bool>("zoomin"))
                {
                    target.localPosition += new Vector3(0, 0,speed * Time.deltaTime);
                }
                if (data.GetValue<bool>("zoomout"))
                {
                    target.localPosition += new Vector3(0,0, -speed * Time.deltaTime);
                }
            }
        }
    }
}
