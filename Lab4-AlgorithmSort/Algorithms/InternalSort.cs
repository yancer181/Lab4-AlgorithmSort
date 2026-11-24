namespace Lab4SortingApp.Algorithms;

/// <summary>
/// Все алгоритмы внутренней сортировки. Каждый — генератор шагов.
/// Алгоритмы НЕ меняют исходный массив напрямую в UI — они возвращают
/// снимки (ArraySnapshot) и индексы, которые нужно подсветить.
/// </summary>
public static class InternalSort
{
    // ------------------------------------------------------------------
    // Сортировка выбором — квадратичная
    // ------------------------------------------------------------------
    public static IEnumerable<SortStep> SelectSort(int[] src, StepLogger log)
    {
        var a = (int[])src.Clone();
        int n = a.Length;

        for (int i = 0; i < n - 1; i++)
        {
            int minIdx = i;
            for (int j = i + 1; j < n; j++)
            {
                log.LogCompare(
                    $"Сравниваем a[{minIdx}]={a[minIdx]} и a[{j}]={a[j]}: " +
                    (a[j] < a[minIdx]
                        ? $"{a[j]} меньше, значит новый минимум — {a[j]}. "
                        : $"{a[j]} не меньше, оставляем минимум {a[minIdx]}. "));
                yield return new SortStep
                {
                    Type = StepType.Compare, IndexA = minIdx, IndexB = j,
                    ArraySnapshot = (int[])a.Clone(),
                    Message = $"Ищем минимум среди оставшихся: a[{minIdx}]={a[minIdx]}, a[{j}]={a[j]}."
                };
                if (a[j] < a[minIdx]) minIdx = j;
            }

            if (minIdx != i)
            {
                log.LogSwap($"Меняем a[{i}]={a[i]} и a[{minIdx}]={a[minIdx]} местами — ставим минимум в начало.");
                yield return new SortStep
                {
                    Type = StepType.Swap, IndexA = i, IndexB = minIdx,
                    ArraySnapshot = (int[])a.Clone(),
                    Message = $"Меняем местами a[{i}]={a[i]} и a[{minIdx}]={a[minIdx]}."
                };
                (a[i], a[minIdx]) = (a[minIdx], a[i]);
            }
        }

        yield return Done(a);
    }

