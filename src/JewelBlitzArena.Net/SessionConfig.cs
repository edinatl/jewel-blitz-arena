namespace JewelBlitzArena.Net
{
    /// <summary>
    /// GGPO-style session knobs (architecture §3 / PLAN D3).
    /// </summary>
    public readonly struct SessionConfig
    {
        public const int DefaultInputDelay = 2;
        public const int DefaultRollbackWindow = 8;
        public const int MaxInputDelay = 4;
        public const int HashIntervalFrames = 30;
        public const int MaxPlayers = 4;

        public int LocalPlayerIndex { get; }
        public int PlayerCount { get; }
        public int InputDelay { get; }
        public int RollbackWindow { get; }

        public SessionConfig(
            int localPlayerIndex,
            int playerCount = 2,
            int inputDelay = DefaultInputDelay,
            int rollbackWindow = DefaultRollbackWindow)
        {
            if (playerCount < 1 || playerCount > MaxPlayers)
            {
                throw new System.ArgumentOutOfRangeException(nameof(playerCount));
            }

            if (localPlayerIndex < 0 || localPlayerIndex >= playerCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(localPlayerIndex));
            }

            if (inputDelay < 0 || inputDelay > MaxInputDelay)
            {
                throw new System.ArgumentOutOfRangeException(nameof(inputDelay));
            }

            if (rollbackWindow < 1 || rollbackWindow > 16)
            {
                throw new System.ArgumentOutOfRangeException(nameof(rollbackWindow));
            }

            LocalPlayerIndex = localPlayerIndex;
            PlayerCount = playerCount;
            InputDelay = inputDelay;
            RollbackWindow = rollbackWindow;
        }

        public static SessionConfig Lan2P(int localPlayerIndex, int inputDelay = DefaultInputDelay) =>
            new SessionConfig(localPlayerIndex, playerCount: 2, inputDelay: inputDelay);
    }
}
