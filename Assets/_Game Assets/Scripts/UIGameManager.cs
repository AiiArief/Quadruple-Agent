/// ----------------------------------------------------------------------
/// <summary>
///     UI for game manager
/// </summary>
/// <author>Narendra Arief Nugraha</author>
/// ----------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIGameManager : MonoBehaviourPunCallbacks
{
    public static UIGameManager Instance { get; private set; }

    [SerializeField] GameObject m_readyPanel;
    [SerializeField] TMP_Text m_readyText;

    [SerializeField] GameObject m_pausePanel;

    [SerializeField] GameObject m_resultPanel;
    [SerializeField] TMP_Text m_winnerText;
    [SerializeField] Button m_restartButton;
    TMP_Text m_restartButtonText;

    #region PUN Callback
    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("Home");
    }

    [PunRPC]
    public void PlayAgainRPC(string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }
    #endregion

    #region Unity's Callback
    private void Awake()
    {
        Instance = this;

        m_restartButtonText = m_restartButton.GetComponentInChildren<TMP_Text>();
    }

    private void Update()
    {
        if(GameManager.Instance.phase != GamePhase.Result && Input.GetKeyDown(KeyCode.Escape))
            m_pausePanel.gameObject.SetActive(!m_pausePanel.gameObject.activeInHierarchy);
    }
    #endregion

    /// <summary>
    /// Called when on start of waiting player to load the scene
    /// </summary>
    public void InitializeReadyPanel()
    {
        m_readyPanel.SetActive(true);
        m_readyText.text = "Waiting for other players";
        m_resultPanel.SetActive(false);
    }

    /// <summary>
    /// Coroutine for counting to start the game
    /// </summary>
    /// <returns></returns>
    public IEnumerator ReadyPanelCountingCoroutine()
    {
        int i = 3;
        while (i > 0)
        {
            m_readyText.text = "Start in " + i + "...";
            i--;
            yield return new WaitForSeconds(1.0f);
        }

        m_readyText.text = "Go!";
        yield return new WaitForSeconds(1.0f);
        m_readyPanel.SetActive(false);
    }

    /// <summary>
    /// Called when on start of result panner
    /// </summary>
    /// <param name="winner">The winner of this round</param>
    public void InitializeResultPanel(Player winner)
    {
        m_resultPanel.SetActive(true);
        m_winnerText.text = winner.NickName + " is the winner!";
        StartCoroutine(HandleRestartButton());
    }

    /// <summary>
    /// Called on click of play again button
    /// </summary>
    public void PlayAgain()
    {
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(PlayAgainCoroutine());
    }

    /// <summary>
    /// Called on leave room butotn
    /// </summary>
    public void LeaveRoom()
    {
        PhotonNetwork.Disconnect();
    }

    private IEnumerator HandleRestartButton()
    {
        while(true)
        {
            if (GameManager.Instance.phase == GamePhase.Result)
            {
                m_restartButton.interactable = PhotonNetwork.IsMasterClient;
                m_restartButtonText.text = (PhotonNetwork.IsMasterClient) ? "Play Again" : "Waiting for host...";
            }

            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator PlayAgainCoroutine()
    {
        photonView.RPC("PlayAgainRPC", RpcTarget.Others, SceneManager.GetActiveScene().name);
        yield return new WaitForEndOfFrame();
        PhotonNetwork.IsMessageQueueRunning = false;
        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
    }
}
