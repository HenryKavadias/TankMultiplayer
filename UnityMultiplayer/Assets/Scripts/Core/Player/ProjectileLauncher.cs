using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TankPlayer player;
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

    private bool isPointerOverUI;
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
        if (firing) 
        { 
            if (isPointerOverUI) 
            { return; } 
        }
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

        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        if (fireCooldown > 0) 
        {
            fireCooldown -= Time.deltaTime;
            return; 
        }

        if (wallet.totalCoins.Value < costToFire) { return; }

        PrimaryFireServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.up);

        SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up, player.TeamIndex.Value);

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

        if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile))
        {
            projectile.Initialise(player.TeamIndex.Value);
        }

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }

        SpawnDummyProjectileClientRpc(spawnPos, direction, player.TeamIndex.Value);
    }

    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction, int teamIndex)
    {
        if (IsOwner) { return; }
        SpawnDummyProjectile(spawnPos, direction, teamIndex);
    }

    private void SpawnDummyProjectile(Vector3 spawnPos, Vector3 direction, int teamIndex)
    {
        muzzleFlash.SetActive(true);
        muzzleFlashCooldown = muzzleFlashDuration;

        GameObject projectileInstance = 
            Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile))
        {
            projectile.Initialise(teamIndex);
        }

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }
    }
}
