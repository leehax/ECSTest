using DefaultNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class UnitMoveOrderSystem:SystemBase
{
    private EntityCommandBufferSystem _ecbSystem;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        if (Input.GetMouseButtonDown(1))
        {
            var grid = SetupPathFindingGrid.Instance;
            float cellSize =grid.Grid.GetCellSize();
            Vector3 planeNormal = new Vector3(0, 1, 0);
            Vector3 planeCenter = new Vector3(0, 0, 0);
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 lineOrigin = cameraRay.origin;
            Vector3 lineDirection = cameraRay.direction;

            Vector3 difference = planeCenter - lineOrigin;
            float denominator = Vector3.Dot(lineDirection, planeNormal);
            float t = Vector3.Dot(difference, planeNormal) / denominator;

            float3 planeIntersection = lineOrigin + (lineDirection * t);
            float3 originOffset = grid.OriginOffset;
            
            GetXY(originOffset,cellSize,planeIntersection, out int endX, out int endY);
            var gridSize = new int2(grid.Grid.GetWidth(), grid.Grid.GetHeight());
            ValidateGridPosition(gridSize, ref endX, ref endY);
            Entities
                .WithNone<PathfindingParams>()
                .ForEach((Entity entity, int entityInQueryIndex,
                DynamicBuffer<PathPosition> pathPositionBuffer,
                ref Translation translation) =>
            {
                //grid.Grid.GetXY(translation.Value+(float3)grid.OriginOffset*cellSize*0.5f ,out int startX, out int startY);
                GetXY(originOffset, cellSize, translation.Value + originOffset * cellSize * 0.5f,
                    out int startX, out int startY);
                ValidateGridPosition(gridSize, ref startX, ref startY);
                ecb.AddComponent(entityInQueryIndex,entity, new PathfindingParams()
                {
                    StartPosition = new int2(startX, startY),
                    EndPosition = new int2(endX, endY),
                });

            }).ScheduleParallel();
            _ecbSystem.AddJobHandleForProducer(Dependency);
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
}