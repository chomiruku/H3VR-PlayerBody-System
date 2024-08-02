using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// Helper script to help with mirroring position and rotation values
    /// </summary>
    [ExecuteInEditMode]
    public class MirrorRotationOnXAxis : MonoBehaviour
    {
        public Transform ToMirror;
        public Transform MirrorRoot;

        [ContextMenu("Mirror")]
        public void Mirror()
        {
            transform.rotation = ReflectRotation(ToMirror.rotation, MirrorRoot.right);
            transform.position = Vector3.Reflect(ToMirror.position - MirrorRoot.position, MirrorRoot.right) + MirrorRoot.position;
        }

        private Quaternion ReflectRotation(Quaternion source, Vector3 normal)
        {
            return Quaternion.LookRotation(Vector3.Reflect(source * Vector3.forward, normal), Vector3.Reflect(source * Vector3.up, normal));
        }
    }
}
