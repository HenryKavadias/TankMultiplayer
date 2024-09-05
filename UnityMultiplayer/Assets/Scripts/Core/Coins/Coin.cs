using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Coin : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    protected int coinValue = 10;
    protected bool isCollected;

    public abstract int Collect();

    public void SetValue(int val)
    {
        coinValue = val;
    }

    protected void Show(bool show)
    {
        spriteRenderer.enabled = show;
    }
}
