namespace Lab4SortingApp.Algorithms;

public static class WordSort
{
    // -------- QuickSort (базовый/усовершенствованный) --------
    public static void QuickSort(string[] a, StepLogger log)
    {
        Qs(a, 0, a.Length - 1, log);
    }

    private static void Qs(string[] a, int lo, int hi, StepLogger log)
    {
        if (lo >= hi) return;
        int i = lo, j = hi;
        string pivot = a[(lo + hi) / 2];
        while (i <= j)
        {
            while (string.Compare(a[i], pivot, StringComparison.Ordinal) < 0)
            { log.LogCompare($"a[{i}]=\"{a[i]}\" < \"{pivot}\""); i++; }
            while (string.Compare(a[j], pivot, StringComparison.Ordinal) > 0)
            { log.LogCompare($"a[{j}]=\"{a[j]}\" > \"{pivot}\""); j--; }
            if (i <= j)
            {
                (a[i], a[j]) = (a[j], a[i]);
                log.LogSwap($"Меняем \"{a[j]}\" и \"{a[i]}\".");
                i++; j--;
            }
        }
        Qs(a, lo, j, log);
        Qs(a, i, hi, log);
    }

    // -------- MergeSort (альтернатива) --------
    public static void MergeSort(string[] a, StepLogger log)
    {
        if (a.Length < 2) return;
        var tmp = new string[a.Length];
        Ms(a, tmp, 0, a.Length - 1, log);
    }

    private static void Ms(string[] a, string[] tmp, int lo, int hi, StepLogger log)
    {
        if (lo >= hi) return;
        int mid = (lo + hi) / 2;
        Ms(a, tmp, lo, mid, log);
        Ms(a, tmp, mid + 1, hi, log);
        int i = lo, j = mid + 1, k = lo;
        while (i <= mid && j <= hi)
        {
            log.LogCompare($"\"{a[i]}\" vs \"{a[j]}\"");
            if (string.Compare(a[i], a[j], StringComparison.Ordinal) <= 0)
                tmp[k++] = a[i++];
            else tmp[k++] = a[j++];
        }
        while (i <= mid) tmp[k++] = a[i++];
        while (j <= hi) tmp[k++] = a[j++];
        for (int t = lo; t <= hi; t++) a[t] = tmp[t];
    }

    // -------- RadixSort (поразрядная, LSD) --------
    public static void RadixSort(string[] a, StepLogger log)
    {
        if (a.Length == 0) return;
        int maxLen = a.Max(s => s.Length);
        for (int pos = maxLen - 1; pos >= 0; pos--)
        {
            // Считаем по символам на позиции pos (0 — если короче)
            var buckets = new List<string>[256];
            for (int i = 0; i < 256; i++) buckets[i] = new List<string>();
            foreach (var s in a)
            {
                int c = pos < s.Length ? s[pos] : 0;
                buckets[c].Add(s);
                log.LogCompare($"Раскладываем \"{s}\" в корзину {c}.");
            }
            int idx = 0;
            foreach (var b in buckets)
                foreach (var s in b)
                    a[idx++] = s;
            log.LogSwap($"После разряда {pos}: массив пересобран.");
        }
    }

    // -------- ABC-сортировка (MSD, упрощённо) --------
    public static void AbcSort(string[] a, StepLogger log)
    {
        AbcSort(a, 0, a.Length - 1, 0, log);
    }

    private static void AbcSort(string[] a, int lo, int hi, int depth, StepLogger log)
    {
        if (hi <= lo) return;
        var buckets = new List<string>[257]; // 0 = конец строки, 1..256 — символы
        for (int i = 0; i < 257; i++) buckets[i] = new List<string>();
        for (int i = lo; i <= hi; i++)
        {
            var s = a[i];
            int c = depth < s.Length ? s[depth] + 1 : 0;
            buckets[c].Add(s);
            log.LogCompare($"\"{s}\": символ на глубине {depth} → корзина {c}.");
        }
        int idx = lo;
        foreach (var b in buckets)
        {
            if (b.Count == 0) continue;
            if (b.Count > 1)
                AbcSort(b.ToArray(), 0, b.Count - 1, depth + 1, log);
            foreach (var s in b) a[idx++] = s;
        }
        log.LogSwap($"Глубина {depth}: подмассив [{lo}..{hi}] пересобран.");
    }
}