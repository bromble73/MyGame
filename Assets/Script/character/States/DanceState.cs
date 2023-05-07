using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class DanceState : State
{
    private ThirdPersonController _tpc;
    public DanceState(ThirdPersonController tpc)
    {
        _tpc = tpc;
    }
    
    public override void Enter()
    {
        base.Enter();
        Debug.Log("Я начинаю танцевать");
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log("Я больше не танцую");
    }

    public override void Tick()
    {
        base.Tick();
        Debug.Log("Я сейчас танцую");
        Dance();
    }
    
    private void Dance()
    {
        if (_tpc.input.dance)
        {
            if (_tpc.animator.GetBool(_tpc._animIDDance))
            {
                _tpc.animator.SetBool(_tpc._animIDDance, false);
                _tpc.stateMachine.ChangeState(new IdleState());
            }
            else
            {
                _tpc.animator.SetBool(_tpc._animIDDance, true);
            }

            _tpc.input.dance = false;
        }
    }
}

