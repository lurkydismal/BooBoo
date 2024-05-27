using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BooBoo.Battle
{
    internal class StateList
    {
        [JsonProperty("NeutralStates")]
        public State[] NeutralStates;
        [JsonProperty("NormalStates")]
        public State[] NormalStates;
        [JsonProperty("SpecialStates")]
        public State[] SpecialStates;
        [JsonProperty("SuperStates")]
        public State[] SuperStates;
        [JsonProperty("HitstunLoopData")]
        public HitstunLoopDef HitstunLoopData;

        public Dictionary<string, State> stateMap { get; private set; }

        public List<State> AllStates { get { return stateMap.Values.ToList(); } }

        public void UpdateStateMap()
        {
            stateMap = new Dictionary<string, State>();
            foreach (State state in NeutralStates)
            {
                state.stateType = StateType.Neutral;
                stateMap.Add(state.Name, state);
            }
            foreach (State state in NormalStates)
            {
                state.stateType = StateType.Normal;
                stateMap.Add(state.Name, state);
            }
            foreach (State state in SpecialStates)
            {
                state.stateType = StateType.Special;
                stateMap.Add(state.Name, state);
            }
            foreach (State state in SuperStates)
            {
                state.stateType = StateType.Super;
                stateMap.Add(state.Name, state);
            }
        }

        public State GetState(string state)
        {
            return stateMap[state];
        }

        public bool HasState(string state)
        {
            return stateMap.ContainsKey(state);
        }

        public State this[string key] { get { return stateMap[key]; } }

        public class State
        {
            [JsonProperty("Name")]
            public string Name;
            [JsonProperty("Position")]
            public StatePosition Position;
            [JsonProperty("Input")]
            public InputType Input;
            [JsonProperty("Button")]
            public ButtonType Button;
            [JsonProperty("Priority")]
            public int Priority;
            [JsonProperty("CancelType")]
            public CancelType CancelType;
            [JsonProperty("CounterType")]
            public CounterType CounterType;
            [JsonProperty("HitOrBlockCancels")]
            public string[] HitOrBlockCancels;
            [JsonProperty("HitCancels")]
            public string[] HitCancels;
            [JsonProperty("WhiffCancels")]
            public string[] WhiffCancels;
            [JsonProperty("Looping")]
            public bool Looping;
            [JsonProperty("LoopPos")]
            public int LoopPos;
            [JsonProperty("NextState")]
            public string NextState;
            [JsonProperty("LandToState")]
            public bool LandToState;
            [JsonProperty("LandState")]
            public string LandState;
            [JsonProperty("StateCanTurn")]
            public bool StateCanTurn;
            [JsonProperty("TurnState")]
            public string TurnState;
            public StateType stateType;
        }

        public struct HitstunLoopDef
        {
            [JsonProperty("StaggerLoopPos")]
            public int StaggerLoopPos;
            [JsonProperty("LaunchLoopPos")]
            public int LaunchLoopPos;
            [JsonProperty("FallLoopPos")]
            public int FallLoopPos;
            [JsonProperty("TripLoopPos")]
            public int TripLoopPos;
            [JsonProperty("BlowbackLoopPos")]
            public int BlowbackLoopPos;
            [JsonProperty("DiagonalSpinLoopPos")]
            public int DiagonalSpinLoopPos;
        }
    }
}
