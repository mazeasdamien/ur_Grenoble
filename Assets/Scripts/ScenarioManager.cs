using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ScenarioManager : MonoBehaviour
{
    public GameObject s1p1;
    public GameObject s1p2;
    public GameObject s1p3;
    public GameObject s1p4;

    public TMP_Text stepCounter;
    public TMP_Text buttonScenario;
    public TMP_Text distanceInfo;

    public List<Toggle> toggles;

    public bool isTrial;
    public GameObject trial;
    public GameObject engine;

    public GameObject takePhotoButton;

    public GameObject DefaultToReport;

    public enum scenario1_parts
    {
        start,
        p1,
        p2,
        p3,
        p4
    }

    public scenario1_parts parts;

    private void Update()
    {
        if (isTrial)
        {
            engine.SetActive(false);
            trial.SetActive(true); 
        }
        else 
        {
            engine.SetActive(true);
            trial.SetActive(false);
        }

        switch (parts)
        {
            case scenario1_parts.start:
                takePhotoButton.SetActive(false);
                DefaultToReport.SetActive(false);
                stepCounter.text = "";
                buttonScenario.text = "START";
                distanceInfo.text = "";
                s1p1.SetActive(false);
                s1p2.SetActive(false);
                s1p3.SetActive(false);
                s1p4.SetActive(false);
                break;
            case scenario1_parts.p1:
                DefaultToReport.SetActive(true);
                foreach (Toggle toggle in toggles)
                {
                    if (toggle.isOn == true)
                    {
                        takePhotoButton.SetActive(true);
                    }
                }
                stepCounter.text = "STEP 1 / 4";
                buttonScenario.text = "NEXT STEP [2]";
                distanceInfo.text = "15 cm";
                s1p1.SetActive(true);
                s1p2.SetActive(false);
                s1p3.SetActive(false);
                s1p4.SetActive(false);
                break;
            case scenario1_parts.p2:
                DefaultToReport.SetActive(true);
                foreach (Toggle toggle in toggles)
                {
                    if (toggle.isOn == true)
                    {
                        takePhotoButton.SetActive(true);
                    }
                }
                stepCounter.text = "STEP 2 / 4";
                buttonScenario.text = "NEXT STEP [3]";
                distanceInfo.text = "10 cm";
                s1p1.SetActive(false);
                s1p2.SetActive(true);
                s1p3.SetActive(false);
                s1p4.SetActive(false);
                break;
            case scenario1_parts.p3:
                DefaultToReport.SetActive(true);
                foreach (Toggle toggle in toggles)
                {
                    if (toggle.isOn == true)
                    {
                        takePhotoButton.SetActive(true);
                    }
                }
                stepCounter.text = "STEP 3 / 4";
                buttonScenario.text = "NEXT STEP [4]";
                distanceInfo.text = "14 cm";
                s1p1.SetActive(false);
                s1p2.SetActive(false);
                s1p3.SetActive(true);
                s1p4.SetActive(false);
                break;
            case scenario1_parts.p4:
                DefaultToReport.SetActive(true);
                foreach (Toggle toggle in toggles)
                {
                    if (toggle.isOn == true)
                    {
                        takePhotoButton.SetActive(true);
                    }
                }
                stepCounter.text = "STEP 4 / 4";
                buttonScenario.text = "SAVE AND EXIT";
                distanceInfo.text = "15 cm";
                s1p1.SetActive(false);
                s1p2.SetActive(false);
                s1p3.SetActive(false);
                s1p4.SetActive(true);
                break;
        }
    }

    public void UnselectAllToggles()
    {
        // Set the isOn property of each toggle to false
        foreach (Toggle toggle in toggles)
        {
            toggle.isOn = false;
        }
    }
}
