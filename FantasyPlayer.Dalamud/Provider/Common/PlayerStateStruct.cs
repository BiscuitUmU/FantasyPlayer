namespace FantasyPlayer.Dalamud.Provider.Common
{
    public struct PlayerStateStruct
    {
        public string ServiceName;
        public bool RequiresLogin;
        public bool IsLoggedIn;
        public bool IsAuthenticating;

        public string RepeatState;
        public bool ShuffleState;
        public bool IsPlaying;
        public int ProgressMs;
        public TrackStruct CurrentlyPlaying;
    }
}