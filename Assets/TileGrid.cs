using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class TileGrid<TGridObject> 
{
    private int _width;
    private int _height;
    private float _cellSize;
    private Vector3 _originOffset;
    private TGridObject[,] _gridArray;

    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;

    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int y;
    }
    public TileGrid(int width, int height, float cellSize, Vector3 originOffset, Func<TileGrid<TGridObject>, int, int, TGridObject> createGridObject)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _originOffset = originOffset;
        _gridArray = new TGridObject[width, height];
        for (int x = 0; x < _gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < _gridArray.GetLength(1); y++)
            {
                _gridArray[x, y] = createGridObject(this,x,y);
            }   
        }
    }

    public void DrawGizmos()
    {
        Gizmos.color = Color.magenta;
        for (int x = 0; x < _gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < _gridArray.GetLength(1); y++)
            {
                Gizmos.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1));
                Gizmos.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y));
            }
        }
        Gizmos.DrawLine(GetWorldPosition(0,_height),GetWorldPosition(_width,_height));
        Gizmos.DrawLine(GetWorldPosition(_width,0),GetWorldPosition(_width,_height));
    }

    public int GetWidth()
    {
        return _width;
    }

    public int GetHeight()
    {
        return _height;
    }

    public float GetCellSize()
    {
        return _cellSize;
    }
    
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, 0,y) * _cellSize + _originOffset;
    }
    
    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - _originOffset).x / _cellSize);
        y = Mathf.FloorToInt((worldPosition - _originOffset).z / _cellSize);
    }

    public void SetValue(int x, int y, TGridObject value)
    {
        if (x >= 0 && y >= 0 && x < _width && y < _height)
        {
            _gridArray[x, y] = value;
            if(OnGridValueChanged!=null)
                OnGridValueChanged(this,new OnGridValueChangedEventArgs{x=x,y=y});
        }
    }
    public TGridObject GetValue(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < _width && y < _height)
        {
            return _gridArray[x, y];
        }
        else
        {
            return default(TGridObject);
        }
    }

    public TGridObject GetValue(Vector3 worldPosition)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetValue(x, y);
    }
    public void TriggerGridObjectChanged(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < _width && y < _height)
        {
            if(OnGridValueChanged!=null)
                OnGridValueChanged(this,new OnGridValueChangedEventArgs{x=x,y=y});
        }
    }
}
