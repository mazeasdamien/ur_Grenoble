using System.IO;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ScenarioManager;
using System.Collections.Generic;

namespace DDS_protocol
{
    public class Recording : MonoBehaviour
    {
        private string filePath;
        public CollisionManager collisionManager;
        public Transform robot;
        public List<Transform> robot_joints = new List<Transform>();
        public Transform IK;
        public string id;
        private float time = 0f;
        private bool isRunning;
        public string folderPath;
        public ScenarioManager SM;

        private void Update()
        {

            if (isRunning)
            {
                time += Time.deltaTime;
            }
        }

        public void StartStopExperiment()
        {
            if (SM.parts == scenario1_parts.start)
            {

                isRunning = true;

                folderPath = $"{Application.dataPath}/Participants_data/participant_{id}";
                Directory.CreateDirectory(folderPath);


                    filePath = $"{folderPath}/log_VR.txt";

                if (!File.Exists(filePath))
                {
                    using (File.Create(filePath)) { }
                }

                SM.parts = scenario1_parts.p1;

                // start of part
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine("part1");
                }

                // record position every 0.5 seconds
                InvokeRepeating("RecordPosition", 0.0f, 0.5f);
            }
            else if(SM.parts == scenario1_parts.p1)
            {
                // start of part
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine("part1 end collisions count " + collisionManager.count);
                    writer.WriteLine("part2");
                }
                SM.parts = scenario1_parts.p2;
            }
            else if (SM.parts == scenario1_parts.p2)
            {
                // start of part
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine("part2 end collisions count " + collisionManager.count);
                    writer.WriteLine("part3");
                }
                SM.parts = scenario1_parts.p3;
            }
            else if (SM.parts == scenario1_parts.p3)
            {
                // start of part
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine("part3 end collisions count " + collisionManager.count);
                    writer.WriteLine("part4");
                }
                SM.parts = scenario1_parts.p4;
            }
            else if (SM.parts == scenario1_parts.p4)
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine("part4 end collisions count " + collisionManager.count);
                }
                isRunning = false;
                CancelInvoke("RecordPosition");
                time = 0;
                Application.Quit();
            }
        }

        void RecordPosition()
        {
            // write position to file
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(
                    Math.Round(time, 2)
                    + ";" + Math.Round(robot.localPosition.x, 2)
                    + ";" + Math.Round(IK.localPosition.x, 2)
                    + ";" + Math.Round(IK.localPosition.y, 2)
                    + ";" + Math.Round(IK.localPosition.z, 2)
                    + ";" + Math.Round(-robot_joints[0].localEulerAngles.z, 2)
                    + ";" + Math.Round(-robot_joints[1].localEulerAngles.z * Mathf.Rad2Deg, 2)
                    + ";" + Math.Round(-robot_joints[2].localEulerAngles.z * Mathf.Rad2Deg, 2)
                    + ";" + Math.Round(-robot_joints[3].localEulerAngles.z * Mathf.Rad2Deg, 2)
                    + ";" + Math.Round(-robot_joints[4].localEulerAngles.z * Mathf.Rad2Deg, 2)
                    + ";" + Math.Round(-robot_joints[5].localEulerAngles.z * Mathf.Rad2Deg, 2));
            }
        }
    }
}