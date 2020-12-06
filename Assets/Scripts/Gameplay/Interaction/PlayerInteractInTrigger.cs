using System;
using UnityEngine;

enum InteractionType
{
    Use,
    Kill,
    Report
}
class PlayerInteractInTrigger : MonoBehaviour
{
    public static Action<PlayerInteractInTrigger> OnTriggerEnter, OnTriggerExit;
    public InteractionType Interaction;


    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || other.gameObject != PlayerSpawnManager.Instance.LocalPlayer.gameObject) return;

        OnTriggerEnter?.Invoke(this);
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (!enabled || other.gameObject != PlayerSpawnManager.Instance.LocalPlayer.gameObject) return;

        OnTriggerExit?.Invoke(this);
    }
}