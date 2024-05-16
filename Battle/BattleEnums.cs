using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BooBoo.Battle
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum InputType
    {
        None,

        _1,
        _2,
        _3,
        _4,
        _5,
        _6,
        _7,
        _8,
        _9,
        Crouching_Any,
        Standing_Any,
        Aerial_Any,

        _236,
        _214,
        _41236,
        _63214,
        _623,
        _421,
        _66,
        _44,
    }

    [Flags]
    public enum ButtonType
    {
        A = 0b_0000_0000_0000_0001,
        B = 0b_0000_0000_0000_0010,
        C = 0b_0000_0000_0000_0100,
        D = 0b_0000_0000_0000_1000,
        E = 0b_0000_0000_0001_0000,
        F = 0b_0000_0000_0010_0000,
        G = 0b_0000_0000_0100_0000,
        H = 0b_0000_0000_1000_0000,

        Charge = 0b_0001_0000_0000_0000,
        Release = 0b_0010_0000_0000_0000,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CounterType
    {
        None,
        Normal,
        High,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CancelType
    {
        Whenever,
        OnlyNeutral,
        OnlyHitOrBlock,
        OnlyWhenSpecified,
    }

    public enum StatePosition
    {
        Standing,
        Crouching,
        Aerial
    }

    public enum StateType
    {
        Neutral,
        Normal,
        Special,
        Super,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FloorType
    {
        Concrete,
        Wood,
        Glass,
        Water,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShadowType
    {
        None,
        Shadow,
        Reflection,
        WaterReflection,
        ShadowAndReflection,
        Custom,
    }
}
