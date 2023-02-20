using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using System.Collections.Generic;
using UnityEngine;

namespace DDS_protocol
{
    public class UR_Joints_Subscriber : MonoBehaviour
    {
        public List<Transform> robot = new List<Transform>();
        public Transform TCP;
        public UR_Joints_Publisher publisher;
        private protected DataReader<DynamicData> Reader { get; private set; }
        private bool init = false;

        private void Update()
        {
            if (!init)
            {
                init = true;
                var typeFactory = DynamicTypeFactory.Instance;
                StructType RobotStateTopic = typeFactory.BuildStruct()
                    .WithName("RobotStateTopic")
                    .AddMember(new StructMember("J1", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("J2", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("J3", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("J4", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("J5", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("J6", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("X", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("Y", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("Z", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("RX", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("RY", typeFactory.GetPrimitiveType<double>()))
                    .AddMember(new StructMember("RZ", typeFactory.GetPrimitiveType<double>()))
                    .Create();

                Reader = DDSHandler.SetupDataReader("RobotStateTopic", RobotStateTopic);
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
                    
                    robot[0].localEulerAngles = new Vector3(0, 0, -(float)data.GetValue<double>("J1") * Mathf.Rad2Deg);
                    robot[1].localEulerAngles = new Vector3(-90, 0, -(float)data.GetValue<double>("J2") * Mathf.Rad2Deg);
                    robot[2].localEulerAngles = new Vector3(0, 0, -(float)data.GetValue<double>("J3") * Mathf.Rad2Deg);
                    robot[3].localEulerAngles = new Vector3(0, 0, -(float)data.GetValue<double>("J4") * Mathf.Rad2Deg);
                    robot[4].localEulerAngles = new Vector3(-90, 0, -(float)data.GetValue<double>("J5") * Mathf.Rad2Deg);
                    robot[5].localEulerAngles = new Vector3(90, 0, -(float)data.GetValue<double>("J6") * Mathf.Rad2Deg);

                    /*
                    double[,] theta_angles = new double[1, 6];
                    theta_angles[0, 0] = data.GetValue<double>("J1");
                    theta_angles[0, 1] = data.GetValue<double>("J2");
                    theta_angles[0, 2] = data.GetValue<double>("J3");
                    theta_angles[0, 3] = data.GetValue<double>("J4");
                    theta_angles[0, 4] = data.GetValue<double>("J5");
                    theta_angles[0, 5] = data.GetValue<double>("J6");
                    Matrix4x4 raw_UR = UR_Inverse_Kinematics.Forward_kinematic_solution(theta_angles);

                    Matrix4x4 mt = Matrix4x4.identity;
                    mt.m11 = -1;
                    Matrix4x4 result = mt * raw_UR * mt.inverse;

                    if (!float.IsNaN(result.m03))
                    {
                        TCP.localPosition = new Vector3(result.m03, result.m13, result.m23);
                        Vector3 orientation_tcp = UR_Inverse_Kinematics.QuaternionFromMatrix(result).eulerAngles;
                        TCP.localEulerAngles = new Vector3(orientation_tcp.x, orientation_tcp.y, orientation_tcp.z);
                    }
                    */
                }
            }
        }
    }
}
