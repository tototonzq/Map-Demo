#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#define SKELD_NEW_INPUT
#endif
using System.Collections.Generic;
using UnityEngine;
#if SKELD_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace SkeldMap
{
    // ตัวห่อ Input — ใช้ได้ทั้ง Input Manager แบบเดิม และ Input System package
    // (โปรเจกต์ที่สร้างจากเทมเพลตของ Unity 6 มักเปิดเฉพาะ Input System ใหม่ ซึ่ง UnityEngine.Input จะ throw)
    public static class GameInput
    {
#if SKELD_NEW_INPUT
        static readonly Dictionary<KeyCode, Key> map = new Dictionary<KeyCode, Key>
        {
            { KeyCode.W, Key.W }, { KeyCode.A, Key.A }, { KeyCode.S, Key.S }, { KeyCode.D, Key.D },
            { KeyCode.Q, Key.Q }, { KeyCode.E, Key.E }, { KeyCode.F, Key.F }, { KeyCode.M, Key.M },
            { KeyCode.R, Key.R }, { KeyCode.V, Key.V }, { KeyCode.N, Key.N },
            { KeyCode.Space, Key.Space }, { KeyCode.Tab, Key.Tab },
            { KeyCode.UpArrow, Key.UpArrow }, { KeyCode.DownArrow, Key.DownArrow },
            { KeyCode.LeftArrow, Key.LeftArrow }, { KeyCode.RightArrow, Key.RightArrow },
            { KeyCode.C, Key.C }, { KeyCode.B, Key.B }, { KeyCode.T, Key.T }, { KeyCode.G, Key.G }, { KeyCode.LeftBracket, Key.LeftBracket }, { KeyCode.RightBracket, Key.RightBracket },
            { KeyCode.Return, Key.Enter }, { KeyCode.Escape, Key.Escape },
        };

        public static bool Held(KeyCode k)
            => Keyboard.current != null && map.TryGetValue(k, out var key) && Keyboard.current[key].isPressed;

        public static bool Pressed(KeyCode k)
            => Keyboard.current != null && map.TryGetValue(k, out var key) && Keyboard.current[key].wasPressedThisFrame;

        // + = เลื่อนล้อขึ้น (ห่างจากตัว)
        public static float Scroll() => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
        public static bool Held(KeyCode k) => Input.GetKey(k);
        public static bool Pressed(KeyCode k) => Input.GetKeyDown(k);
        public static float Scroll() => Input.mouseScrollDelta.y;
#endif
    }
}
