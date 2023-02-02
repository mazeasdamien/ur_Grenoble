using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public class XRinterract : XRGrabInteractable
    {
        private IXRInteractable interactable;
        public GameObject parent;
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            var obj = args.interactableObject;
            obj.transform.parent = parent.transform;
            if (args.interactorObject is XRDirectInteractor || args.interactorObject is XRRayInteractor)
            {
                Transform attachController = args.interactorObject.GetAttachTransform(interactable);
                SetAttachTransform(attachController);

                XRBaseController xRController = args.interactorObject.transform.GetComponent<XRBaseController>();
                HapticFeedback(xRController, 0.5f, 0.25f);
            }

        }


        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            if (args.interactorObject is XRDirectInteractor || args.interactorObject is XRRayInteractor)
            {
                XRBaseController xRController = args.interactorObject.transform.GetComponent<XRBaseController>();
                HapticFeedback(xRController, 0.5f, 0.15f);
            }
        }

        public void SetAttachTransform(Transform attachController)
        {
            bool hasAttach = attachTransform != null;
            attachController.SetPositionAndRotation(hasAttach ? attachTransform.position : transform.position,
                hasAttach ? attachTransform.rotation : transform.rotation);
        }

        public void HapticFeedback(XRBaseController xRController, float amplitude, float duration)
        {
            xRController.SendHapticImpulse(amplitude, duration);

        }
    }
}
