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
    #region Nested Structures

    private class NestedClass
    {
        public int num { get; set; }

        public NestedClass()
        {
            num = 0;
        }
    }

    #endregion

    public override void SomeVirtualMethod()
    {
        base.SomeVirtualMethod();
        SomeProtectedMethod();

        NestedClass nested = new NestedClass();

        NestedMethod(5);

        #region Nested Methods

        void NestedMethod(int num)
        {
           nested.num = num;
           CalledByNestedMethod();
        }

        #endregion
    }

    private void CalledByNestedMethod()
    {

    }
}
