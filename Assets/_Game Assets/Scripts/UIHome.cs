/// ----------------------------------------------------------------------
/// <summary>
///     Handle UI for menuscreen, and lobby.
/// </summary>
/// <author>Narendra Arief Nugraha</author>
/// ----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public enum Home_Phase
{
    Menuscreen, Lobby, Room
}

public class UIHome : MonoBehaviourPunCallbacks
{
    [Header("Menuscreen")]
    [SerializeField] GameObject m_menuscreen;
    [SerializeField] TMP_InputField m_onlinePlayerNameInputField;

    [Header("Lobby")]
    [SerializeField] GameObject m_lobby;
    [SerializeField] TMP_InputField m_createRoomInput;
    [SerializeField] TMP_InputField m_joinRoomInput;

    [Header("Room")]
    [SerializeField] GameObject m_room;
    [SerializeField] TMP_Text m_roomNameText;
    [SerializeField] Button m_roomStartButton;
    [SerializeField] TMP_Text[] m_playersNameText;
    [SerializeField] Button[] m_playerReadyButton;
    Player[] m_player;

    public Home_Phase phase { get; private set; } = Home_Phase.Menuscreen;
    Dictionary<Home_Phase, GameObject> m_uis = new();

    #region PUN Callbacks
    public override void OnConnected()
    {
        _ChangePhase(Home_Phase.Lobby);
        _ResetPlayer();
        GameSystem.Instance.HideNotification();
    }

    public override void OnJoinedRoom()
    {
        _ChangePhase(Home_Phase.Room);
        _UpdateRoomUI();
        GameSystem.Instance.HideNotification();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        _UpdateRoomUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        _UpdateRoomUI(false);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        GameSystem.Instance.ShowNotification(cause.ToString());
        _ChangePhase(Home_Phase.Menuscreen);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        GameSystem.Instance.ShowNotification(returnCode + " - " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        GameSystem.Instance.ShowNotification(returnCode + " - " + message);
    }

    public override void OnLeftRoom()
    {
        _ResetPlayer();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _UpdateRoomUI();
    }
    #endregion

    #region Unity's Callback
    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        m_uis.Add(Home_Phase.Menuscreen, m_menuscreen);
        m_uis.Add(Home_Phase.Lobby, m_lobby);
        m_uis.Add(Home_Phase.Room, m_room);
    }
    #endregion

    #region Menuscreen
    /// <summary>
    /// Function for on click connect to server button
    /// </summary>
    public void ConnectToServer()
    {
        if (m_onlinePlayerNameInputField.text.Length == 0)
        {
            GameSystem.Instance.ShowNotification("Name cannot empty");
            return;
        }

        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.NickName = m_onlinePlayerNameInputField.text;
        GameSystem.Instance.ShowNotification("Loading ...", false);
    }

    /// <summary>
    /// Function for on click exit game button
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }
    #endregion

    #region Lobby
    /// <summary>
    /// Function for on click create room button
    /// </summary>
    public void CreateRoom()
    {
        if (m_createRoomInput.text.Length == 0)
        {
            GameSystem.Instance.ShowNotification("Room Name cannot empty");
            return;
        }

        RoomOptions room = new RoomOptions();
        room.MaxPlayers = GameSystem.MAX_PLAYER;

        PhotonNetwork.CreateRoom(m_createRoomInput.text, room);
        GameSystem.Instance.ShowNotification("Loading ...", false);
    }

    /// <summary>
    /// Function for on click join room button
    /// </summary>
    public void JoinRoom()
    {
        if (m_joinRoomInput.text.Length == 0)
        {
            GameSystem.Instance.ShowNotification("Room Name cannot empty");
            return;
        }

        PhotonNetwork.JoinRoom(m_joinRoomInput.text);
        GameSystem.Instance.ShowNotification("Loading ...", false);
    }

    /// <summary>
    /// Function for on click disconnect button
    /// </summary>
    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }
    #endregion

    #region Room
    /// <summary>
    /// Function for on click ready button. 
    /// Only interactable by owner
    /// </summary>
    public void SetPlayerReady()
    {
        Hashtable props = new() { { GameSystem.IS_PLAYER_READY_STR, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    /// <summary>
    /// Function for on click start game button after everyone is ready. 
    /// Only interactable by masterclient
    /// </summary>
    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    /// <summary>
    /// Function for on click leave room button
    /// </summary>
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        _ChangePhase(Home_Phase.Lobby);
    }

    private void _UpdateRoomUI(bool recachePlayerList = true)
    {
        if (recachePlayerList)
            m_player = PhotonNetwork.PlayerList;

        bool isAllPlayersReady = true;
        m_roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        for (int i = 0; i < GameSystem.MAX_PLAYER; i++)
        {
            if (i < m_player.Length)
            {
                m_playersNameText[i].text = m_player[i].NickName;
                m_playerReadyButton[i].gameObject.SetActive(true);

                bool isLocalPlayer = m_player[i].IsLocal;
                bool isReady = (bool)m_player[i].CustomProperties[GameSystem.IS_PLAYER_READY_STR];

                m_playerReadyButton[i].interactable = isLocalPlayer && !isReady;
                m_playerReadyButton[i].GetComponentInChildren<TMP_Text>().text =
                    (isLocalPlayer) ?
                        (isReady) ?
                            "Ready!" :
                            "Ready?" :
                        (isReady) ?
                            "Ready!" :
                            "Not ready yet";

                if (!isReady)
                    isAllPlayersReady = false;
            }
            else
            {
                m_playersNameText[i].text = "Waiting for other player...";
                m_playerReadyButton[i].gameObject.SetActive(false);
            }
        }

        m_roomStartButton.interactable = isAllPlayersReady && PhotonNetwork.IsMasterClient;
        m_roomStartButton.GetComponentInChildren<TMP_Text>().text =
            (PhotonNetwork.IsMasterClient) ?
                (isAllPlayersReady) ?
                    (m_player.Length == GameSystem.MAX_PLAYER) ?
                        "Start" :
                        "Start as is" :
                    "Not all players ready..." :
                (isAllPlayersReady) ?
                    "Waiting for host to start" :
                    "Not all players ready...";
    }

    private IEnumerator StartGameCoroutine()
    {
        Hashtable props = new Hashtable() { { GameSystem.EXPECTED_PLAYER_COUNT_STR, m_player.Length } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        yield return new WaitForEndOfFrame();

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel("Game-0");
    }
    #endregion

    private void _ChangePhase(Home_Phase newPhase)
    {
        phase = newPhase;
        _DisableAllUI();
        m_uis[newPhase].SetActive(true);
    }

    private void _DisableAllUI()
    {
        m_menuscreen.SetActive(false);
        m_lobby.SetActive(false);
        m_room.SetActive(false);
    }

    private void _ResetPlayer()
    {
        Hashtable props = new() { { GameSystem.IS_PLAYER_READY_STR, false }, { GameSystem.PLAYER_HEALTH_STR, GameSystem.PLAYER_MAXHEALTH } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
}
