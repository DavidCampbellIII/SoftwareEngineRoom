using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnotherClass : MonoBehaviour
{
    private void Awake()
    {
        SomeMethod(1);
    }

    private void SomeMethod(int num1)
    {

    }

    public virtual void SomeVirtualMethod()
    {

    }

    protected void SomeProtectedMethod()
    {

    }
}

public class YetAnotherClass : AnotherClass
{
    public override void SomeVirtualMethod()
    {
        base.SomeVirtualMethod();
        SomeProtectedMethod();
    }
}
