using System;

public static class GameEvents
{
    public static Action OnGameStarted;
    public static Action OnGameRestarted;
    public static Action OnGameOver;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    
    public static void PausedGame() { OnGamePaused?.Invoke(); }
    public static void ResumedGame() { OnGameResumed?.Invoke(); }
}