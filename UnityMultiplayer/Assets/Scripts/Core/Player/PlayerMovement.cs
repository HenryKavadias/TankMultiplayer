using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private ParticleSystem dustCloud;
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turningRate = 30f;
    [SerializeField] private float particleEmmisionValue = 10;

    private ParticleSystem.EmissionModule emissionModule;

    private Vector2 previousMoveInput = Vector2.zero;
    private Vector3 previousPos;

    private const float PARTICLESTOPTHRESHOLD = 0.0001f;

    private void Awake()
    {
        emissionModule = dustCloud.emission;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
        inputReader.moveEvent += HandleMove;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }
        inputReader.moveEvent -= HandleMove;
    }

    private void HandleMove(Vector2 moveInput)
    {
        previousMoveInput = moveInput;
    }

    private void Update()
    {
        if ((transform.position - previousPos).sqrMagnitude > PARTICLESTOPTHRESHOLD)
        {
            emissionModule.rateOverTime = particleEmmisionValue;
        }
        else
        {
            emissionModule.rateOverTime = 0;
        }
        previousPos = transform.position;

        if (!IsOwner) { return; }

        // turn 
        float zRotation = previousMoveInput.x * -turningRate * Time.deltaTime;
        bodyTransform.Rotate(0f, 0f, zRotation);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }

        rb.velocity = (Vector2)bodyTransform.up * previousMoveInput.y * moveSpeed;
    }
}
