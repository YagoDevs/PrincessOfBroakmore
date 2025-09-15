using System;

public static class GameStats
{
    private static bool _isManic;
    public static event Action<bool> OnManicChanged;

    public static bool isManic
    {
        get => _isManic;
        set
        {
            if (_isManic != value)
            {
                _isManic = value;
                OnManicChanged?.Invoke(_isManic);
            }
        }
    }
}
