// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;
// using Random = Unity.Mathematics.Random;
//
// namespace DefaultNamespace
// {
//     [UpdateInGroup(typeof(SimulationSystemGroup))]
//     public class MoveSystem : SystemBase
//     {
//         private EntityQuery _query;
//
//         protected override void OnCreate()
//         {
//             _query = GetEntityQuery(
//                 ComponentType.ReadWrite<Translation>(),
//                 ComponentType.ReadWrite<Direction>(),
//                 ComponentType.ReadWrite<Target>());
//             RequireForUpdate(_query);
//         }
//
//         protected override void OnUpdate()
//         {
//             var entities = _query.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jobhandle);
//             float3 mousepos = Input.mousePosition;
//             mousepos.z = 10f;
//             mousepos = Camera.main.ScreenToWorldPoint(mousepos);
//
//             Dependency = JobHandle.CombineDependencies(Dependency, jobhandle);
//
//             Dependency = new AvoidMouseJob()
//                 {
//                     DirectionType = GetComponentTypeHandle<Direction>(),
//                     TranslationType = GetComponentTypeHandle<Translation>(),
//                     MousePos = mousepos
//                 }
//                 .ScheduleParallel(_query, Dependency);
//             // Dependency = new SetDirectionJob()
//             //     {
//             //         DirectionType = GetComponentTypeHandle<Direction>(),
//             //         TranslationType = GetComponentTypeHandle<Translation>(),
//             //         TargetType = GetComponentTypeHandle<Target>()
//             //     }
//             //     .ScheduleParallel(_query, Dependency);
//             //
//             // Dependency = new UpdateTargetJob()
//             //     {
//             //         TranslationType = GetComponentTypeHandle<Translation>(),
//             //         TargetType = GetComponentTypeHandle<Target>(),
//             //         Time = Time.ElapsedTime,
//             //         MousePos = mousepos
//             //     }
//             //     .ScheduleParallel(_query, Dependency);
//             Dependency = new MoveJob
//                 {
//                     DirectionType = GetComponentTypeHandle<Direction>(true),
//                     TranslationType = GetComponentTypeHandle<Translation>(false),
//                     DeltaTime = Time.DeltaTime
//                 }
//                 .ScheduleParallel(_query, Dependency);
//             entities.Dispose(Dependency);
//         }
//
//         [BurstCompile]
//         private struct MoveJob : IJobChunk
//         {
//             [ReadOnly] public ComponentTypeHandle<Direction> DirectionType;
//             public ComponentTypeHandle<Translation> TranslationType;
//             public float DeltaTime;
//
//             public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//             {
//                 var directions = chunk.GetNativeArray(DirectionType);
//                 var translations = chunk.GetNativeArray(TranslationType);
//                 var speed = 1f * DeltaTime;
//
//                 for (int i = 0; i < chunk.Count; i++)
//                 {
//                     var translation = translations[i].Value + directions[i].Value * speed;
//                     translations[i] = new Translation {Value = translation};
//                 }
//             }
//         }
//
//         [BurstCompile]
//         private struct UpdateTargetJob : IJobChunk
//         {
//             public ComponentTypeHandle<Translation> TranslationType;
//             public ComponentTypeHandle<Target> TargetType;
//             public double Time;
//             public float3 MousePos;
//
//             public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//             {
//                 var translations = chunk.GetNativeArray(TranslationType);
//                 var targets = chunk.GetNativeArray(TargetType);
//                 var random = new Random((uint) (Time * 10000 + 1));
//                 for (int i = 0; i < chunk.Count; i++)
//                 {
//                     var translation = translations[i];
//                     var target = targets[i];
//
//                     if (math.abs(target.Value - MousePos).x < 10 ||
//                         math.abs(target.Value - MousePos).y < 10 ||
//                         math.abs(translation.Value - MousePos).x < 10 ||
//                         math.abs(translation.Value - MousePos).y < 10)
//                     {
//                         targets[i] = new Target {Value = math.normalizesafe(random.NextFloat3()) * 100f};
//                     }
//
//                     // || math.all(math.abs(target.Value - translation.Value)<1)
//                 }
//             }
//         }
//
//         [BurstCompile]
//         private struct SetDirectionJob : IJobChunk
//         {
//             public ComponentTypeHandle<Direction> DirectionType;
//             public ComponentTypeHandle<Translation> TranslationType;
//             public ComponentTypeHandle<Target> TargetType;
//
//             public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//             {
//                 var translations = chunk.GetNativeArray(TranslationType);
//                 var targets = chunk.GetNativeArray(TargetType);
//                 var directions = chunk.GetNativeArray(DirectionType);
//
//                 for (int i = 0; i < chunk.Count; i++)
//                 {
//                     var direction = directions[i];
//                     var translation = translations[i];
//                     var target = targets[i];
//                     math.normalizesafe(direction.Value = target.Value - translation.Value);
//                     directions[i] = new Direction {Value = direction.Value};
//
//                 }
//             }
//         }
//
//         [BurstCompile]
//         private struct AvoidMouseJob : IJobChunk
//         {
//             public ComponentTypeHandle<Direction> DirectionType;
//             public ComponentTypeHandle<Translation> TranslationType;
//             public float3 MousePos;
//
//             public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//             {
//                 var translations = chunk.GetNativeArray(TranslationType);
//                 var directions = chunk.GetNativeArray(DirectionType);
//
//                 //move away from the mouse unless it is more than 10 units away
//
//                 for (int i = 0; i < chunk.Count; i++)
//                 {
//                     var direction = directions[i];
//                     var translation = translations[i];
//
//                     if (math.abs(translation.Value - MousePos).x < 1 &&
//                         math.abs(translation.Value - MousePos).y < 1)
//                     {
//                         math.normalizesafe(direction.Value = MousePos - translation.Value);
//                         direction.Value.z = 0;
//                         directions[i] = new Direction {Value = -direction.Value*2};
//                     }
//                     else
//                     {
//                         directions[i] = new Direction {Value = float3.zero};
//                     }
//                 }
//             }
//         }
//     }
// }