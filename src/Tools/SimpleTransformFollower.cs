using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// A simple script to keep a game object aligned with another one.
    /// I use this for the pose testers usually
    /// </summary>
    [ExecuteInEditMode]
    public class SimpleTransformFollower : MonoBehaviour
    {
        public Transform ToFollow;

        public void Update()
        {
            if (ToFollow != null)
            {
                transform.position = ToFollow.position;
                transform.rotation = ToFollow.rotation;
            }
        }
    }
}
