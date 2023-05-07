using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : State
{
    public override void Enter()
    {
        base.Enter();
        Debug.Log("Я начинаю просто стоять");
    }

    public override void Exit()
    {
        base.Enter();
        Debug.Log("Я закончил просто стоять");
    }
}
