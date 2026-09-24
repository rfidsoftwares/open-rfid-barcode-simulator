namespace OpenScanSim.Common.Constants;

/// <summary>
/// Centralized application and protocol constants.
/// </summary>
public static class AppConstants
{
    public static class Win32
    {
        public const int InputKeyboard = 1;
        public const uint KeyEventFExtendedKey = 0x0001;
        public const uint KeyEventFKeyUp = 0x0002;
        public const uint KeyEventFUnicode = 0x0004;
        public const uint KeyEventFScanCode = 0x0008;

        public const int GwlExStyle = -20;
        public const int WsExNoActivate = 0x08000000;
        public const int WsExTopmost = 0x00000008;

        public const int WmHotkey = 0x0312;
    }

    public static class Protocols
    {
        public const string DefaultEpcHeader = "30";
        public const string DefaultPcWord96Bit = "3000";
        public const string DefaultPcWord128Bit = "4000";
        public const char Gs1Fnc1Separator = '\x1D'; // ASCII 29 [GS]

        public const string AimCodeGs1128 = "]C1";
        public const string AimCodeQrCode = "]Q1";
        public const string AimCodeDataMatrix = "]d2";

        public const ushort Crc16CcittPolynomial = 0x1021;
        public const ushort Crc16Gen2Preset = 0xFFFF;
    }

    public static class Hotkeys
    {
        public const int StartStopHotkeyId = 9001;
        public const int PauseResumeHotkeyId = 9002;
        public const int SingleStepHotkeyId = 9003;

        public const uint VkF8 = 0x77;
        public const uint VkF9 = 0x78;
        public const uint VkF10 = 0x79;
    }

    public static class Defaults
    {
        public const double DefaultCountdownSeconds = 5.0;
        public const double DefaultIntervalSeconds = 2.5;
        public const int DefaultBurstBatchSize = 1;
        public const int DefaultHumanTypingSpeedMs = 110;
        public const int DefaultHumanJitterMs = 35;
        public const string DefaultUhfMask = "3034{HEX:8}{SEQ:8}";
    }
}
