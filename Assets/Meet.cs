using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;

public class MeetingDialog : MonoBehaviourPun
{
    public float meetingDuration = 120f;
    public Text timerText;
    public Transform playerListContainer;
    public GameObject playerVotePrefab;

    private float remainingTime;
    private bool meetingActive = false;
    private int aliveAtMeetingStart;

    private Dictionary<int, int> votes = new Dictionary<int, int>();
    private HashSet<int> playersVoted = new HashSet<int>();

    void Start()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!meetingActive) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime > 0)
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(remainingTime) + "s";
        }
        else
        {
            EndMeeting();
        }
    }

    // ✅ Called by any player to start a meeting
    public void StartMeeting()
    {
        photonView.RPC("RPC_OpenMeeting", RpcTarget.AllBuffered);
        photonView.RPC("RPC_RemoveAllDeadBodies", RpcTarget.All);
    }

    [PunRPC]
    void RPC_RemoveAllDeadBodies()
    {
        GameObject[] bodies = GameObject.FindGameObjectsWithTag("DeadBody");
        foreach (GameObject body in bodies)
        {
            PhotonView pv = body.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) PhotonNetwork.Destroy(body);
            else if (pv == null) Destroy(body);
        }
    }

    [PunRPC]
    void RPC_OpenMeeting()
    {
        gameObject.SetActive(true);
        remainingTime = meetingDuration;
        meetingActive = true;
        votes.Clear();
        playersVoted.Clear();

        aliveAtMeetingStart = GetAlivePlayerCount();
        PopulatePlayerList();

        Debug.Log("📢 Meeting started. Alive players: " + aliveAtMeetingStart);
    }

    void PopulatePlayerList()
    {
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            GameObject entry = Instantiate(playerVotePrefab, playerListContainer);
            Text nameText = entry.transform.Find("PlayerName").GetComponent<Text>();
            if (nameText != null)
            {
                // ✅ Always use PlayerColor from Custom Properties instead of nickname
                string colorName = p.CustomProperties.ContainsKey("PlayerColor")
                    ? (string)p.CustomProperties["PlayerColor"]
                    : "Unassigned";

                nameText.text = colorName + (IsPlayerDead(p) ? " ☠️" : "");
            }


            Button voteBtn = entry.transform.Find("VoteButton").GetComponent<Button>();
            if (voteBtn != null)
            {
                int targetID = p.ActorNumber;
                voteBtn.interactable = !IsPlayerDead(p);
                voteBtn.onClick.AddListener(() => OnVote(targetID));
            }
        }
    }

    // ✅ Send votes to all clients instead of local-only
    void OnVote(int targetID)
    {
        int voterID = PhotonNetwork.LocalPlayer.ActorNumber;
        Photon.Realtime.Player voter = GetPhotonPlayer(voterID);
        if (voter == null || IsPlayerDead(voter)) return;
        if (playersVoted.Contains(voterID)) return;

        photonView.RPC("RPC_RegisterVote", RpcTarget.All, voterID, targetID);
    }

    [PunRPC]
    void RPC_RegisterVote(int voterID, int targetID)
    {
        if (playersVoted.Contains(voterID)) return;

        playersVoted.Add(voterID);
        if (!votes.ContainsKey(targetID)) votes[targetID] = 0;
        votes[targetID]++;

        Debug.Log($"🗳️ Vote received → Voter {voterID} → Target {targetID}");

        if (voterID == PhotonNetwork.LocalPlayer.ActorNumber) DisableVotingUI();

        if (playersVoted.Count >= aliveAtMeetingStart && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("✅ All votes received → Master ending meeting.");
            photonView.RPC("RPC_EndMeetingAndDecide", RpcTarget.All, SerializeVotes());
        }
    }

    void DisableVotingUI()
    {
        foreach (Transform entry in playerListContainer)
        {
            Button btn = entry.Find("VoteButton").GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }

    int GetAlivePlayerCount()
    {
        int count = 0;
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
            if (!IsPlayerDead(p)) count++;
        return count;
    }

    // ✅ Master sends final decision
    [PunRPC]
    void RPC_EndMeetingAndDecide(string votesData)
    {
        votes = DeserializeVotes(votesData);
        int eliminatedID = GetVotedOutPlayer();

        meetingActive = false;
        gameObject.SetActive(false);

        if (eliminatedID != -1)
        {
            photonView.RPC("RPC_EliminatePlayer", RpcTarget.All, eliminatedID, false);
            Debug.Log($"☠️ Player {eliminatedID} eliminated by vote.");
        }
        else
        {
            Debug.Log("🤝 Tie or no votes → No elimination.");
        }
    }

    // ✅ Voting calculation
    int GetVotedOutPlayer()
    {
        int maxVotes = 0;
        int candidateID = -1;
        bool tie = false;

        foreach (var kvp in votes)
        {
            if (kvp.Value > maxVotes)
            {
                maxVotes = kvp.Value;
                candidateID = kvp.Key;
                tie = false;
            }
            else if (kvp.Value == maxVotes && kvp.Value > 0)
            {
                tie = true;
            }
        }
        return tie ? -1 : candidateID;
    }

    // ✅ Elimination
    [PunRPC]
    void RPC_EliminatePlayer(int actorID, bool killedAsGhost)
    {
        GameObject target = FindPlayerObject(actorID);
        PhotonView victimView = target != null ? target.GetComponent<PhotonView>() : null;
        bool isLocalVictim = victimView != null && victimView.IsMine;

        if (target != null)
        {
            foreach (Collider c in target.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (Collider2D c2 in target.GetComponentsInChildren<Collider2D>()) c2.enabled = false;

            foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
            {
                if (isLocalVictim)
                {
                    r.enabled = true;
                    if (r.material.HasProperty("_Color"))
                    {
                        Color c = r.material.color;
                        c.a = 0.3f;
                        r.material.color = c;
                    }
                }
                else r.enabled = false;
            }
        }

        Photon.Realtime.Player p = GetPhotonPlayer(actorID);
        if (p != null)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["IsDead"] = true;
            p.SetCustomProperties(props);
        }
    }

    // ✅ Helpers
    GameObject FindPlayerObject(int actorID)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = obj.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == actorID)
                return obj;
        }
        return null;
    }

    Photon.Realtime.Player GetPhotonPlayer(int actorID)
    {
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
            if (p.ActorNumber == actorID) return p;
        return null;
    }

    bool IsPlayerDead(Photon.Realtime.Player p)
    {
        return p.CustomProperties.ContainsKey("IsDead") && (bool)p.CustomProperties["IsDead"];
    }

    // ✅ Serialize votes for RPC
    string SerializeVotes()
    {
        List<string> parts = new List<string>();
        foreach (var kvp in votes) parts.Add(kvp.Key + ":" + kvp.Value);
        return string.Join(",", parts);
    }

    Dictionary<int, int> DeserializeVotes(string data)
    {
        Dictionary<int, int> result = new Dictionary<int, int>();
        if (string.IsNullOrEmpty(data)) return result;
        foreach (string part in data.Split(','))
        {
            string[] kv = part.Split(':');
            if (kv.Length == 2 && int.TryParse(kv[0], out int id) && int.TryParse(kv[1], out int count))
                result[id] = count;
        }
        return result;
    }

    void EndMeeting()
    {
        if (!meetingActive) return;
        meetingActive = false;

        int eliminatedID = GetVotedOutPlayer();

        if (eliminatedID != -1)
        {
            photonView.RPC("RPC_EliminatePlayer", RpcTarget.All, eliminatedID, false);
            Debug.Log($"☠️ Player {eliminatedID} eliminated by vote.");
        }
        else
        {
            Debug.Log("🤝 Voting tied. No one eliminated.");
        }

        gameObject.SetActive(false);
    }

}
