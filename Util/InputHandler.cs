using System;
using System.Numerics;
using BooBoo.Battle;
using Raylib_cs;

namespace BooBoo.Util
{
    internal class InputHandler
    {
        public bool useKeyboard = false;
        public int controllerNum = -1;

        public InputHandler(bool useKeyboard, int controllerNum)
        {
            this.useKeyboard = useKeyboard;
            this.controllerNum = controllerNum;
        }

        public Vector2 GetDirectionVector()
        {
            Vector2 returnVector = Vector2.Zero;

            if (useKeyboard)
            {
                float LR = Raylib.IsKeyDown(KeyboardKey.D) - Raylib.IsKeyDown(KeyboardKey.A);
                float UD = Raylib.IsKeyDown(KeyboardKey.W) - Raylib.IsKeyDown(KeyboardKey.S);

                Vector2 vec = new Vector2(LR, UD);
                returnVector = Vector2.Normalize(vec);
            }
            else if (controllerNum > -1)
            {
                Vector2 axis = new Vector2(Raylib.GetGamepadAxisMovement(controllerNum, GamepadAxis.LeftX),
                    -Raylib.GetGamepadAxisMovement(controllerNum, GamepadAxis.LeftY));
                Vector2 dpad = new Vector2(
                    Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.LeftFaceRight) - Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.LeftFaceLeft),
                    Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.LeftFaceUp) - Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.LeftFaceDown));
                axis.X = MathF.Round(axis.X);
                axis.Y = MathF.Round(axis.Y);
                returnVector = Vector2.Normalize(axis + dpad);
            }

            return returnVector;
        }

        public int GetInputDirection()
        {
            Vector2 vec = GetDirectionVector();

            int direction = 5;

            if (MathF.Abs(vec.Y) >= 0.5f)
            {
                direction += vec.Y > 0 ? 3 : -3;
            }

            if (MathF.Abs(vec.X) >= 0.5f)
            {
                direction += vec.X > 0 ? 1 : -1;
            }

            return direction;
        }

        public ButtonType GetBattleButton()
        {
            ButtonType button = 0;

            if(useKeyboard)
            {
                button |= Raylib.IsKeyDown(KeyboardKey.J) ? ButtonType.A : 0;
                button |= Raylib.IsKeyDown(KeyboardKey.I) ? ButtonType.B : 0;
                button |= Raylib.IsKeyDown(KeyboardKey.L) ? ButtonType.C : 0;
                button |= Raylib.IsKeyDown(KeyboardKey.K) ? ButtonType.D : 0;
                button |= Raylib.IsKeyDown(KeyboardKey.O) ? ButtonType.E : 0;
            }
            else if(controllerNum > -1)
            {
                button |= Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.RightFaceLeft) ? ButtonType.A : 0;
                button |= Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.RightFaceUp) ? ButtonType.B : 0;
                button |= Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.RightFaceRight) ? ButtonType.C : 0;
                button |= Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.RightFaceDown) ? ButtonType.D : 0;
                button |= Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.RightTrigger1) ? ButtonType.E : 0;
            }

            return button;
        }

        public bool MenuSelectDown()
        {
            if (useKeyboard)
                return Raylib.IsKeyDown(KeyboardKey.J);
            else if (controllerNum > -1)
                return Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.RightFaceDown);
            return false;
        }

        public bool MenuBackDown()
        {
            if (useKeyboard)
                return Raylib.IsKeyDown(KeyboardKey.I);
            else if (controllerNum > -1)
                return Raylib.IsGamepadButtonDown(controllerNum, GamepadButton.RightFaceRight);
            return false;
        }
    }
}
