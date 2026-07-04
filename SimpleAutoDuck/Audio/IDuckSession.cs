namespace SimpleAutoDuck.Audio
{
    public interface IDuckSession
    {
        string ProcessName { get; }
        bool IsMainApp { get; }
        bool IsExcluded { get; }
        double GetPeakLevel();
        double GetVolume();
        void SetVolume(double v);
        void SnapshotUserVolume();
        double GetUserVolume();
    }
}