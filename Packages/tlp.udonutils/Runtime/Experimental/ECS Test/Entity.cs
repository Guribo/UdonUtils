using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Entity : UdonSharpBehaviour
{
    public Vector3 Position;
    public Vector3 Velocity;

    private void Start() {
        Position = Random.insideUnitSphere;
        Velocity = Random.insideUnitSphere;
    }

    private void Update() {
        Position += Velocity * Time.deltaTime;
    }
}