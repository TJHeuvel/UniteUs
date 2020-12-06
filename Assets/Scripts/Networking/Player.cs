using UnityEngine;

interface IPlayer
{
    ulong ID { get; }
    string Name { get; }
}

class NetworkPlayer : IPlayer
{
    public string Name { get; set; }
    public ulong ID { get; set; }

    public bool IsLocal => ID == MLAPI.NetworkingManager.Singleton.LocalClientId;

    public override string ToString() => $"{ID} : {Name} ({(IsLocal ? "(local)" : "")})";
}