using Rti.Dds.Core;
using Rti.Dds.Domain;
using Rti.Dds.Publication;
using Rti.Dds.Subscription;
using Rti.Dds.Topics;
using Rti.Types.Dynamic;
using UnityEngine;

namespace DDS_protocol
{
    public class DDSHandler : MonoBehaviour
    {
        private static QosProvider provider;
        private static DomainParticipant participant;
        private void Start()
        {
            provider = new QosProvider("URGrenoble.xml");
            participant = DomainParticipantFactory.Instance.CreateParticipant(0, provider.GetDomainParticipantQos());
        }

        public static DataReader<DynamicData> SetupDataReader(string topicName, DynamicType dynamicData)
        {
            if (participant != null)
            {
                DataReader<DynamicData> reader;
                Topic<DynamicData> topic = participant.CreateTopic(topicName, dynamicData);
                var subscriberQos = provider.GetSubscriberQos("QosURGrenoble::QosProfile");
                Subscriber subscriber = participant.CreateSubscriber(subscriberQos);
                var readerQos = provider.GetDataReaderQos("QosURGrenoble::QosProfile");
                reader = subscriber.CreateDataReader(topic, readerQos);
                return reader;
            }
            return null;
        }

        public static DataWriter<DynamicData> SetupDataWriter(string topicName, DynamicType dynamicData)
        {
            if (participant != null)
            {
                DataWriter<DynamicData> writer;
                Topic<DynamicData> topic = participant.CreateTopic(topicName, dynamicData);
                var publisherQos = provider.GetPublisherQos("QosURGrenoble::QosProfile");
                Publisher publisher = participant.CreatePublisher(publisherQos);
                var writerQos = provider.GetDataWriterQos("QosURGrenoble::QosProfile");
                writer = publisher.CreateDataWriter(topic, writerQos);
                return writer;
            }
            return null;
        }

        private void OnApplicationQuit()
        {
            participant.Dispose();
        }
    }
}