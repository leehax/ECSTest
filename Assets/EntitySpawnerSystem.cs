using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EntitySpawnerSystem : SystemBase
{

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnEntities(500);
        }


    }

    private void SpawnEntities(int spawnCount)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            var prefabComponent = GetSingleton<PrefabEntityComponent>();
            var spawnedEntity = EntityManager.Instantiate(prefabComponent.Value);
            EntityManager.SetComponentData(spawnedEntity, new Translation {Value = new float3(0, 1, 0)});

            EntityManager.AddBuffer<PathPosition>(spawnedEntity);
            EntityManager.AddComponent<AgentTag>(spawnedEntity);
            EntityManager.AddComponent<AwaitingOrder>(spawnedEntity);
        }
    }
}