
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[DefaultExecutionOrder(1)]
public class MovementSystem : UdonSharpBehaviour
{
    public EcsData Data;

    private void Update()
    {
        float dt = Time.deltaTime;

        int dataEntityCount = Data.EntityCount;
        for (int i = 0; i < dataEntityCount; i++)
        {
            Data.Positions[i] += Data.Velocities[i] * dt;
        }
    }
}