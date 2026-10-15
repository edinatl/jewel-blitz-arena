using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>
    /// Keyboard greybox controls → quantized <see cref="InputFrame"/>.
    /// P1: WASD move, J jump, K attack, L action, I strong, U/O surges.
    /// P2: arrows move, keypad 1 jump, 2 attack, 3 action.
    /// </summary>
    public static class LocalInputReader
    {
        public static InputFrame ReadPlayer1()
        {
            int x = 0;
            int z = 0;
            if (Input.GetKey(KeyCode.A))
            {
                x -= 1;
            }

            if (Input.GetKey(KeyCode.D))
            {
                x += 1;
            }

            if (Input.GetKey(KeyCode.S))
            {
                z -= 1;
            }

            if (Input.GetKey(KeyCode.W))
            {
                z += 1;
            }

            return InputQuantizer.FromCardinal(
                x,
                z,
                jump: Input.GetKey(KeyCode.J),
                attack: Input.GetKey(KeyCode.K),
                action: Input.GetKey(KeyCode.L),
                strong: Input.GetKey(KeyCode.I),
                surgeA: Input.GetKey(KeyCode.U),
                surgeB: Input.GetKey(KeyCode.O));
        }

        public static InputFrame ReadPlayer2()
        {
            int x = 0;
            int z = 0;
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                x -= 1;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                x += 1;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                z -= 1;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                z += 1;
            }

            return InputQuantizer.FromCardinal(
                x,
                z,
                jump: Input.GetKey(KeyCode.Keypad1) || Input.GetKey(KeyCode.Alpha1),
                attack: Input.GetKey(KeyCode.Keypad2) || Input.GetKey(KeyCode.Alpha2),
                action: Input.GetKey(KeyCode.Keypad3) || Input.GetKey(KeyCode.Alpha3));
        }
    }
}
