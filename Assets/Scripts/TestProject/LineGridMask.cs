using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineGridMask : IGridMask
{
    /// <summary>
    /// Max amount a point's distance can differ from the distance which would consider the point to be masked
    /// </summary>
    private const float POINT_DISTANCE_THRESHOLD = 0.001f;

    #region Nested Structures

    public class NestedClass
    {
        public int num { get; }

        public NestedClass()
        {
            num = 0;
        }
    }

    #endregion

    public Vector2Int startCoord { get; }
    public Vector2Int endCoord { get; }

    private readonly float distanceBetweenInitialPoints;

    public LineGridMask(Vector2Int startCoord, Vector2Int endCoord)
    {
        this.startCoord = startCoord;
        this.endCoord = endCoord;
        distanceBetweenInitialPoints = Vector2Int.Distance(startCoord, endCoord);
    }

    /// <summary>
    /// Checks if we should mask the given point because it falls on this line.
    /// </summary>
    /// <param name="point">Point we want to check</param>
    /// <returns>If we should mask the point or not (if it falls on this line or not)</returns>
    public bool ShouldMaskPoint(Vector2Int point)
    {
        float dist = Vector2Int.Distance(point, startCoord) + Vector2Int.Distance(point, endCoord);
        return Mathf.Abs(dist - distanceBetweenInitialPoints)  <= POINT_DISTANCE_THRESHOLD;
    }
}