    // ------------------------------------------------------------------
    // Сортировка пузырьком (с флагом улучшения)
    // ------------------------------------------------------------------
    public static IEnumerable<SortStep> BubbleSort(int[] src, StepLogger log)
    {
        var a = (int[])src.Clone();
        int n = a.Length;
        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - 1 - i; j++)
            {
                log.LogCompare(
                    $"Сравниваем a[{j}]={a[j]} и a[{j + 1}]={a[j + 1]}: " +
                    (a[j] > a[j + 1]
                        ? "левый больше правого — меняем местами."
                        : "порядок правильный, идём дальше."));
                yield return new SortStep
                {
                    Type = StepType.Compare, IndexA = j, IndexB = j + 1,
                    ArraySnapshot = (int[])a.Clone(),
                    Message = $"Сравниваем соседей a[{j}]={a[j]} и a[{j + 1}]={a[j + 1]}."
                };
                if (a[j] > a[j + 1])
                {
                    (a[j], a[j + 1]) = (a[j + 1], a[j]);
                    swapped = true;
                    log.LogSwap($"Меняем местами a[{j}] и a[{j + 1}], чтобы меньший был слева.");
                    yield return new SortStep
                    {
                        Type = StepType.Swap, IndexA = j, IndexB = j + 1,
                        ArraySnapshot = (int[])a.Clone(),
                        Message = $"Меняем местами a[{j}]={a[j]} и a[{j + 1}]={a[j + 1]}."
                    };
                }
            }
            if (!swapped)
            {
                log.LogInfo("За проход не было обменов — массив уже отсортирован, выходим.");
                break;
            }
        }
        yield return Done(a);
    }

    // ------------------------------------------------------------------
    // Сортировка простыми вставками — квадратичная
    // ------------------------------------------------------------------
    public static IEnumerable<SortStep> InsertSort(int[] src, StepLogger log)
    {
        var a = (int[])src.Clone();
        int n = a.Length;
        for (int i = 1; i < n; i++)
        {
            int key = a[i];
            int j = i - 1;
            log.LogInfo($"Берём a[{i}]={key} и вставляем в уже отсортированную часть слева.");
            while (j >= 0)
            {
                log.LogCompare($"Сравниваем ключ {key} с a[{j}]={a[j]}.");
                yield return new SortStep
                {
                    Type = StepType.Compare, IndexA = j, IndexB = i,
                    ArraySnapshot = (int[])a.Clone(),
                    Message = $"Сравниваем ключ {key} с a[{j}]={a[j]}."
                };
                if (a[j] <= key) break;
                a[j + 1] = a[j];
                log.LogSwap($"Сдвигаем a[{j}]={a[j]} вправо, освобождаем место для {key}.");
                yield return new SortStep
                {
                    Type = StepType.Set, IndexA = j + 1,
                    ArraySnapshot = (int[])a.Clone(),
                    Message = $"Сдвигаем {a[j]} вправо."
                };
                j--;
            }
            if (j + 1 != i)
            {
                a[j + 1] = key;
                yield return new SortStep
                {
                    Type = StepType.Set, IndexA = j + 1,
                    ArraySnapshot = (int[])a.Clone(),
                    Message = $"Ставим ключ {key} на позицию {j + 1}."
                };
            }
        }
        yield return Done(a);
    }

    // ------------------------------------------------------------------
    // Сортировка Шелла — субквадратичная
    // ------------------------------------------------------------------
    public static IEnumerable<SortStep> ShellSort(int[] src, StepLogger log)
    {
        var a = (int[])src.Clone();
        int n = a.Length;
        for (int gap = n / 2; gap > 0; gap /= 2)
        {
            log.LogInfo($"Шаг (gap) = {gap}. Сравниваем элементы на расстоянии {gap}.");
            for (int i = gap; i < n; i++)
            {
                int key = a[i];
                int j = i;
                while (j >= gap)
                {
                    log.LogCompare($"Сравниваем ключ {key} с a[{j - gap}]={a[j - gap]} (шаг {gap}).");
                    yield return new SortStep
                    {
                        Type = StepType.Compare, IndexA = j - gap, IndexB = j,
                        ArraySnapshot = (int[])a.Clone(),
                        Message = $"Шаг {gap}: сравниваем {key} и a[{j - gap}]={a[j - gap]}."
                    };
                    if (a[j - gap] <= key) break;
                    a[j] = a[j - gap];
                    log.LogSwap($"Сдвигаем a[{j - gap}]={a[j - gap]} на позицию {j}.");
                    yield return new SortStep
                    {
                        Type = StepType.Set, IndexA = j,
                        ArraySnapshot = (int[])a.Clone(),
                        Message = $"Сдвигаем {a[j]} вправо на {gap}."
                    };
                    j -= gap;
                }
                a[j] = key;
            }
        }
        yield return Done(a);
    }

    // ------------------------------------------------------------------
    // Пирамидальная сортировка (HeapSort) — усовершенствованная
    // ------------------------------------------------------------------
    public static IEnumerable<SortStep> HeapSort(int[] src, StepLogger log)
    {
        var a = (int[])src.Clone();
        int n = a.Length;

        for (int i = n / 2 - 1; i >= 0; i--)
            foreach (var s in SiftDown(a, i, n, log)) yield return s;

        for (int end = n - 1; end > 0; end--)
        {
            log.LogSwap($"Меняем корень кучи a[0]={a[0]} с последним a[{end}]={a[end]} — максимум встаёт в конец.");
            yield return new SortStep
            {
                Type = StepType.Swap, IndexA = 0, IndexB = end,
                ArraySnapshot = (int[])a.Clone(),
                Message = $"Максимум {a[0]} уходит в конец (позиция {end})."
            };
            (a[0], a[end]) = (a[end], a[0]);
            foreach (var s in SiftDown(a, 0, end, log)) yield return s;
        }
        yield return Done(a);
    }

    private static IEnumerable<SortStep> SiftDown(int[] a, int root, int n, StepLogger log)
    {
        while (true)
        {
            int child = 2 * root + 1;
            if (child >= n) yield break;
            if (child + 1 < n)
            {
                log.LogCompare($"Сравниваем детей корня: a[{child}]={a[child]} и a[{child + 1}]={a[child + 1]}.");
                yield return new SortStep
                {
                    Type = StepType.Compare, IndexA = child, IndexB = child + 1,
                    ArraySnapshot = (int[])a.Clone(),
                    Message = $"Ищем большего ребёнка: {a[child]} vs {a[child + 1]}."
                };
                if (a[child + 1] > a[child]) child++;
            }
            log.LogCompare($"Сравниваем корень a[{root}]={a[root]} с ребёнком a[{child}]={a[child]}.");
            yield return new SortStep
            {
                Type = StepType.Compare, IndexA = root, IndexB = child,
                ArraySnapshot = (int[])a.Clone(),
                Message = $"Сравниваем корень {a[root]} с ребёнком {a[child]}."
            };
            if (a[root] >= a[child]) yield break;

            (a[root], a[child]) = (a[child], a[root]);
            log.LogSwap($"Меняем местами корень и ребёнка: {a[child]} ↔ {a[root]}.");
            yield return new SortStep
            {
                Type = StepType.Swap, IndexA = root, IndexB = child,
                ArraySnapshot = (int[])a.Clone(),
                Message = "Меняем корень и ребёнка."
            };
            root = child;
        }
    }

    // ------------------------------------------------------------------
    // Быстрая сортировка Хоара — усовершенствованная
    // ------------------------------------------------------------------
    public static IEnumerable<SortStep> QuickSort(int[] src, StepLogger log)
    {
        var a = (int[])src.Clone();
        var stack = new Stack<(int lo, int hi)>();
        stack.Push((0, a.Length - 1));

        while (stack.Count > 0)
        {
            var (lo, hi) = stack.Pop();
            if (lo >= hi) continue;

            int pivot = a[(lo + hi) / 2];
            log.LogInfo($"Опорный элемент pivot = {pivot}.");
            yield return new SortStep
            {
                Type = StepType.Compare, IndexA = (lo + hi) / 2, IndexB = (lo + hi) / 2,
                ArraySnapshot = (int[])a.Clone(),
                Message = $"Выбрали опорный элемент {pivot}."
            };

            int i = lo, j = hi;
            while (i <= j)
            {
                while (a[i] < pivot)
                {
                    log.LogCompare($"a[{i}]={a[i]} < pivot={pivot} — идём вправо.");
                    yield return new SortStep
                    {
                        Type = StepType.Compare, IndexA = i, IndexB = (lo + hi) / 2,
                        ArraySnapshot = (int[])a.Clone(),
                        Message = $"{a[i]} < {pivot} — двигаем левый указатель."
                    };
                    i++;
                }
                while (a[j] > pivot)
                {
                    log.LogCompare($"a[{j}]={a[j]} > pivot={pivot} — идём влево.");
                    yield return new SortStep
                    {
                        Type = StepType.Compare, IndexA = j, IndexB = (lo + hi) / 2,
                        ArraySnapshot = (int[])a.Clone(),
                        Message = $"{a[j]} > {pivot} — двигаем правый указатель."
                    };
                    j--;
                }
                if (i <= j)
                {
                    (a[i], a[j]) = (a[j], a[i]);
                    log.LogSwap($"Меняем a[{i}]={a[j]} и a[{j}]={a[i]}.");
                    yield return new SortStep
                    {
                        Type = StepType.Swap, IndexA = i, IndexB = j,
                        ArraySnapshot = (int[])a.Clone(),
                        Message = $"Меняем местами a[{i}] и a[{j}]."
                    };
                    i++; j--;
                }
            }
            if (lo < j) stack.Push((lo, j));
            if (i < hi) stack.Push((i, hi));
        }
        yield return Done(a);
    }

    private static SortStep Done(int[] a) => new()
    {
        Type = StepType.Done,
        ArraySnapshot = (int[])a.Clone(),
        Message = "Сортировка завершена!"
    };
}