using UnityEngine;
using System.Collections.Generic;


//State Management
[System.Serializable]
public class StateManager
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
        public State State;
        public float TimeLengthMin;
        public float TimeLengthMax;

        public StateInfo(State state, float timeLengthMin, float timeLengthMax)
        {
            State = state;
            TimeLengthMax = timeLengthMax;
            TimeLengthMin = timeLengthMin;
        }
    }

    /// Variables
    public State CurrentState { get; set; } = State.Empty;
    public float TimeForNextSwitch { get; set; } = 0.0f;
    public int CurrentLevelInState { get; set; } = 0;
    [SerializeField] List<StateInfo> States = new List<StateInfo>();

    /// Functions
    public void Start(float currentTime)
    {
        if (States.Count != 0)
        {
            CurrentState = States[CurrentLevelInState].State;
            TimeForNextSwitch = currentTime + Random.Range(States[CurrentLevelInState].TimeLengthMin, States[CurrentLevelInState].TimeLengthMax);
        }
    }
    public void Update(float currentTime)
    {
        if (CurrentState != State.Empty && States.Count != 0)
        {
            //Error checking to make sure states are initialized correctly
            if (TimeForNextSwitch <= currentTime)
            {
                CurrentLevelInState++;
                if (States.Count > CurrentLevelInState)
                {
                    CurrentState = States[CurrentLevelInState].State;
                    TimeForNextSwitch = currentTime + Random.Range(States[CurrentLevelInState].TimeLengthMin, States[CurrentLevelInState].TimeLengthMax);
                }
                else
                {
                    CurrentLevelInState = 0;
                    CurrentState = States[CurrentLevelInState].State;
                    TimeForNextSwitch = currentTime + Random.Range(States[CurrentLevelInState].TimeLengthMin, States[CurrentLevelInState].TimeLengthMax);
                }
            }
        }
    }

    public void SwitchStateTo(State state, float currentTime)
    {
        for (int i = 0; i < States.Count; i++)
        {
            if (States[i].State == state)
            {
                CurrentLevelInState = i;
                CurrentState = States[CurrentLevelInState].State;
                TimeForNextSwitch = currentTime + Random.Range(States[CurrentLevelInState].TimeLengthMin, States[CurrentLevelInState].TimeLengthMax);
                return;
            }
        }
    }
}
