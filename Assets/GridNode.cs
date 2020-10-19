using Unity.Transforms;

namespace DefaultNamespace
{
    public class GridNode
    {
        private TileGrid<GridNode> _grid;
        private int _x;
        private int _y;
        private bool _isWalkable;
        private bool _isPath;
        public GridNode(TileGrid<GridNode> grid, int x, int y)
        {
            _grid = grid;
            _x = x;
            _y = y;
            _isWalkable = true;
        }

        public bool IsWalkable()
        {
            return _isWalkable;
        }

        public bool IsPath()
        {
            return _isPath;
        }

        public void SetIsPath(bool isPath)
        {
            _isPath = isPath;
            _grid.TriggerGridObjectChanged(_x, _y);
        }
        public void SetIsWalkable(bool isWalkable)
        {
            _isWalkable = isWalkable;
            _grid.TriggerGridObjectChanged(_x, _y);
        }
    }
}