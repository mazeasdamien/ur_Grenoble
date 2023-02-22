using DDS_protocol;
using Rti.Dds.Publication;
using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using K4os.Compression.LZ4;

public class UR_CameraDepth : MonoBehaviour
{
    private bool init = false;
    private protected DataReader<DynamicData> reader { get; private set; }

    public int indexDepth;
    [HideInInspector]
    public float[] DepthData;

    void Update()
    {
        if (!init)
        {
            init = true;

            var typeFactory = DynamicTypeFactory.Instance;

            var CameraDepthTopic = typeFactory.BuildStruct()
                .WithName("CameraDepthTopic")
                .AddMember(new StructMember("Index", typeFactory.GetPrimitiveType<int>()))
                .AddMember(new StructMember("Depth", typeFactory.CreateSequence(typeFactory.GetPrimitiveType<float>(), 2000000)))
                .Create();

            reader = DDSHandler.SetupDataReader("CameraDepthTopic", CameraDepthTopic);

        }
        ProcessData(reader);
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

                indexDepth = data.GetValue<int>("Index");
                DepthData = data.GetValue<float[]>("Depth");
                GetComponent<UR_Pointcloud>().enabled = true;
            }
        }
    }
}
