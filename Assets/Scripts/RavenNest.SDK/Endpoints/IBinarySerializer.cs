using System;

namespace RavenNest.SDK.Endpoints
{
    public interface IBinarySerializer
    {
        object Deserialize(byte[] data, Type type);
        byte[] Serialize(object data);
    }
}