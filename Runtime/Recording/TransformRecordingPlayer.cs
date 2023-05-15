using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Recording
{
    public class TransformRecordingPlayer : UdonSharpBehaviour
    {
        public TransformRecorder transformRecorder;

        public bool playOnEnable;
        internal float PlaybackStart;

        public void OnEnable()
        {
            if (!playOnEnable)
            {
                enabled = false;
                return;
            }

            PlaybackStart = Time.realtimeSinceStartup;
        }

        public void Update()
        {
            float time = Time.realtimeSinceStartup - PlaybackStart;
            transform.SetPositionAndRotation(
                transformRecorder.GetPosition(time),
                transformRecorder.GetRotation(time)
            );
        }
    }
}