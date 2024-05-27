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
            public string Name = "";
            [JsonProperty("Position")]
            public StatePosition Position = StatePosition.Standing;
            [JsonProperty("Input")]
            public InputType Input = InputType.None;
            [JsonProperty("Button")]
            public ButtonType Button = 0;
            [JsonProperty("Priority")]
            public int Priority = 0;
            [JsonProperty("CancelType")]
            public CancelType CancelType = CancelType.Whenever;
            [JsonProperty("CounterType")]
            public CounterType CounterType = CounterType.None;
            [JsonProperty("HitOrBlockCancels")]
            public string[] HitOrBlockCancels = new string[0];
            [JsonProperty("HitCancels")]
            public string[] HitCancels = new string[0];
            [JsonProperty("WhiffCancels")]
            public string[] WhiffCancels = new string[0];
            [JsonProperty("AutoAddCancels")]
            public bool AutoAddCancels = true;
            [JsonProperty("Looping")]
            public bool Looping = false;
            [JsonProperty("LoopPos")]
            public int LoopPos = 0;
            [JsonProperty("NextState")]
            public string NextState = "CmnStand";
            [JsonProperty("LandToState")]
            public bool LandToState = false;
            [JsonProperty("LandState")]
            public string LandState = "CmnLand";
            [JsonProperty("StateCanTurn")]
            public bool StateCanTurn = false;
            [JsonProperty("TurnState")]
            public string TurnState = "";
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
