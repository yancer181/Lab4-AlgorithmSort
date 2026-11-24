using System.Globalization;

namespace Lab4SortingApp.Algorithms;

/// <summary>
/// Внешняя сортировка таблицы по выбранному столбцу.
/// Реализованы: прямое слияние, естественное слияние, k-путевое слияние.
/// Таблица хранится как List<string[]> (каждая строка — запись).
/// </summary>
public static class ExternalSort
{
    // Модель «шага» для внешней сортировки
    public class ExternalStep
    {
        public string Description = "";
        public List<int> HighlightRowIndexes = new();
        public List<string[]> CurrentData = new();
    }

    // ------------------------------------------------------------------
    // 1. Прямое слияние (двухпутевое, с фиксированными сериями)
    // ------------------------------------------------------------------
    public static IEnumerable<ExternalStep> StraightMerge(
        List<string[]> rows, int keyCol, StepLogger log)
    {
        int n = rows.Count;
        var runs = new List<List<string[]>>();
        for (int i = 0; i < n; i += 1)
            runs.Add(new List<string[]> { rows[i] });

        log.LogInfo($"Прямое слияние: разбиваем на {runs.Count} серий по 1 записи.");
        yield return Snapshot(runs, "Начальное разбиение на одноэлементные серии.");

        while (runs.Count > 1)
        {
            var next = new List<List<string[]>>();
            for (int i = 0; i < runs.Count; i += 2)
            {
                if (i + 1 >= runs.Count) { next.Add(runs[i]); continue; }
                var merged = new List<string[]>();
                int p = 0, q = 0;
                var A = runs[i]; var B = runs[i + 1];
                while (p < A.Count && q < B.Count)
                {
                    log.LogCompare($"Сравниваем «{A[p][keyCol]}» и «{B[q][keyCol]}».");
                    int cmp = string.Compare(A[p][keyCol], B[q][keyCol], StringComparison.CurrentCulture);
                    if (cmp <= 0) { merged.Add(A[p]); p++; }
                    else { merged.Add(B[q]); q++; }
                    yield return Snapshot(new List<List<string[]>> { merged }, "Сливаем две серии…");
                }
                while (p < A.Count) merged.Add(A[p++]);
                while (q < B.Count) merged.Add(B[q++]);
                log.LogSwap($"Серии слиты, длина {merged.Count}.");
                next.Add(merged);
            }
            runs = next;
            log.LogInfo($"Проход слияния завершён, осталось серий: {runs.Count}.");
        }

        yield return Snapshot(runs, "Сортировка завершена.");
    }

    // ------------------------------------------------------------------
    // 2. Естественное слияние (серии — уже отсортированные фрагменты)
    // ------------------------------------------------------------------
    public static IEnumerable<ExternalStep> NaturalMerge(
        List<string[]> rows, int keyCol, StepLogger log)
    {
        var runs = new List<List<string[]>>();
        var current = new List<string[]> { rows[0] };
        for (int i = 1; i < rows.Count; i++)
        {
            if (string.Compare(rows[i - 1][keyCol], rows[i][keyCol],
                    StringComparison.CurrentCulture) <= 0)
                current.Add(rows[i]);
            else
            {
                runs.Add(current);
                current = new List<string[]> { rows[i] };
            }
        }
        runs.Add(current);

        log.LogInfo($"Естественное слияние: нашли {runs.Count} готовых серий.");
        yield return Snapshot(runs, "Выделили естественные серии.");

        while (runs.Count > 1)
        {
            var next = new List<List<string[]>>();
            for (int i = 0; i < runs.Count; i += 2)
            {
                if (i + 1 >= runs.Count) { next.Add(runs[i]); continue; }
                var A = runs[i]; var B = runs[i + 1];
                var merged = new List<string[]>();
                int p = 0, q = 0;
                while (p < A.Count && q < B.Count)
                {
                    log.LogCompare($"Сравниваем «{A[p][keyCol]}» и «{B[q][keyCol]}».");
                    if (string.Compare(A[p][keyCol], B[q][keyCol], StringComparison.CurrentCulture) <= 0)
                        merged.Add(A[p++]);
                    else merged.Add(B[q++]);
                    yield return Snapshot(new List<List<string[]>> { merged }, "Сливаем две серии…");
                }
                while (p < A.Count) merged.Add(A[p++]);
                while (q < B.Count) merged.Add(B[q++]);
                next.Add(merged);
            }
            runs = next;
        }
        yield return Snapshot(runs, "Сортировка завершена.");
    }

    // ------------------------------------------------------------------
    // 3. k-путевое слияние
    // ------------------------------------------------------------------
    public static IEnumerable<ExternalStep> KWayMerge(
        List<string[]> rows, int keyCol, int k, StepLogger log)
    {
        if (k < 2) k = 2;
        var runs = new List<List<string[]>>();
        int size = Math.Max(1, rows.Count / k);
        for (int i = 0; i < rows.Count; i += size)
            runs.Add(rows.GetRange(i, Math.Min(size, rows.Count - i)));

        foreach (var r in runs)
            r.Sort((x, y) => string.Compare(x[keyCol], y[keyCol], StringComparison.CurrentCulture));

        log.LogInfo($"k-путевое слияние: {runs.Count} серий, k={k}.");
        yield return Snapshot(runs, "Разбили на серии и отсортировали каждую.");

        while (runs.Count > 1)
        {
            var group = runs.Take(k).ToList();
            runs = runs.Skip(k).ToList();

            // Простое k-путевое слияние: выбираем минимум голов среди group
            var merged = new List<string[]>();
            var pos = new int[group.Count];
            while (true)
            {
                int best = -1;
                for (int g = 0; g < group.Count; g++)
                {
                    if (pos[g] >= group[g].Count) continue;
                    if (best == -1 ||
                        string.Compare(group[g][pos[g]][keyCol],
                                       group[best][pos[best]][keyCol],
                                       StringComparison.CurrentCulture) < 0)
                        best = g;
                }
                if (best == -1) break;
                merged.Add(group[best][pos[best]++]);
                yield return Snapshot(new List<List<string[]>> { merged }, "k-путевое слияние…");
            }
            runs.Insert(0, merged);
        }
        yield return Snapshot(runs, "Сортировка завершена.");
    }

    private static ExternalStep Snapshot(List<List<string[]>> runs, string desc)
    {
        var flat = runs.SelectMany(r => r).ToList();
        return new ExternalStep { Description = desc, CurrentData = flat };
    }
}