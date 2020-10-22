using DefaultNamespace;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class PathFollowSystem:SystemBase
{
    private EntityCommandBufferSystem _ecbSystem;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var dt = Time.DeltaTime;
        Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation,
            ref PathIndex pathIndex) =>
        {
            if (pathIndex.Value >= 0)
            {
                int2 pathPosition = pathPositionBuffer[pathIndex.Value].Value;

                float3 targetPosition = new float3(pathPosition.x, translation.Value.y, pathPosition.y);
                float3 moveDirection = math.normalizesafe(targetPosition - translation.Value);
                float moveSpeed = 3f;

                translation.Value += moveDirection * moveSpeed * dt;
                if (math.distance(translation.Value, targetPosition) < .1f)
                {
                    //next waypoint
                    pathIndex.Value--;
                }
            }
            else
            {
                ecb.AddComponent(entityInQueryIndex,entity, new AwaitingOrder());
            }
        }).ScheduleParallel();
    }
}