// using System.Collections;
// using System.Collections.Generic;
// using DefaultNamespace;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// public class EntitySpawnr : SystemBase
// {
//     private BeginPresentationEntityCommandBufferSystem _bufferSystem;
//     
//     protected override void OnCreate()
//     {
//         _bufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
//     }
//     
//     protected override void OnUpdate()
//     {
//         var commandBuffer = _bufferSystem.CreateCommandBuffer();
//
//         var random = new Random((uint) (Time.ElapsedTime * 10000 +1));
//         
//         Entities
//             .ForEach((Entity entity, int entityInQueryIndex, in SpawnComponent spawnComponent) =>
//         {
//             random.state += (uint) entityInQueryIndex;
//             for (int i = 0; i < spawnComponent.Count; i++)
//             {
//                 var e = commandBuffer.Instantiate(spawnComponent.Value);
//                 commandBuffer.AddComponent(e, new Translation {Value = math.normalizesafe(random.NextFloat3())});
//                 commandBuffer.AddComponent(e, new Target() {Value = math.normalizesafe(random.NextFloat3())});
//                 commandBuffer.AddComponent(e,new Direction());
//             }
//
//             commandBuffer.DestroyEntity(entity);
//         })
//         .Schedule();
//
//         _bufferSystem.AddJobHandleForProducer(Dependency);
//     }
//     
//     
// }
