using DDS_protocol;
using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using K4os.Compression.LZ4;
using UnityEngine.Rendering;

public class UR_VideoFeed : MonoBehaviour
{
    public RawImage rawTexture;
    private protected DataReader<DynamicData> Reader { get; private set; }
    private bool init = false;

    void Update()
    {
        if (!init)
        {
            init = true;
            var typeFactory = DynamicTypeFactory.Instance;

            var VideoFeed = typeFactory.BuildStruct()
                .WithName("VideoFeed")
                .AddMember(new StructMember("Index", typeFactory.GetPrimitiveType<int>()))
                .AddMember(new StructMember("Memory", typeFactory.CreateSequence(typeFactory.GetPrimitiveType<byte>(), 1500000)))
                .Create();


            Reader = DDSHandler.SetupDataReader("VideoFeed", VideoFeed);
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
                Texture2D texture2D = new Texture2D(640, 480, TextureFormat.RGB24, false);
                texture2D.LoadRawTextureData(LZ4Pickler.Unpickle(data.GetValue<byte[]>("Memory")));
                texture2D.Apply();
                rawTexture.texture = texture2D;
            }
        }
    }
}
