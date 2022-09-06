using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
#if OSC_CORE
using OscCore;
#endif
using System;

namespace ChunkyOSC
{
    [RequireComponent(typeof(LookAtConstraint))]
    public class ChunkyOSC : MonoBehaviour
    {
#if OSC_CORE
        private OscClient Client;
#endif

        public int InputPort = 9000;
        [Tooltip("Currently unused, but it is here as a placeholder for future versions that require it.")]
        public int OutputPort = 9001;

        public string[] PositionParameters = new string[3] { "ChunkyPosX", "ChunkyPosY", "ChunkyPosZ" };
        public string[] RotationParameters = new string[4] { "ChunkyAimX", "ChunkyAimY", "ChunkyAimZ", "ChunkyRoll" };
        public string[] ChunkCoordParameters = new string[3] { "ChunkyCoordX", "ChunkyCoordY", "ChunkyCoordZ" };
        public string[] ChunkPolarParameters = new string[3] { "ChunkyPolarX", "ChunkyPolarY", "ChunkyPolarZ" };

        [SerializeField]
        private Transform Target;
        private LookAtConstraint Constraint;

        public static int MAX_CHUNKS = 255;
        [HideInInspector, Range(1, 8)]
        public int ChunkSize = 2;

        [HideInInspector]
        public OscParameter[] ExtraParameters = new OscParameter[0];

        // Start is called before the first frame update
        void Start()
        {
#if OSC_CORE
            Client = new OscClient("127.0.0.1", InputPort);
            Constraint = GetComponent<LookAtConstraint>();

            Client.Send("/avatar/parameters/ChunkySize", true);
            Client.Send("/avatar/parameters/ChunkyOSC", true);
#endif
        }

        void OnApplicationQuit()
        {
#if OSC_CORE
            if (Client != null)
                Client.Send("/avatar/parameters/ChunkyOSC", false);
#endif
        }

        #region Tracking Variables
        private float[] lastPos = new float[3] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
        private float[] lastRot = new float[4] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
        private Vector3Int lastChunk = Vector3Int.FloorToInt(Vector3.positiveInfinity);
        private bool[] lastPolar = new bool[3] { false, false, false };
        private object[] lastExtra = new object[0];
        private int lastSize = 0;
#endregion

        // Update is called once per frame
        void Update()
        {
#if OSC_CORE
            if (Application.isPlaying && Client != null)
            {
                // Get the current object pose data.
                GetChunkedPose(out float[] position, out float[] rotation, out Vector3 chunk);

                // Floor the chunk value and account for the current polarity.
                Vector3Int finalChunk = Vector3Int.FloorToInt(chunk);
                bool[] chunkPolarity = new bool[] { chunk.x >= 0, chunk.y >= 0, chunk.z >= 0 };
                for (int i = 0; i < chunkPolarity.Length; i++)
                {
                    finalChunk[i] += chunkPolarity[i] ? 0 : 1;
                    finalChunk[i] = Math.Abs(finalChunk[i]);
                }

                // Debugging.
                //Debug.Log(string.Join(", ", position) + " | " + string.Join(", ", rotation) + " | " + finalChunk + " | " + string.Join(", ", chunkPolarity));

                // Transmit the data to VRChat over OSC.
                for (int i = 0; i < position.Length; i++) // Position
                {
                    if (position[i] != lastPos[i])
                    {
                        lastPos[i] = position[i];
                        Client.Send("/avatar/parameters/" + PositionParameters[i], position[i]);
                    }
                }

                for (int i = 0; i < rotation.Length; i++) // Rotation
                {
                    if (rotation[i] != lastRot[i])
                    {
                        lastRot[i] = rotation[i];
                        Client.Send("/avatar/parameters/" + RotationParameters[i], rotation[i]);
                    }
                }

                if (ChunkSize != lastSize) // Chunk Size
                {
                    lastSize = ChunkSize;
                    Client.Send("/avatar/parameters/ChunkySize", ChunkSize);
                }

                for (int i = 0; i < 3; i++) // Chunk Coordinates
                {
                    if (finalChunk[i] != lastChunk[i])
                    {
                        lastChunk[i] = finalChunk[i];
                        Client.Send("/avatar/parameters/" + ChunkCoordParameters[i], finalChunk[i]);
                    }
                }

                for (int i = 0; i < 3; i++) // Chunk Polarity
                {
                    if (chunkPolarity[i] != lastPolar[i])
                    {
                        lastPolar[i] = chunkPolarity[i];
                        Client.Send("/avatar/parameters/" + ChunkPolarParameters[i], chunkPolarity[i]);
                    }
                }

                if (lastExtra.Length != ExtraParameters.Length) // Extra Parameters
                    lastExtra = new object[ExtraParameters.Length];
                for (int i = 0; i < ExtraParameters.Length; i++)
                {
                    object extra = ExtraParameters[i].Value;
                    if (lastExtra[i] == null || extra != lastExtra[i])
                    {
                        switch (ExtraParameters[i].type)
                        {
                            case OscParameter.Types.Bool:
                                Client.Send("/avatar/parameters/" + ExtraParameters[i].name, (bool)extra);
                                break;
                            case OscParameter.Types.Float:
                                Client.Send("/avatar/parameters/" + ExtraParameters[i].name, (float)extra);
                                break;
                            case OscParameter.Types.Int:
                                Client.Send("/avatar/parameters/" + ExtraParameters[i].name, (int)extra);
                                break;
                        }
                        lastExtra[i] = extra;
                    }
                }
            }
#endif
        }

        private void GetChunkedPose(out float[] position, out float[] rotation, out Vector3 chunk)
        {
            // Chunk
            chunk = new Vector3
            {
                x = Mathf.Clamp(transform.position.x / ChunkSize, -256, 255),
                y = Mathf.Clamp(transform.position.y / ChunkSize, -256, 255),
                z = Mathf.Clamp(transform.position.z / ChunkSize, -256, 255)
            };

            // Position
            position = new float[3]
            {
            Mathf.Clamp((transform.position.x % ChunkSize * 2 / ChunkSize) - (chunk.x >= 0 ? 1 : -1), -1, 1),
            Mathf.Clamp((transform.position.y % ChunkSize * 2 / ChunkSize) - (chunk.y >= 0 ? 1 : -1), -1, 1),
            Mathf.Clamp((transform.position.z % ChunkSize * 2 / ChunkSize) - (chunk.z >= 0 ? 1 : -1), -1, 1)
            };

            // Rotation
            rotation = new float[4]
            {
            Mathf.Clamp(Target.localPosition.x, -1, 1),
            Mathf.Clamp(Target.localPosition.y, -1, 1),
            Mathf.Clamp(Target.localPosition.z, -1, 1),
            Mathf.Clamp(-Constraint.roll / 180, -1, 1)
            };
        }

        public Vector3 GetChunk()
        {
            return new Vector3
            {
                // Chunk
                x = Mathf.Clamp(transform.position.x / ChunkSize, -256, 255),
                y = Mathf.Clamp(transform.position.y / ChunkSize, -256, 255),
                z = Mathf.Clamp(transform.position.z / ChunkSize, -256, 255)
            };
        }
    }
}