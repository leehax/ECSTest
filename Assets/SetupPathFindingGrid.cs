using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.WSA;

namespace DefaultNamespace
{
    public class SetupPathFindingGrid : MonoBehaviour
    {
        public static SetupPathFindingGrid Instance { private set; get; }
        public int Width = 5;
        public int Height = 5;
        public float CellSize = 10f;
        public Vector3 OriginOffset = new Vector3(0, 0, 0);

        [SerializeField] private PathFindingVisual _pathFindingVisual;
        public TileGrid<GridNode> Grid;

        private void Awake()
        {
            if (Instance != null)
                Destroy(this);
            if(Instance==null)
                Instance = this;
        }

        private void Start()
        {
            Grid = new TileGrid<GridNode>(Width, Height, CellSize, OriginOffset,
                (TileGrid<GridNode> grid, int x, int y) => new GridNode(grid, x, y));
            _pathFindingVisual.SetGrid(Grid);
        }
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
               
                Vector3 planeNormal = new Vector3(0, 1, 0);
                Vector3 planeCenter = gameObject.transform.position;
                Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 lineOrigin = cameraRay.origin;
                Vector3 lineDirection = cameraRay.direction;

                Vector3 difference = planeCenter - lineOrigin;
                float denominator = Vector3.Dot(lineDirection, planeNormal);
                float t = Vector3.Dot(difference, planeNormal) / denominator;

                Vector3 planeIntersection = lineOrigin + (lineDirection * t);
                
                int x, y;
                Grid.GetXY(planeIntersection, out x, out y);
                
                Debug.Log($"X = {x}, Y = {y}");
                var node = Grid.GetValue(x, y);
                if (node != null)
                {
                    node.SetIsWalkable(!node.IsWalkable());
                }
            }
        }

        private void OnDrawGizmos()
        {
            if(Grid!=null)
                Grid.DrawGizmos();
        }
    }
}