/// ----------------------------------------------------------------------
/// <summary>
///     Handle game manager : Instantiate players and control game phase.
/// </summary>
/// <author>Narendra Arief Nugraha</author>
/// ----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum GamePhase
{
    Ready,
    Gameplay,
    Result
}

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [SerializeField] Transform m_players;
    public Transform players { get { return m_players; } }

    [SerializeField] Transform m_playerSpawners;
    [SerializeField] CinemachineVirtualCamera m_camera;

    public GamePhase phase { get; private set; } = GamePhase.Ready;

    PlayerController[] m_playerControllers;

    #region PUN Callback
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer == PhotonNetwork.LocalPlayer)
            return;

        m_playerControllers = new PlayerController[m_players.childCount];
        for (int i = 0; i < m_players.childCount; i++)
            m_playerControllers[i] = m_players.GetChild(i).GetComponent<PlayerController>();

        object expectedPlayerCountObj;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSystem.EXPECTED_PLAYER_COUNT_STR, out expectedPlayerCountObj))
        {
            int expectedPlayerCounts = (int)expectedPlayerCountObj;
            Hashtable props = new Hashtable() { { GameSystem.EXPECTED_PLAYER_COUNT_STR, expectedPlayerCounts - 1 } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
    #endregion

    #region Unity's Callback
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        StartCoroutine(_WaitingForAllPlayers());
    }
    #endregion

    /// <summary>
    /// Check all player is dead, get the winner, and control phase
    /// </summary>
    public void CheckForWinner()
    {
        PlayerController winner = null;
        foreach (PlayerController player in m_playerControllers)
        {
            if (!player.isDead)
            {
                if (winner != null)
                    return;

                winner = player;
            }
        }

        UIGameManager.Instance.InitializeResultPanel(winner.photonView.Owner);
        phase = GamePhase.Result;
    }

    private IEnumerator _WaitingForAllPlayers()
    {
        UIGameManager.Instance.InitializeReadyPanel();

        yield return new WaitForSeconds(1.0f);

        int id = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Transform spawner = m_playerSpawners.GetChild(id);
        var player = PhotonNetwork.Instantiate(GameSystem.Instance.playerControllerPrefabs[id].name, spawner.position, spawner.rotation);
        m_camera.Follow = player.transform;

        object expectedPlayerCountObj;
        int expectedPlayerCounts = 1;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSystem.EXPECTED_PLAYER_COUNT_STR, out expectedPlayerCountObj))
            expectedPlayerCounts = (int)expectedPlayerCountObj;

        while (m_players.childCount < expectedPlayerCounts)
            yield return new WaitForEndOfFrame();

        m_playerControllers = new PlayerController[m_players.childCount];
        for (int i = 0; i < m_players.childCount; i++)
            m_playerControllers[i] = m_players.GetChild(i).GetComponent<PlayerController>();

        StartCoroutine(UIGameManager.Instance.ReadyPanelCountingCoroutine());
        yield return new WaitForSeconds(3.0f);
        phase = GamePhase.Gameplay;
    }

}
