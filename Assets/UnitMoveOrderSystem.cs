using DefaultNamespace;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class UnitMoveOrderSystem:SystemBase
{
    private EntityCommandBufferSystem ECB;
    protected override void OnCreate()
    {
        ECB = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = ECB.CreateCommandBuffer();
        if (Input.GetMouseButtonDown(1))
        {
            float cellSize = SetupPathFindingGrid.Instance.Grid.GetCellSize();
            Vector3 planeNormal = new Vector3(0, 1, 0);
            Vector3 planeCenter = new Vector3(0, 0, 0);
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 lineOrigin = cameraRay.origin;
            Vector3 lineDirection = cameraRay.direction;

            Vector3 difference = planeCenter - lineOrigin;
            float denominator = Vector3.Dot(lineDirection, planeNormal);
            float t = Vector3.Dot(difference, planeNormal) / denominator;

            float3 planeIntersection = lineOrigin + (lineDirection * t);

            var grid = SetupPathFindingGrid.Instance;
            grid.Grid.GetXY(planeIntersection, out int endX, out int endY);
            ValidateGridPosition(ref endX, ref endY);
           // Debug.Log($"X = {endX}, Y = {endY}");
            Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<PathPosition> pathPositionBuffer,
                ref Translation translation) =>
            {
                grid.Grid.GetXY(translation.Value+(float3)SetupPathFindingGrid.Instance.OriginOffset*cellSize*0.5f ,out int startX, out int startY);
  
                ValidateGridPosition(ref startX, ref startY);
                //Debug.Log($"Added params comp index {entityInQueryIndex}");
                ecb.AddComponent(entity, new PathfindingParams
                {
                    StartPosition = new int2(startX, startY),
                    EndPosition = new int2(endX, endY),
                });

            }).WithStructuralChanges()
                .Run();
        
        }
    }

    private void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, SetupPathFindingGrid.Instance.Grid.GetWidth() - 1);
        y = math.clamp(y, 0, SetupPathFindingGrid.Instance.Grid.GetHeight() - 1);
    }
}