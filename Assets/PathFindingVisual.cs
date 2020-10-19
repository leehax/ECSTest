using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.WSA;

namespace DefaultNamespace
{
    public class PathFindingVisual : MonoBehaviour
    {
        private TileGrid<GridNode> _grid;
        private Mesh _mesh;
        private bool _updateMesh;

        private void Awake()
        {
            _mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = _mesh;
            _updateMesh = true;
        }

        public void SetGrid(TileGrid<GridNode> grid)
        {
            _grid = grid;

            _grid.OnGridValueChanged += Grid_OnValueChanged;
        }

        private void Grid_OnValueChanged(object sender, TileGrid<GridNode>.OnGridValueChangedEventArgs e)
        {
            _updateMesh = true;
        }

        private void LateUpdate()
        {
            if (_updateMesh)
            {
                _updateMesh = false;
                UpdateVisual();
            }
        }

        private void UpdateVisual()
        {
            MeshUtils.CreateEmptyMeshArrays(_grid.GetWidth() * _grid.GetHeight(), out Vector3[] vertices,
                out Vector2[] uv, out int[] triangles);

            for (int x = 0; x < _grid.GetWidth(); x++)
            {
                for (int y = 0; y < _grid.GetHeight(); y++)
                {
                    int index = x * _grid.GetHeight() + y;
                    Vector3 quadSize = new Vector3(1, 0, 1) * _grid.GetCellSize();
                    var gridValue = _grid.GetValue(x, y);
                    float gridValueNormalized = gridValue.IsWalkable() ? 1f : 0f;
                    Vector2 gridValueUV = new Vector2(gridValueNormalized, 0f);

                    // if (!gridValue.IsWalkable())
                    // {
                    //     vertices[index] += new Vector3(0, 1, 0);
                    // }
                    MeshUtils.AddToMeshArrays(vertices, uv, triangles, index,
                        _grid.GetWorldPosition(x, y) + quadSize * .5f, 0f, quadSize, gridValueUV,
                        gridValueUV);

                   
                }
            }

            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
        }
    }
}