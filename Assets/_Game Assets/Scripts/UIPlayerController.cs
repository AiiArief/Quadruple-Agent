/// ----------------------------------------------------------------------
/// <summary>
///     UI for player controller in world space above player
/// </summary>
/// <author>Narendra Arief Nugraha</author>
/// ----------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerController : MonoBehaviour
{
    [SerializeField] PlayerController m_playerController;
    [SerializeField] TMP_Text m_name;
    [SerializeField] Slider m_healthBar;
    [SerializeField] Slider m_cooldownBar;

    [Tooltip("Color for local player's health bar")]
    [SerializeField] Color m_ownerColor;

    [Tooltip("Color for other player's health bar")]
    [SerializeField] Color m_enemyColor;

    Quaternion m_originalCamRotation;

    private void Start()
    {
        m_originalCamRotation = transform.rotation;

        m_name.text = m_playerController.photonView.Owner.NickName;
        m_healthBar.fillRect.GetComponent<Image>().color = (m_playerController.photonView.IsMine) ? m_ownerColor : m_enemyColor;
    }

    private void Update()
    {
        m_name.gameObject.SetActive(!m_playerController.isDead);
        m_healthBar.gameObject.SetActive(!m_playerController.isDead);
        m_cooldownBar.gameObject.SetActive(!m_playerController.isDead && m_playerController.photonView.IsMine);

        if (m_playerController.isDead)
            return;

        transform.rotation = Camera.main.transform.rotation * m_originalCamRotation;
        m_healthBar.value = m_playerController.currentHealth * 1.0f / GameSystem.PLAYER_MAXHEALTH;
        m_cooldownBar.value = m_playerController.currentShot / m_playerController.maxShot;
    }
}
