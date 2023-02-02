using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class UR_AnimationGripper : MonoBehaviour
{
    public Animation anim;
    private bool gripperopen = true;

    void Update()
    {
        if (anim.isPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            PlayAnimationGripper();
        }

    }

    public void PlayAnimationGripper()
    {
        if (gripperopen)
        {
            anim["gripper_open"].speed = 3f;
            anim.Play("gripper_open");
            gripperopen = false;
        }
        else
        {
            anim["gripper_open"].speed = -3f;
            anim["gripper_open"].time = anim["gripper_open"].length;
            anim.Play("gripper_open");
            gripperopen = true;
        }
    }
}
