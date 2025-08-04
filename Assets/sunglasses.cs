using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerColorApplier : MonoBehaviourPunCallbacks
{
    public SpriteRenderer sunglassesRenderer;  // Assign sunglasses SpriteRenderer in Inspector

    private void Start()
    {
        ApplyColor(); // ✅ Apply on spawn
    }

    // ✅ Called when any player's custom properties update
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == photonView.Owner && changedProps.ContainsKey("PlayerColor"))
        {
            ApplyColor(); // ✅ Re-apply color if it changed
        }
    }

    // ✅ Apply color based on Photon property
    public void ApplyColor()
    {
        if (photonView.Owner != null && photonView.Owner.CustomProperties.ContainsKey("PlayerColor"))
        {
            string colorName = photonView.Owner.CustomProperties["PlayerColor"].ToString();
            Color newColor = GetColorByName(colorName);
            sunglassesRenderer.color = newColor;

            Debug.Log($"🕶️ Applied sunglasses color {colorName} to {photonView.Owner.NickName}");
        }
    }

    // ✅ String → Unity Color mapping
    private Color GetColorByName(string name)
    {
        switch (name)
        {
            case "Red": return Color.red;
            case "Blue": return Color.blue;
            case "Green": return Color.green;
            case "Yellow": return Color.yellow;
            case "Black": return Color.black;// custom purple
            default: return Color.white;
        }
    }
}
