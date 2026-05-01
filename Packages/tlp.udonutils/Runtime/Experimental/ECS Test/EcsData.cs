using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[DefaultExecutionOrder(0)]
public class EcsData : UdonSharpBehaviour
{
    public int EntityCount;

    public Vector3[] Positions;
    public Vector3[] Velocities;

    private void Start() {
        Positions = new Vector3[EntityCount];
        Velocities = new Vector3[EntityCount];

        for (int i = 0; i < EntityCount; i++) {
            Positions[i] = Random.insideUnitSphere;
            Velocities[i] = Random.insideUnitSphere;
        }
    }
}