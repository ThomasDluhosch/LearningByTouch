using Unity.Netcode;
using UnityEngine;

public struct PaintStroke : INetworkSerializable
{
    public Vector2 uv;
    public float radius;
    public Color color;
    public bool hard;
    public bool isErase;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref uv);
        s.SerializeValue(ref radius);
        s.SerializeValue(ref color);
        s.SerializeValue(ref hard);
        s.SerializeValue(ref isErase);
    }
}