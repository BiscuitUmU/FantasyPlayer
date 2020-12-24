using FantasyPlayer.Dalamud.Provider.Common;

namespace FantasyPlayer.Dalamud.Provider
{
    public interface IPlayerProvider
    {
        public PlayerStateStruct PlayerState { get; set; }

        public void Initialize(Plugin plugin);
        public void Update();
        public void Dispose();

        public void StartAuth();
        public void RetryAuth();

        public void SwapRepeatState();
        public void SetPauseOrPlay(bool play);
        public void SetSkip(bool forward);
        public void SetShuffle(bool value);
        public void SetVolume(int volume);
    }
}