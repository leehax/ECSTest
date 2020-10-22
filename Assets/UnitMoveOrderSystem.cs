using DefaultNamespace;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class UnitMoveOrderSystem:SystemBase
{
    private EntityCommandBufferSystem _ecbSystem;
    private EntityQuery _query;
    private NativeArray<Random> _randoms;
    private SetupPathFindingGrid _grid;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        _query = GetEntityQuery(ComponentType.ReadOnly<AgentTag>(), ComponentType.ReadOnly<AwaitingOrder>(), ComponentType.Exclude<PathfindingParams>());
        _randoms = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory);
        var r = (uint) UnityEngine.Random.Range(int.MaxValue, int.MinValue);
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            _randoms[i] = new Random(r == 0 ? r + 1 : r);
        }
       
    }

    protected override void OnDestroy()
    {
        _randoms.Dispose();
    }

    protected override void OnUpdate()
    {
        if (_grid == null)
            _grid = SetupPathFindingGrid.Instance;

        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        var grid = SetupPathFindingGrid.Instance;
        float cellSize = grid.Grid.GetCellSize();
        float3 originOffset = grid.OriginOffset;
        //GetXY(originOffset,cellSize,planeIntersection, out int endX, out int endY);
        var gridSize = new int2(grid.Grid.GetWidth(), grid.Grid.GetHeight());
        var mousePos = GetMousePosOnPlane();
        GetXY(originOffset, cellSize, mousePos, out int endX, out int endY);
        var entities = _query.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jobhandle);
        Dependency = JobHandle.CombineDependencies(Dependency, jobhandle);
        Dependency = new RandomMoveOrderJob
        {
            CommandBuffer = ecb,
            EntityType = GetEntityTypeHandle(),
            GridSize = gridSize,
            OriginOffset = originOffset,
            CellSize = cellSize,
            //EndPos = new int2(endX, endY),
            TranslationType = GetComponentTypeHandle<Translation>(true),
            Randoms = _randoms
        }.ScheduleParallel(_query, Dependency);
        // Entities
        //     .WithNone<PathfindingParams>()
        //     .ForEach((Entity entity, int entityInQueryIndex,
        //     DynamicBuffer<PathPosition> pathPositionBuffer,
        //     ref Translation translation) =>
        // {
        //     Random random = new Random((uint)Time.DeltaTime+1);
        //     var end = GetRandomPos(random,gridSize);
        //     int endX = end.x;
        //     int endY = end.y;
        //     ValidateGridPosition(gridSize, ref endX, ref endY);
        //     //grid.Grid.GetXY(translation.Value+(float3)grid.OriginOffset*cellSize*0.5f ,out int startX, out int startY);
        //     GetXY(originOffset, cellSize, translation.Value + originOffset * cellSize * 0.5f,
        //         out int startX, out int startY);
        //     ValidateGridPosition(gridSize, ref startX, ref startY);
        //     ecb.AddComponent(entityInQueryIndex,entity, new PathfindingParams()
        //     {
        //         StartPosition = new int2(startX, startY),
        //         EndPosition = new int2(endX, endY),
        //     });
        //
        // }).ScheduleParallel();
        _ecbSystem.AddJobHandleForProducer(Dependency);
        entities.Dispose(Dependency);

    }

    private struct RandomMoveOrderJob : IJobChunk
    {
       // [NativeSetThreadIndex] private int _threadId;

        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public int2 GridSize;

        [ReadOnly] public float3 OriginOffset;
        [ReadOnly] public float CellSize;
        //[ReadOnly] public int2 EndPos;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationType;
        public NativeArray<Random> Randoms;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(EntityType);
            var translations = chunk.GetNativeArray(TranslationType);
            for (int i = 0; i < chunk.Count; i++)
            {
                var translation = translations[i];
                var random = Randoms[chunkIndex];
                var end = random.NextInt2(new int2(0, 0), GridSize - 1);
                //var end = EndPos;
                Randoms[chunkIndex] = random;
                ValidateGridPosition(GridSize, ref end);
                GetXY(OriginOffset, CellSize, translation.Value + OriginOffset * CellSize * 0.5f,
                    out int startX, out int startY);
                ValidateGridPosition(GridSize, ref startX, ref startY);
                CommandBuffer.AddComponent(chunkIndex, entities[i], new PathfindingParams()
                {
                    StartPosition = new int2(startX, startY),
                    EndPosition = end
                });
                CommandBuffer.RemoveComponent<AwaitingOrder>(chunkIndex, entities[0]);
            }
        }
    }
    private static void GetXY(float3 originOffset, float cellSize, float3 worldPosition, out int x, out int y)
    {      
        x = Mathf.FloorToInt((worldPosition - originOffset).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originOffset).z / cellSize);
    }

    private static void ValidateGridPosition(int2 gridDimensions, ref int x, ref int y)
    {
        x = math.clamp(x, 0, gridDimensions.x - 1);
        y = math.clamp(y, 0, gridDimensions.y - 1);
    }
    
    private static void ValidateGridPosition(int2 gridDimensions, ref int2 xy)
    {
        xy.x = math.clamp(xy.x, 0, gridDimensions.x - 1);
        xy.y = math.clamp(xy.y, 0, gridDimensions.y - 1);
    }

    private static float3 GetMousePosOnPlane()
    {
        Vector3 planeNormal = new Vector3(0, 1, 0);
        Vector3 planeCenter = new Vector3(0, 0, 0);
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 lineOrigin = cameraRay.origin;
        Vector3 lineDirection = cameraRay.direction;

        Vector3 difference = planeCenter - lineOrigin;
        float denominator = Vector3.Dot(lineDirection, planeNormal);
        float t = Vector3.Dot(difference, planeNormal) / denominator;

        float3 planeIntersection = lineOrigin + (lineDirection * t);
        return planeIntersection;
    }
}