using K4os.Compression.LZ4;
using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DDS_protocol
{
    public class UR_videoSimu : MonoBehaviour
    {
        private protected DataReader<DynamicData> Reader { get; private set; }
        private bool init = false;
        public RawImage rawTexture;
        private Texture2D texture2D;

        void Start()
        {
            texture2D = new Texture2D(1, 1);
        }

        private void Update()
        {
            if (!init)
            {
                init = true;

                var typeFactory = DynamicTypeFactory.Instance;

                var VideoFeed = typeFactory.BuildStruct()
                    .WithName("VideoFeedSimu")
                    .AddMember(new StructMember("Memory", typeFactory.CreateSequence(typeFactory.GetPrimitiveType<byte>(), 1500000)))
                    .Create();

                Reader = DDSHandler.SetupDataReader("VideoFeedSimu", VideoFeed);
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
                    texture2D.LoadImage(data.GetValue<byte[]>("Memory"));
                    texture2D.Apply();
                    rawTexture.texture = texture2D;
                }
            }
        }
    }
}