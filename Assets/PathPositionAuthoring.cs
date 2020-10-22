using Unity.Entities;
using UnityEngine;

public class PathPositionAuthoring:MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<PathPosition>(entity);
        dstManager.AddComponent<AgentTag>(entity);
        dstManager.AddComponent<AwaitingOrder>(entity);
    }
}