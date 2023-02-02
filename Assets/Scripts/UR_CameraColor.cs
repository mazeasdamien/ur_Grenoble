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

public class UR_CameraColor : MonoBehaviour
{

    private bool init = false;
    private protected DataReader<DynamicData> reader1 { get; private set; }

    public int indexColor;
    [HideInInspector]
    public byte[] Colordata;

    void Update()
    {
        if (!init)
        {
            init = true;

            var typeFactory1 = DynamicTypeFactory.Instance;

            var CameraColorTopic = typeFactory1.BuildStruct()
                .WithName("CameraColorTopic")
                .AddMember(new StructMember("Index", typeFactory1.GetPrimitiveType<int>()))
                .AddMember(new StructMember("Color", typeFactory1.CreateSequence(typeFactory1.GetPrimitiveType<byte>(), 1000000)))
                .Create();


            reader1 = DDSHandler.SetupDataReader("CameraColorTopic", CameraColorTopic);

        }
        ProcessData(reader1);
    }

    void ProcessData(AnyDataReader anyReader)
    {

        var reader1 = (DataReader<DynamicData>)anyReader;
        using var samples1 = reader1.Take();
        foreach (var sample in samples1)
        {
            if (sample.Info.ValidData)
            {
                DynamicData data = sample.Data;

                indexColor = data.GetValue<int>("Index");
                byte[] dd = data.GetValue<byte[]>("Color");
                Colordata = LZ4Pickler.Unpickle(dd);
            }
        }
    }
}
