using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WolfProximityDetector : MonoBehaviourPun
{
    [Header("Detection Settings")]
    public float detectRange = 1f;
    public float killCooldown = 30f;
    public GameObject deadBodyPrefab;

    private GameObject killButton;
    private Image killButtonImage;
    private Text declarationText;
    private TMP_Text declarationTMP;

    private bool isWolf;
    private bool isOnCooldown;
    private bool isInitialCooldown;
    private float cooldownTimer;

    void Start()
    {
        // ✅ Cache references
        killButton = GameObject.Find("KillButton");
        if (killButton != null)
        {
            killButtonImage = killButton.GetComponent<Image>();
            if (killButton.TryGetComponent(out Button btn))
                btn.onClick.AddListener(OnKillPressed);
            killButton.SetActive(false);
        }

        GameObject declarationObj = GameObject.Find("Declaration");
        if (declarationObj != null)
        {
            declarationText = declarationObj.GetComponent<Text>();
            declarationTMP = declarationObj.GetComponent<TMP_Text>();
        }

        StartCoroutine(WaitForRoleAssignment());
    }

    private IEnumerator WaitForRoleAssignment()
    {
        float timer = 0f;
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role") && timer < 5f)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        isWolf = IsLocalPlayerWolf();

        if (photonView.IsMine)
        {
            string msg = isWolf
                ? "It's a secret! You are the Wolf. Steal and win!"
                : "You are a Bonk! Find the Wolf or lose!";
            if (declarationTMP) declarationTMP.text = msg;
            if (declarationText) declarationText.text = msg;
        }

        if (photonView.IsMine && isWolf)
            StartInitialCooldown();

        Debug.Log($"✅ Role assigned: {(isWolf ? "Wolf" : "Bonk")}");
    }

    // ✅ Starts cooldown at match start
    private void StartInitialCooldown()
    {
        isOnCooldown = true;
        isInitialCooldown = true;
        cooldownTimer = killCooldown;
        UpdateKillButtonState(true);
    }

    void Update()
    {
        if (!photonView.IsMine || !isWolf || killButton == null) return;

        // ✅ Handle cooldown timer
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                isOnCooldown = false;
                isInitialCooldown = false;
            }
        }

        // ✅ Show button: during initial cooldown OR when enemy is near
        bool showButton = isInitialCooldown || IsAnyCrewmateNearby();
        UpdateKillButtonState(showButton);
    }

    private void UpdateKillButtonState(bool visible)
    {
        if (killButton == null) return;

        killButton.SetActive(visible);
        if (killButtonImage)
        {
            Color c = killButtonImage.color;
            c.a = isOnCooldown ? 0.4f : 1f;
            killButtonImage.color = c;
        }

        if (killButton.TryGetComponent(out Button btn))
            btn.interactable = !isOnCooldown;
    }

    private bool IsLocalPlayerWolf()
    {
        return PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object role) &&
               role is string r && r == "Wolf";
    }

    // ✅ Kill logic
    private void OnKillPressed()
    {
        if (isOnCooldown) return;

        GameObject target = GetClosestCrewmate();
        if (target == null) return;

        PhotonView targetView = target.GetComponent<PhotonView>();
        if (targetView != null)
        {
            photonView.RPC(nameof(RPC_KillPlayer), RpcTarget.All, targetView.ViewID);
        }

        // ✅ Start cooldown
        isOnCooldown = true;
        cooldownTimer = killCooldown;
    }


    [PunRPC]
    void RPC_KillPlayer(int targetViewID)
    {
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView == null) return;

        GameObject victim = targetView.gameObject;
        bool isLocalVictim = targetView.IsMine;

        Debug.Log($"💀 Player Killed → {victim.name}");

        // ✅ Only MasterClient spawns the dead body, and informs all clients via RPC
        if (PhotonNetwork.IsMasterClient && deadBodyPrefab != null)
        {
            Vector3 spawnPos = victim.transform.position;
            photonView.RPC("RPC_SpawnDeadBody", RpcTarget.All, spawnPos);
        }

        // ✅ Ghost & disable visuals for the victim
        foreach (Renderer r in victim.GetComponentsInChildren<Renderer>())
        {
            if (isLocalVictim)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color c = r.material.color;
                    c.a = 0.3f;
                    r.material.color = c;
                }
            }
            else
            {
                r.enabled = false;
            }
        }

        // ✅ Disable collisions
        foreach (Collider c in victim.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (Collider2D c2 in victim.GetComponentsInChildren<Collider2D>()) c2.enabled = false;

        if (isLocalVictim)
        {
            Rigidbody rb = victim.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = false;
            }

            GameObject btn = GameObject.Find("KillButton");
            if (btn) btn.SetActive(false);
        }
    }


    [PunRPC]
    void RPC_SpawnDeadBody(Vector3 position)
    {
        if (deadBodyPrefab != null)
        {
            GameObject body = Instantiate(deadBodyPrefab, position, Quaternion.identity);
            body.tag = "DeadBody"; // ✅ So meeting cleanup works
            Debug.Log("🪦 Dead body spawned at " + position);
        }
    }


    // ✅ Helper: nearest crewmate
    private GameObject GetClosestCrewmate()
    {
        Vector3 wolfPos = transform.position;
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == this.gameObject) continue;
            float d = Vector3.Distance(wolfPos, p.transform.position);
            if (d < minDist && d <= detectRange)
            {
                minDist = d;
                closest = p;
            }
        }
        return closest;
    }

    private bool IsAnyCrewmateNearby()
    {
        Vector3 wolfPos = transform.position;
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == this.gameObject) continue;
            if (Vector3.Distance(wolfPos, p.transform.position) <= detectRange)
                return true;
        }
        return false;
    }
}
