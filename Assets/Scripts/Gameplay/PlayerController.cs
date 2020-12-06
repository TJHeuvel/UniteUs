using UnityEngine;

enum PlayerRole
{
    Unknown,
    Civilian,
    Imposter
}
class PlayerController : MonoBehaviour
{
    public PlayerNetworkController NetworkController;

    [SerializeField] private Sprite[] playerTextures, ghostTextures;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private TMPro.TextMeshPro lblPlayerName;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerInteractInTrigger interactTrigger;

    public int PlayerIndex { get; private set; }
    public Sprite Sprite => playerRenderer.sprite;

    public bool IsAlive { get; private set; } = true;
    public NetworkPlayer NetworkPlayer { get; private set; }
    public PlayerRole Role => NetworkPlayer.Role;
    public void SetPlayer(NetworkPlayer player, int index)
    {
        this.NetworkPlayer = player;
        this.PlayerIndex = index;

        playerRenderer.sprite = playerTextures[PlayerIndex];
        lblPlayerName.text = player.Name;

        if (player.Role == PlayerRole.Imposter)
        {
            lblPlayerName.color = Color.red;
            interactTrigger.enabled = false;
        }
    }
    public void Die()
    {
        Debug.Log("DIE " + this, this);
        IsAlive = false;
        playerRenderer.sprite = ghostTextures[PlayerIndex];
        animator.SetBool("Dead", true);

        interactTrigger.enabled = true;
        interactTrigger.Interaction = InteractionType.Report;

        if (TryGetComponent<LocalPlayerMovementController>(out var movement))
            movement.enabled = false;
    }
}