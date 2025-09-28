namespace AuralRehab.GamePlay {
    /// <summary>호스트에서 게임 일시정지/재개를 제어하기 위한 최소 인터페이스</summary>
    public interface IPausableGame {
        void Pause();
        void Resume();
        bool IsPaused { get; }
    }
}