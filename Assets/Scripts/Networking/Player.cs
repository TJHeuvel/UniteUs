using System.IO;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using UnityEngine;

class NetworkPlayer : IBitWritable
{
    public ulong ID { get; set; }
    public string Name { get; set; }

    public bool IsLocal => ID == MLAPI.NetworkingManager.Singleton.LocalClientId;


    public override string ToString() => $"{ID} : {Name} ({(IsLocal ? "(local)" : "")})";

    public void Read(Stream stream)
    {
        using (PooledBitReader reader = PooledBitReader.Get(stream))
        {
            //TODO: ID could very often be inferred by who sent it?
            ID = reader.ReadUInt64();
            Name = reader.ReadString().ToString();
        }
    }
    public void Write(Stream stream)
    {
        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
        {
            writer.WriteUInt64(ID);
            writer.WriteString(Name);
        }

    }
}

