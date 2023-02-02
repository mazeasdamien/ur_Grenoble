using System;
using UnityEngine;
using TMPro;

public class inspection_cam : MonoBehaviour
{
    public float maxLength = 100f;

    public LineRenderer lineRenderer;
    public Material lineMaterial;

    public GameObject canvasPrefab;
    private GameObject canvasInstance;
    public Camera mainCamera;

    void Update()
    {
        Vector3 startPosition = gameObject.transform.position;
        Vector3 endPosition = startPosition + gameObject.transform.forward * maxLength;

        RaycastHit hit;
        if (Physics.Linecast(startPosition, endPosition, out hit))
        {
            if (hit.transform.gameObject.layer == 9)
            {
                lineMaterial.color = Color.green;

                if (canvasInstance == null)
                {
                    canvasInstance = Instantiate(canvasPrefab, hit.point, Quaternion.identity);
                }
                else
                {
                    canvasInstance.transform.position = hit.point;


                    canvasInstance.transform.LookAt(mainCamera.transform);

                    TMP_Text distanceText = canvasInstance.GetComponentInChildren<TMP_Text>();
                    float distance = Vector3.Distance(startPosition, hit.point)*100;
                    distanceText.text = Math.Round(distance,2) + " cm";
                }

            }
            else
            {
                lineMaterial.color = Color.red;

                if (canvasInstance != null)
                {
                    Destroy(canvasInstance);
                    canvasInstance = null;
                }
            }
            endPosition = hit.point;
        }
        else {

                lineMaterial.color = Color.red;

            if (canvasInstance != null)
            {
                Destroy(canvasInstance);
                canvasInstance = null;
            }
        }

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }
}
