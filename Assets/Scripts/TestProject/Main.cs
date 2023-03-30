using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    private float testFloat;

    private void Start()
    {
        Test();

        LineGridMask mask = new LineGridMask(new Vector2Int(0, 0), new Vector2Int(1, 1));
        Debug.Log(mask.ShouldMaskPoint(new Vector2Int(0, 1)));
    }

    public void Test()
    {
        testFloat += 1;
    }
}
