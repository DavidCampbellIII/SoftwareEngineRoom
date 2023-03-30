using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGridMask
{
    /// <summary>
    /// Checks if we should mask the given point
    /// </summary>
    /// <param name="point">Point we want to check</param>
    public bool ShouldMaskPoint(Vector2Int point);
}
