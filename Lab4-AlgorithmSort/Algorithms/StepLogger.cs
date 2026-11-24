using System.Text;

namespace Lab4SortingApp.Algorithms;

/// <summary>Счётчики и текстовый лог.</summary>
public class StepLogger
{
    public int Comparisons { get; private set; }
    public int Swaps { get; private set; }
    public StringBuilder Log { get; } = new();

    public void LogCompare(string what)
    {
        Comparisons++;
        Log.AppendLine($"[Сравнение #{Comparisons}] {what}");
    }

    public void LogSwap(string what)
    {
        Swaps++;
        Log.AppendLine($"[Перестановка #{Swaps}] {what}");
    }

    public void LogInfo(string what) => Log.AppendLine(what);

    public void Reset()
    {
        Comparisons = 0;
        Swaps = 0;
        Log.Clear();
    }
}