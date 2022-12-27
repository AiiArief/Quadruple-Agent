/// ----------------------------------------------------------------------
/// <summary>
///     Handle databases and show system notification.
/// </summary>
/// <author>Narendra Arief Nugraha</author>
/// ----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSystem : MonoBehaviour
{
    public static GameSystem Instance { get; private set; }

    public const int MAX_PLAYER = 4;
    public const string IS_PLAYER_READY_STR = "isPlayerReady";
    public const string EXPECTED_PLAYER_COUNT_STR = "expectedPlayerCount";
    public const string PLAYER_HEALTH_STR = "playerHealth";
    public const int PLAYER_MAXHEALTH = 5;

    [Header("Database")]
    [Tooltip("Prefab for player controller. Must put in Resources folder.")]
    [SerializeField] PlayerController[] m_playerControllerPrefabs;
    public PlayerController[] playerControllerPrefabs { get { return m_playerControllerPrefabs; } }

    [Tooltip("Prefab for bullet controller. Instantiated without Photon's instantiate.")]
    [SerializeField] BulletController m_bulletPrefab;
    public BulletController bulletPrefab { get { return m_bulletPrefab; } }

    [Header("Notification")]
    [SerializeField] GameObject m_notificationPanel;
    [SerializeField] TMP_Text m_notificationText;
    [SerializeField] Button m_closeButton;
    Action m_customActionOnClose;

    #region Unity's Callback
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    /// <summary>
    /// Show simple noification
    /// </summary>
    /// <param name="textStr">Notification text</param>
    /// <param name="clickToHide">If true then you can click the notification to hide it. False if you want something else (like loading, etc) </param>
    /// <param name="customActionOnClose">Invoke this action on close this notification</param>
    public void ShowNotification(string textStr, bool clickToHide = true, Action customActionOnClose = null)
    {
        m_notificationPanel.SetActive(true);
        m_notificationText.text = textStr;
        m_closeButton.gameObject.SetActive(clickToHide);
        m_customActionOnClose = customActionOnClose;
    }

    /// <summary>
    /// Hide the notification manually by script and also invoke last action on close
    /// </summary>
    public void HideNotification()
    {
        m_notificationPanel.SetActive(false);
        m_customActionOnClose?.Invoke();
    }
}
