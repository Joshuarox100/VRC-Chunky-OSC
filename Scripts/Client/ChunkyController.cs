using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace ChunkyOSC
{
    public class ChunkyController : MonoBehaviour
    {
        [SerializeField]
        private GameObject ChunkyObject;
        [SerializeField]
        private GameObject RotationTarget;
        private LookAtConstraint LookAt;

        [SerializeField]
        private Transform Environment;
        [SerializeField]
        private Material EnvMaterial;

        [Space]
        [Range(1, 8)]
        public int ChunkSize = 2;

        [Space]
        public string customName = "";
        [HideInInspector]
        public PresetUtility.Preset currentPreset = null;
        public OscParameter[] ExtraParameters = new OscParameter[0];

        [Space]
        [Tooltip("Show the camera boundaries at all times.")]
        public bool AlwaysShowBounds = false;

        private bool Valid = false;
        private ChunkyOSC chunky;

        // Start is called before the first frame update
        void Start()
        {
            if (ChunkyObject != null)
            {
                LookAt = ChunkyObject.GetComponent<LookAtConstraint>();
                chunky = ChunkyObject.GetComponent<ChunkyOSC>();
                if (chunky == null)
                {
                    Debug.LogError("Camera Controller incorrectly configured!");
                    return;
                }
            }
            else
            {
                Debug.LogError("Camera Controller incorrectly configured!");
                return;
            }

            if (LookAt == null || RotationTarget == null)
            {
                Debug.LogError("Camera Controller incorrectly configured!");
                return;
            }


            Valid = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (Valid)
            {
                // Clamp Position
                Vector3 clampedPosition = new Vector3(
                    Mathf.Clamp(transform.position.x, -chunky.ChunkSize * (ChunkyOSC.MAX_CHUNKS + 1) + 0.0001f,
                        chunky.ChunkSize * (ChunkyOSC.MAX_CHUNKS + 1) - 0.0001f),
                    Mathf.Clamp(transform.position.y, -chunky.ChunkSize * (ChunkyOSC.MAX_CHUNKS + 1) + 0.0001f,
                        chunky.ChunkSize * (ChunkyOSC.MAX_CHUNKS + 1) - 0.0001f),
                    Mathf.Clamp(transform.position.z, -chunky.ChunkSize * (ChunkyOSC.MAX_CHUNKS + 1) + 0.0001f,
                        chunky.ChunkSize * (ChunkyOSC.MAX_CHUNKS + 1) - 0.0001f));
                transform.position = clampedPosition;

                ChunkyObject.transform.position = clampedPosition;
                RotationTarget.transform.rotation = transform.rotation;

                // Wrap the roll value if necessary.
                if (transform.rotation.eulerAngles.z > 180)
                    LookAt.roll = transform.rotation.eulerAngles.z % 360 - 360;
                else if (transform.rotation.eulerAngles.z < -180)
                    LookAt.roll = transform.rotation.eulerAngles.z % 360 + 360;
                else
                    LookAt.roll = transform.rotation.eulerAngles.z;

                chunky.ChunkSize = ChunkSize;
                chunky.ExtraParameters = ExtraParameters;

                // Update the environment to reflect the chunk size.
                if (Environment != null)
                    Environment.localScale = ChunkSize * 256 * Vector3.one;
                if (EnvMaterial != null)
                    EnvMaterial.SetTextureScale("_MainTex", 256 * Vector2.one);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!AlwaysShowBounds)
                DrawGizmos();
        }

        private void OnDrawGizmos()
        {
            if (AlwaysShowBounds)
                DrawGizmos();
        }

        private void DrawGizmos()
        {
            if (Application.isPlaying && Valid)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(Vector3.zero, 2 * chunky.ChunkSize * ChunkyOSC.MAX_CHUNKS * Vector3.one);

                Vector3 chunk = chunky.GetChunk(); // Valid asserts cam != null.
                Vector3 finalChunk = Vector3Int.FloorToInt(chunk);
                finalChunk += .5f * Vector3.one;

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(finalChunk * chunky.ChunkSize, chunky.ChunkSize * Vector3.one);
            }
        }
    }
}