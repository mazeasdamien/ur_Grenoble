using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using static ScenarioManager;
using TMPro;
using Unity.VisualScripting;
using System.Linq;

namespace DDS_protocol
{
    public class CameraCopy : MonoBehaviour
    {
        public RawImage imageCam;
        public RawImage savedImage;
        public Recording Recording;
        private byte[] snapshotdata;
        public ScenarioManager scenarioManager;
        private string fileName;
        private float distanceInfo;
        public MoveRobot_PUB moveRobot_PUB;

        public ToggleGroup partsDefault;
        private string getDefault;


        private void Update()
        {
            distanceInfo = moveRobot_PUB.distance;
        }
        public void saveImage()
        {
            switch (scenarioManager.parts)
            {
                case scenario1_parts.p1:
                    fileName = "p1_" + distanceInfo +"_" + getDefault + ".jpg";
                    break;
                case scenario1_parts.p2:
                    fileName = "p2_" + distanceInfo + "_" + getDefault + ".jpg";
                    break;
                case scenario1_parts.p3:
                    fileName = "p3_" + distanceInfo + "_" + getDefault + ".jpg";
                    break;
                case scenario1_parts.p4:
                    fileName = "p4_" + distanceInfo+ "_" + getDefault + ".jpg";
                    break;
                default:
                    fileName = "invalidPhoto.jpg";
                    break;
            }


            string filePath = System.IO.Path.Combine(Recording.folderPath, fileName); // combine the file name with the specified path

            Texture2D snapshot = (Texture2D)imageCam.texture;
            snapshot.Apply();
            snapshotdata = snapshot.EncodeToJPG();

            File.WriteAllBytes(filePath, snapshotdata);
            // Load the image from the saved file path
            byte[] imageData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(imageData);

            // Set the texture to the RawImage component
            savedImage.texture = texture;
            texture.Apply();
        }
    }
}