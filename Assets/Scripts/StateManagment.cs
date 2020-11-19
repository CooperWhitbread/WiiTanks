using UnityEngine;
using System.Collections.Generic;


//State Management
[System.Serializable]
class StateManager
{
    //Can be controlled mainly in parent class but contains basic functionality to work in the background

    /// Data storage
    public enum State
    {
        Empty,
        Attack,
        Stelth,
        Idle,
        Chase,
        Escape
    }
    [System.Serializable]
    public struct StateInfo
    {
        public State M_State;
        public float M_TimeLength;
    }

    /// Variables
    public State M_CurrentState { get; set; } = State.Empty;
    public float M_TimeForNextSwitch { get; set; } = 0.0f;
    public int M_CurrentLevelInState { get; set; } = 0;
    [SerializeField] List<StateInfo> M_States;

    /// Functions
    public void Start(float currentTime)
    {
        if (M_States.Count != 0)
        {
            M_CurrentState = M_States[M_CurrentLevelInState].M_State;
            M_TimeForNextSwitch = currentTime + M_States[M_CurrentLevelInState].M_TimeLength;

        }
    }
    public void Update(float currentTime)
    {
        if (M_CurrentState != State.Empty && M_States.Count != 0)
        {
            //Error checking to make sure states are initialized correctly
            if (M_TimeForNextSwitch <= currentTime)
            {
                M_CurrentLevelInState++;
                if (M_States.Count > M_CurrentLevelInState)
                {
                    M_CurrentState = M_States[M_CurrentLevelInState].M_State;
                    M_TimeForNextSwitch = currentTime + M_States[M_CurrentLevelInState].M_TimeLength;
                }
                else
                {
                    M_CurrentLevelInState = 0;
                    M_CurrentState = M_States[M_CurrentLevelInState].M_State;
                    M_TimeForNextSwitch = currentTime + M_States[M_CurrentLevelInState].M_TimeLength;
                }
            }
        }
    }
}
