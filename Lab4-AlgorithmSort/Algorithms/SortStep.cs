namespace Lab4SortingApp.Algorithms;

/// <summary>Тип шага сортировки.</summary>
public enum StepType
{
    Compare,   // сравниваем два элемента
    Swap,      // меняем местами
    Set,       // записываем значение в ячейку (для сортировки вставками/слиянием)
    Done       // сортировка завершена
}

/// <summary>
/// Один шаг сортировки. Возвращается любым алгоритмом и используется
/// UI-потоком для визуализации и логирования.
/// </summary>
public class SortStep
{
    public StepType Type { get; init; }
    public int IndexA { get; init; }
    public int IndexB { get; init; }
    public int[] ArraySnapshot { get; init; } = Array.Empty<int>();
    public string Message { get; init; } = "";
    public string[]? StringSnapshot { get; init; } // для вкладки 3
}