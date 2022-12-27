/// ----------------------------------------------------------------------
/// <summary>
///     Handle Input from player
/// </summary>
/// <author>Narendra Arief Nugraha</author>
/// ----------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks
{
    CharacterController m_characterController;
    Animator m_animState;
    Light m_light;

    [Header("Movement")]
    [Tooltip("Player's move speed (walking)")] 
    public float speed = 3f;

    [Tooltip("Speed multiplier when sprinting")] 
    public float sprintMultiplier = 2.0f;
    
    public bool isDead { get; private set; } = false;
    public int currentHealth { get; private set; }

    [Header("Shoot")]
    [SerializeField] Transform m_bulletSpawn;

    [Tooltip("A click down contain how much bullet")]
    public int bulletBurstPerShot = 3;

    [Tooltip("Randomize angle of shooting")]
    public float shotRecoil = 1.5f;

    [Tooltip("Wait for another bullet when burst shot")]
    public float bulletFiringRate = 0.15f;

    [Tooltip("How many clicks before empty")]
    public int maxShot = 7;

    [Tooltip("How many second to fill one shot")]
    public float shotCooldownTime = 1.0f;

    public float currentShot { get; private set; }
    bool m_isFiring = false;

    #region PUN Callback
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != photonView.Owner)
            return;

        object healthObj;
        if (!changedProps.TryGetValue(GameSystem.PLAYER_HEALTH_STR, out healthObj))
            return;

        currentHealth = (int)healthObj;
        if (currentHealth <= 0)
        {
            _SetIsDead(true);
            GameManager.Instance.CheckForWinner();
        }
    }

    [PunRPC]
    private void _FireBulletRPC(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
    {
        float lag = (float)(PhotonNetwork.Time - info.SentServerTime);
        BulletController bullet = Instantiate(GameSystem.Instance.bulletPrefab, position, Quaternion.identity);
        bullet.InitializeBullet(rotation * Vector3.forward, lag);
    }

    [PunRPC]
    private void _HitByBulletRPC()
    {
        if (!photonView.IsMine)
            return;

        object healthObj;
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(GameSystem.PLAYER_HEALTH_STR, out healthObj))
            return;

        currentHealth = (int)healthObj;
        Hashtable props = new() { { GameSystem.PLAYER_HEALTH_STR, Mathf.Max(0, currentHealth - 1) } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    #endregion

    #region Unity's Callback
    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
        m_animState = GetComponentInChildren<Animator>();
        m_light = GetComponentInChildren<Light>();
    }

    private void Start()
    {
        _SetIsDead(false);

        transform.SetParent(GameManager.Instance.players);
        Hashtable props = new() { { GameSystem.PLAYER_HEALTH_STR, currentHealth } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void Update()
    {
        if (!photonView.IsMine || GameManager.Instance.phase != GamePhase.Gameplay || isDead)
            return;

        currentShot = Mathf.Min(maxShot, currentShot + Time.deltaTime / shotCooldownTime);
        bool fire = Input.GetButtonDown("Fire1");
        if (fire && !m_isFiring && currentShot >= 1.0f)
        {
            StartCoroutine(_IsFiringCoroutine());
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool sprint = !m_isFiring && Input.GetButton("Sprint");

        Vector3 dir = new Vector3(horizontal, 0f, vertical).normalized;
        float currentSprintMultiplier = (sprint) ? sprintMultiplier : 1.0f;

        if (dir.magnitude >= 0.1f)
        {
            float targetAngleMove = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            Vector3 movedir = Quaternion.Euler(0f, targetAngleMove, 0f) * Vector3.forward;
            m_characterController.Move(movedir.normalized * speed * currentSprintMultiplier * Time.deltaTime);
        }

        Vector2 playerPos = Camera.main.WorldToViewportPoint(transform.position);
        Vector2 mousePosOnScreen = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        Vector2 rotDir = mousePosOnScreen - playerPos;
        float targetAngleRot = Mathf.Atan2(rotDir.x, rotDir.y) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0.0f, targetAngleRot, 0.0f);

        m_animState.SetFloat("moveRange", Mathf.RoundToInt(dir.magnitude) * currentSprintMultiplier);
    }
    #endregion

    private void _SetIsDead(bool isDead)
    {
        this.isDead = isDead;
        m_characterController.enabled = !isDead;
        m_animState.SetBool("isDead", isDead);
        m_light.enabled = photonView.IsMine && !isDead;
        currentShot = (isDead) ? 0 : maxShot;
        currentHealth = (isDead) ? 0 : GameSystem.PLAYER_MAXHEALTH;
    }

    private IEnumerator _IsFiringCoroutine()
    {
        m_isFiring = true;
        currentShot = Mathf.Max(0, currentShot - 1);
        for (int i = 0; i < bulletBurstPerShot; i++)
        {
            Quaternion randRot = Quaternion.Euler(0.0f, m_bulletSpawn.rotation.eulerAngles.y + Random.Range(-shotRecoil, shotRecoil) * i, 0.0f);
            photonView.RPC("_FireBulletRPC", RpcTarget.AllViaServer, m_bulletSpawn.position, randRot);
            yield return new WaitForSeconds(bulletFiringRate);
        }
        m_isFiring = false;
    }
}
