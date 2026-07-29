namespace Signals
{
    /// <summary>
    /// Доля опыта одного XP-орба физически долетела до прогресс-бара (либо
    /// TowerExperienceOrbSystem решила, что визуал невозможен, и сразу шлёт сумму как fallback,
    /// чтобы опыт не терялся). TowerExperienceService слушает этот сигнал и только тогда
    /// переводит сумму в PendingExperience — до этого момента опыт не засчитан.
    /// </summary>
    public class TowerExperienceOrbArrivedSignal
    {
        public int amount;
    }
}
