using UnityEngine;

enum PlayerRole
{
    Unknown, //Before the value is set. For civilians this is every other player, i only know of myself that i'm a civilian. For imposters this is only before we get information, otherwise we do know. 
    Civilian, 
    Imposter
}
class PlayerController : MonoBehaviour 
{
    [SerializeField] private Sprite[] playerTextures, ghostTextures;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private TMPro.TextMeshPro lblPlayerName;
    [SerializeField] private Animator animator;

    private int playerIndex;
    public bool IsAlive { get; private set; } = true;
    public NetworkPlayer Player { get; private set; }
    public PlayerRole Role { get; private set; }
    public void SetPlayer(NetworkPlayer player, int index)
    {
        this.Player = player;
        this.playerIndex = index;

        playerRenderer.sprite = playerTextures[playerIndex];
        lblPlayerName.text = player.Name;
    }
    public void SetRole(PlayerRole role)
    {
        this.Role = role;

        if (role == PlayerRole.Imposter)
            lblPlayerName.color = Color.red;
    }

    public void Die()
    {
        IsAlive = false;
        playerRenderer.sprite = ghostTextures[playerIndex];
        animator.SetBool("Dead", true);
    }
}