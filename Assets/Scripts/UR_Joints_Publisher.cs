using Rti.Dds.Publication;
using Rti.Types.Dynamic;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace DDS_protocol
{
    public class UR_Joints_Publisher : MonoBehaviour
    {
        private protected DataWriter<DynamicData> Writer { get; private set; }
        public Material robotShadow;
        public Transform baseRobot;
        private DynamicData sample = null;
        private bool init = false;
        public Transform TCP_sender;
        public List<string> IK_Solutions = new List<string>();
        private UnityEngine.Vector3 tempPos = new();
        private UnityEngine.Vector3 tempRot = new();
        private List<double> goodSolution = new List<double>();
        public List<Transform> robot = new List<Transform>();
        public float limitHauteur = 0.15f;
        public float limitProfondeur = 0.23f;

        public int solutionID;

        void Update()
        {
            if (!init)
            {
                init = true;

                var typeFactory = DynamicTypeFactory.Instance;

                var UnitySolutionTopic = typeFactory.BuildStruct()
                   .WithName("UnitySolutionTopic")
                   .AddMember(new StructMember("J1", typeFactory.GetPrimitiveType<double>()))
                   .AddMember(new StructMember("J2", typeFactory.GetPrimitiveType<double>()))
                   .AddMember(new StructMember("J3", typeFactory.GetPrimitiveType<double>()))
                   .AddMember(new StructMember("J4", typeFactory.GetPrimitiveType<double>()))
                   .AddMember(new StructMember("J5", typeFactory.GetPrimitiveType<double>()))
                   .AddMember(new StructMember("J6", typeFactory.GetPrimitiveType<double>()))
                   .Create();

                Writer = DDSHandler.SetupDataWriter("UnitySolutionTopic", UnitySolutionTopic);
                sample = new DynamicData(UnitySolutionTopic);
            }

            UnityEngine.Matrix4x4 transform_matrix = UR_motion_toolkit.GetTransformMatrix(TCP_sender);

            UnityEngine.Matrix4x4 mt = UnityEngine.Matrix4x4.identity;
            mt.m11 = -1;
            UnityEngine.Matrix4x4 mt_inverse = mt.inverse;
            UnityEngine.Matrix4x4 result = mt * transform_matrix * mt_inverse;

            double[,] solutions = UR_motion_toolkit.Inverse_kinematic_solution(result);
            IK_Solutions.Clear();
            IK_Solutions = UR_motion_toolkit.DisplaySolutions(solutions);

            UR_motion_toolkit.SetSolution(IK_Solutions, solutions, solutionID, robot);
            goodSolution.Clear();
            goodSolution.Add(solutions[0, solutionID]);
            goodSolution.Add(solutions[1, solutionID]);
            goodSolution.Add(solutions[2, solutionID]);
            goodSolution.Add(solutions[3, solutionID]);
            goodSolution.Add(solutions[4, solutionID]);
            goodSolution.Add(solutions[5, solutionID]);

            if (TCP_sender.localPosition != tempPos || TCP_sender.localEulerAngles != tempRot)
            {
                if (UnityEngine.Vector3.Distance(baseRobot.localPosition, TCP_sender.localPosition) <= 0.8f
                    && TCP_sender.localPosition.z >= limitHauteur
                   && TCP_sender.localPosition.y >= 0.218)
                {
                    robotShadow.color = Color.white;
                    Color color = robotShadow.color;
                    color.a = 0.05f;
                    robotShadow.color = color;
                    sample.SetValue("J1", solutions[0, solutionID]);
                    sample.SetValue("J2", solutions[1, solutionID]);
                    sample.SetValue("J3", solutions[2, solutionID]);
                    sample.SetValue("J4", solutions[3, solutionID]);
                    sample.SetValue("J5", solutions[4, solutionID]);
                    sample.SetValue("J6", solutions[5, solutionID]);

                    Writer.Write(sample);
                    tempPos = TCP_sender.localPosition;
                    tempRot = TCP_sender.localEulerAngles;
                }
                else {

                    robotShadow.color = Color.red;
                    Color color = robotShadow.color;
                    color.a = 0.5f;
                    robotShadow.color = color;
                }
            }
        }
    }
}