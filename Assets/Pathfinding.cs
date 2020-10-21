using System;
using System.IO;
using System.Linq;
using DefaultNamespace;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class Pathfinding : SystemBase
{

    public const int MOVE_STRAIGHT_COST = 10;
    public const int MOVE_DIAGONAL_COST = 14;

    private TileGrid<PathNode> _grid;

    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    private EntityQuery _query;

    private NativeArray<int2> _neighbourOffsets;
    //private NativeArray<int2> _neighbourOffsets;
    
    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _query = GetEntityQuery(ComponentType.ReadOnly<AgentTag>(),
            ComponentType.ReadOnly<PathfindingParams>());
        RequireForUpdate(_query);
      
    }

    protected override void OnDestroy()
    {
       // _neighbourOffsets.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        int gridWidth = DefaultNamespace.SetupPathFindingGrid.Instance.Grid.GetWidth();
        int gridHeight = DefaultNamespace.SetupPathFindingGrid.Instance.Grid.GetHeight();
        int2 gridSize = new int2(gridWidth, gridHeight);

        var pathNodeArray = GetPathNodeArray();
        _neighbourOffsets = new NativeArray<int2>(8, Allocator.TempJob)
        {
            [0] = new int2(-1, 0), //left
            [1] = new int2(+1, 0), //right
            [2] = new int2(0, +1), //up
            [3] = new int2(0, -1), //down
            [4] = new int2(-1, -1), //left down
            [5] = new int2(-1, +1), //left up
            [6] = new int2(+1, -1), //right down
            [7] = new int2(+1, +1), //right up
        };
        
        var entities = _query.ToEntityArrayAsync(Allocator.TempJob, out JobHandle jobhandle);
        Dependency = JobHandle.CombineDependencies(Dependency, jobhandle);
        Dependency = new FindPathJob
        {
            GridSize = gridSize,
            PathNodeArray = new NativeArray<PathNode>(pathNodeArray, Allocator.TempJob),
            PathfindingParamsType = GetComponentTypeHandle<PathfindingParams>(),
            PathIndexType = GetComponentTypeHandle<PathIndex>(),
            PathPositionType = GetBufferTypeHandle<PathPosition>(),
            CommandBuffer = ecb,
            EntityType = GetEntityTypeHandle(),
            NeighbourOffsets = new NativeArray<int2>(_neighbourOffsets,Allocator.TempJob),
        }.ScheduleParallel(_query,Dependency);
        
        _ecbSystem.AddJobHandleForProducer(Dependency);
        pathNodeArray.Dispose(Dependency);
        entities.Dispose(Dependency);
        _neighbourOffsets.Dispose(Dependency);

    }

    [BurstCompile]
    private struct FindPathJob : IJobChunk
    {
        public int2 GridSize;
        [DeallocateOnJobCompletion]
        public NativeArray<PathNode> PathNodeArray;
        [ReadOnly]public EntityTypeHandle EntityType;
        public ComponentTypeHandle<PathfindingParams> PathfindingParamsType;
        public ComponentTypeHandle<PathIndex> PathIndexType;
        public BufferTypeHandle<PathPosition> PathPositionType;
        [DeallocateOnJobCompletion] 
        public NativeArray<int2> NeighbourOffsets;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(EntityType);
            var pathfindingParamsArray = chunk.GetNativeArray(PathfindingParamsType);
            var pathIndexArray = chunk.GetNativeArray(PathIndexType);
            var pathPositionBuffers = chunk.GetBufferAccessor(PathPositionType);
            for (int i = 0; i < chunk.Count; i++)
            {
                var pathfindingParams = pathfindingParamsArray[i];
                var endNodeIndex = CalculateIndex(pathfindingParams.EndPosition.x, pathfindingParams.EndPosition.y, GridSize.x);
                var startNode = PathNodeArray[CalculateIndex(pathfindingParams.StartPosition.x, pathfindingParams.StartPosition.y, GridSize.x)];
                startNode.GCost = 0;
                startNode.CalculateFCost();
                PathNodeArray[startNode.Index] = startNode;
                
                NativeList<int> openList = new NativeList<int>(128, Allocator.Temp);
                NativeList<int> closedList = new NativeList<int>(128,Allocator.Temp);
                
                openList.Add(startNode.Index);
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, PathNodeArray);
                var recursionCounter = 0;
                var flags = RecursivePathFinding(currentNodeIndex, endNodeIndex, openList, closedList, recursionCounter);
                
                DynamicBuffer<PathPosition> pathPositionBuffer = pathPositionBuffers[i];
                pathPositionBuffer.Clear();
                if (flags == 0)//Found Path
                {
                    PathNode endNode = PathNodeArray[endNodeIndex];
                    CalculatePath(PathNodeArray, endNode, pathPositionBuffer);
                    pathIndexArray[i] = new PathIndex {Value = pathPositionBuffer.Length - 1};

                }
                else
                {
                    pathIndexArray[i] = new PathIndex {Value = -1};
                }

                openList.Dispose();
                closedList.Dispose();
                CommandBuffer.RemoveComponent<PathfindingParams>(chunkIndex,entities[chunkIndex]);
            }
        }
        private int RecursivePathFinding( int currentNodeIndex, int endNodeIndex, NativeList<int> openList, NativeList<int> closedList, int counter)
        {
            counter++;
            //Debug.Log($"counter {counter}");
            if (counter > 100)
            {
                //Debug.Log("broke out of recursive loop");
                return -1;
            }
            var endNode = PathNodeArray[endNodeIndex];
            var endPosition = new int2(endNode.X, endNode.Y);

            var currentNode = PathNodeArray[currentNodeIndex];
            if (currentNodeIndex == endNodeIndex)
            {
                if (endNode.CameFromNodeIndex == -1)
                {
                    //didnt find path
                    return -1;
                }
                return 0;
            }
            else
            {
                var lowestF = int.MaxValue;
                int index = -1;

                for (int i = 0; i < NeighbourOffsets.Length; i++)
                {
                    int2 neighbourOffset = NeighbourOffsets[i];
                    
                    int2 neighbourPosition = new int2(currentNode.X + neighbourOffset.x,
                        currentNode.Y + neighbourOffset.y);
                    
                    if (!IsPositionValid(neighbourPosition, GridSize))
                    {
                        continue;
                    }
                    int neighbourNodeIndex =
                        CalculateIndex(neighbourPosition.x, neighbourPosition.y, GridSize.x);
                    bool isInOpenOrClosedList =
                        openList.Contains(neighbourNodeIndex) || closedList.Contains(neighbourNodeIndex);
                    if (isInOpenOrClosedList)
                    {
                        continue;
                    }
                    var neighbourNode = PathNodeArray[neighbourNodeIndex];
                    if (neighbourNode.IsWalkable)
                    {
                        var currentNodePosition = new int2(currentNode.X, currentNode.Y);
                      
                        int tentativeGCost = currentNode.GCost +
                                             CalculateDistanceCost(currentNodePosition, neighbourPosition);
                        if (tentativeGCost < neighbourNode.GCost)
                        {

                            if (neighbourNode.CameFromNodeIndex != -1)
                            {
                                Debug.Log(
                                    $"already had a valid came from index{neighbourNode.CameFromNodeIndex}");
                            }

                            neighbourNode.CameFromNodeIndex = currentNode.Index;
                            neighbourNode.GCost = tentativeGCost;
                            neighbourNode.HCost = CalculateDistanceCost(neighbourPosition, endPosition);
                            neighbourNode.CalculateFCost();
                            PathNodeArray[neighbourNodeIndex] = neighbourNode;

                            openList.Add(neighbourNode.Index);
                        }
                    }
                }
                if(!closedList.Contains(currentNode.Index))
                    closedList.Add(currentNode.Index);
                var indexToRemove = -1;
                for (int j = 0; j < openList.Length; j++)
                {
                    if (openList[j] == currentNode.Index)
                    {
                        indexToRemove = j;
                        break;
                    }
                }
                if(indexToRemove>=0)
                    openList.RemoveAtSwapBack(indexToRemove);
                
                if (openList.Length > 0)
                {
                    for (int i = 0; i < openList.Length; i++)
                    {
                        if (PathNodeArray[openList[i]].FCost < lowestF)
                        {
                            lowestF = PathNodeArray[openList[i]].FCost;
                            index = i;
                        }
                    }

                    currentNodeIndex = openList[index];
                    return RecursivePathFinding(currentNodeIndex,endNodeIndex,openList,closedList, counter);
                }

                return -1;
            }
        }
    }

   
    private NativeArray<PathNode> GetPathNodeArray()
    {
        TileGrid<GridNode> grid = DefaultNamespace.SetupPathFindingGrid.Instance.Grid;

        int2 gridSize = new int2(grid.GetWidth(), grid.GetHeight());
       // Debug.Log($"GridSize {gridSize}");
        NativeArray<PathNode> pathNodeArray =
            new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                var pathNode = new PathNode
                {
                    X = x,
                    Y = y,
                    Index = CalculateIndex(x, y, gridSize.x),
                    GCost = int.MaxValue,
                    IsWalkable = grid.GetValue(x, y).IsWalkable(),
                    CameFromNodeIndex = -1
                };

        
                pathNodeArray[pathNode.Index] = pathNode;
            }
        }

        return pathNodeArray;
    }

    private static NativeArray<PathNode> GetNeighbours(NativeArray<PathNode> pathNodeArray,
        NativeArray<PathNode> neighbourOffsets, PathNode currentNode, int2 gridSize )
    {
        var neighbours = new NativeArray<PathNode>();
        for (int i = 0; i < neighbourOffsets.Length; i++)
        {
            neighbours[i] =
                pathNodeArray[
                    CalculateIndex(currentNode.X + neighbourOffsets[i].X,
                        currentNode.Y + neighbourOffsets[i].Y, gridSize.x)];
        }

        return neighbours;
    }

    private static void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode,
        DynamicBuffer<PathPosition> pathPositionBuffer)
    {
        if (endNode.CameFromNodeIndex == -1)
        {
            //didnt find path
            Debug.Log("Didnt find path");
        }

        //found path
        pathPositionBuffer.Add(new PathPosition {Value = new int2(endNode.X, endNode.Y)});
        var existingList = new NativeList<int>(Allocator.Temp);
        var currentNode = endNode;
        existingList.Add(currentNode.Index);
        while (currentNode.CameFromNodeIndex != -1)
        {
            var cameFromNode = pathNodeArray[currentNode.CameFromNodeIndex];

            if (existingList.Contains(cameFromNode.Index))
            {
                Debug.Log("BREAK PATH CONSTRUCTION, DUPLICATE ");
                break;
            }
            else
            {
                existingList.Add(cameFromNode.Index);
                pathPositionBuffer.Add(new PathPosition {Value = new int2(cameFromNode.X, cameFromNode.Y)});
                currentNode = cameFromNode;
            }

        }
    }

    private static NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
    {
        if (endNode.CameFromNodeIndex == -1)
        {
            //didnt find path
            return new NativeList<int2>(Allocator.Temp);
        }

        //found path
        var path = new NativeList<int2>(Allocator.Temp);
        path.Add(new int2(endNode.X, endNode.Y));

        var currentNode = endNode;
        while (currentNode.CameFromNodeIndex != -1)
        {
            var cameFromNode = pathNodeArray[currentNode.CameFromNodeIndex];
            path.Add(new int2(cameFromNode.X, cameFromNode.Y));
            currentNode = cameFromNode;
        }

        return path;
    }
    

    private static bool IsPositionValid(int2 gridPosition, int2 gridSize)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }

    private static int CalculateIndex(int x, int y, int gridWidth)
    {
        return x + y * gridWidth;
    }

    private static int2 GetXY(int index, int gridWidth)
    {
        return new int2(index % gridWidth, index / gridWidth);
    }

    private static int CalculateDistanceCost(int2 aPosition, int2 bPosition)
    {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    {
        var lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 0; i < openList.Length; i++)
        {
            var testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.FCost < lowestCostPathNode.FCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }

        return lowestCostPathNode.Index;
    }

    public struct PathNode
    {
        public int X;
        public int Y;

        public int Index;

        public int GCost;
        public int HCost;
        public int FCost;

        public bool IsWalkable;

        public int CameFromNodeIndex;

        public void CalculateFCost()
        {
            FCost = GCost + HCost;
        }

        public void SetIsWalkable(bool isWalkable)
        {
            IsWalkable = isWalkable;
        }


    }


}