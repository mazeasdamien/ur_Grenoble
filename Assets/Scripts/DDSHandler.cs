using Rti.Dds;
using Rti.Dds.Publication;
using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using UnityEngine;

namespace DDS_protocol
{
    // 2026: same interface as the 2023 RTI Connext handler, now backed by LocalBus
    // (TCP on 127.0.0.1, one port per topic) because the RTI licence has expired.
    public class DDSHandler : MonoBehaviour
    {
        public static DataReader<DynamicData> SetupDataReader(string topicName, DynamicType dynamicData)
        {
            Debug.Log($"LocalBus: reader '{topicName}' on port {LocalBus.PortFor(topicName)}");
            return new DataReader<DynamicData>(topicName, dynamicData);
        }

        public static DataWriter<DynamicData> SetupDataWriter(string topicName, DynamicType dynamicData)
        {
            Debug.Log($"LocalBus: writer '{topicName}' to port {LocalBus.PortFor(topicName)}");
            return new DataWriter<DynamicData>(topicName);
        }

        private void OnApplicationQuit()
        {
            LocalBus.Shutdown();
        }
    }
}
