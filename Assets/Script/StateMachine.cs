using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public State currentState { get; set; }

    public void Initialize(State startState)
    {
        currentState = startState;
        currentState.Enter();
    }

    public void ChangeState(State newState)
    {
        currentState.Exit(); // Выходим из предыдущего состояния
        currentState = newState; // Определяем новое состояние
        currentState.Enter(); // Входим в новое состояние
    }
}
