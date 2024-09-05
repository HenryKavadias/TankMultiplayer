using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private CoinWallet wallet;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private Collider2D playerCollider;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed;
    [SerializeField] private float fireRate;
    [SerializeField] private float muzzleFlashDuration;
    [SerializeField] private int costToFire = 2;

    private bool firing;
    private float fireCooldown;
    private float muzzleFlashCooldown;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
        inputReader.primaryFireEvent += HandlePrimaryFire;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }
        inputReader.primaryFireEvent -= HandlePrimaryFire;
    }

    private void HandlePrimaryFire(bool _firing)
    {
        firing = _firing;
    }

    void Update()
    {
        if (muzzleFlashCooldown > 0f)
        {
            muzzleFlashCooldown -= Time.deltaTime;
            if (muzzleFlashCooldown <= 0f)
            {
                muzzleFlash.SetActive(false);
            }
        }
        
        if (!IsOwner || !firing) { return; }

        if (fireCooldown > 0) 
        {
            fireCooldown -= Time.deltaTime;
            return; 
        }

        if (wallet.totalCoins.Value < costToFire) { return; }

        SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up);

        PrimaryFireServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.up);

        fireCooldown = 1 / fireRate;
    }

    [ServerRpc]
    private void PrimaryFireServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        if (wallet.totalCoins.Value < costToFire) { return; }

        wallet.SpendCoins(costToFire);

        GameObject projectileInstance =
            Instantiate(serverProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<DamageOnContact>(out DamageOnContact dealDamage)) 
        {
            dealDamage.SetOwner(OwnerClientId);
        }

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }

        SpawnDummyProjectileClientRpc(spawnPos, direction);
    }

    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction)
    {
        if (IsOwner) { return; }
        SpawnDummyProjectile(spawnPos, direction);
    }

    private void SpawnDummyProjectile(Vector3 spawnPos, Vector3 direction)
    {
        muzzleFlash.SetActive(true);
        muzzleFlashCooldown = muzzleFlashDuration;

        GameObject projectileInstance = 
            Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }
    }
}
