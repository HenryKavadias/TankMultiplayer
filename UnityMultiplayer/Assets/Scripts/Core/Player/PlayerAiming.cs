using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform turretTransform;

    private void LateUpdate()
    {
        if (!IsOwner) { return; }
        Vector2 aimScreenPos = inputReader.aimPosition;
        Vector2 aimWorldPos = Camera.main.ScreenToWorldPoint(aimScreenPos);

        turretTransform.up = new Vector2(
            aimWorldPos.x - turretTransform.position.x, 
            aimWorldPos.y - turretTransform.position.y);
    }
}
