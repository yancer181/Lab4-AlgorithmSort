using System.Diagnostics;
using System.Text.RegularExpressions;
using Lab4SortingApp.Algorithms;

namespace Lab4SortingApp.UI;

public class TabWordSort : TabPage
{
    private readonly TextBox txtInput = new() { Multiline = true, Dock = DockStyle.Top, Height = 120, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox chkLower = new() { Text = "Приводить к нижнему регистру", Dock = DockStyle.Top };
    private readonly ComboBox cmbBase = new() { Dock = DockStyle.Top };
    private readonly ComboBox cmbFast = new() { Dock = DockStyle.Top };
    private readonly Button btnLoad = new() { Text = "Загрузить .txt", Dock = DockStyle.Top };
    private readonly Button btnSort = new() { Text = "Сортировать (оба алгоритма)", Dock = DockStyle.Top };
    private readonly Button btnExperiment = new() { Text = "Эксперимент (100…10000)", Dock = DockStyle.Top };
    private readonly Button btnExport = new() { Text = "Экспорт эксперимента в CSV", Dock = DockStyle.Top };
    private readonly DataGridView gridFreq = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly DataGridView gridExp = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly RichTextBox logBox = new() { Dock = DockStyle.Bottom, Height = 140 };
    private readonly Panel chart = new() { Dock = DockStyle.Bottom, Height = 160, BackColor = Color.White };

    private string[] lastWords = Array.Empty<string>();
    private readonly List<(int n, double a, double b)> experimentResults = new();

    public TabWordSort()
    {
        Text = "3. Слова + частоты + эксперимент";

        cmbBase.Items.AddRange(new[] { "QuickSort", "MergeSort", "HeapSort (встроенный)" });
        cmbBase.SelectedIndex = 0;
        cmbFast.Items.AddRange(new[] { "RadixSort", "ABC-сортировка (MSD)" });
        cmbFast.SelectedIndex = 0;

        var top = new Panel { Dock = DockStyle.Top, Height = 320 };
        top.Controls.Add(btnExport);
        top.Controls.Add(btnExperiment);
        top.Controls.Add(btnSort);
        top.Controls.Add(btnLoad);
        top.Controls.Add(cmbFast);
        top.Controls.Add(cmbBase);
        top.Controls.Add(chkLower);
        top.Controls.Add(txtInput);
        top.Controls.Add(new Label { Text = "Второй алгоритм:", Dock = DockStyle.Top, Height = 18 });
        top.Controls.Add(new Label { Text = "Первый алгоритм:", Dock = DockStyle.Top, Height = 18 });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
        split.Panel1.Controls.Add(gridFreq);
        split.Panel2.Controls.Add(gridExp);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 300 };
        bottom.Controls.Add(chart);
        bottom.Controls.Add(logBox);

        Controls.Add(split);
        Controls.Add(bottom);
        Controls.Add(top);

        gridFreq.Columns.Add("w", "Слово");
        gridFreq.Columns.Add("c", "Количество");
        gridExp.Columns.Add("n", "N слов");
        gridExp.Columns.Add("a", "Время A (мс)");
        gridExp.Columns.Add("b", "Время B (мс)");

        btnLoad.Click += (_, _) => LoadText();
        btnSort.Click += (_, _) => DoSort();
        btnExperiment.Click += (_, _) => RunExperiment();
        btnExport.Click += (_, _) => ExportExperiment();
        chart.Paint += DrawChart;
    }

    private void LoadText()
    {
        using var dlg = new OpenFileDialog { Filter = "Текст (*.txt)|*.txt|Все (*.*)|*.*" };
        if (dlg.ShowDialog() == DialogResult.OK)
            txtInput.Text = File.ReadAllText(dlg.FileName);
    }

    private void DoSort()
    {
        if (!PrepareWords(out var words)) return;
        loggerReset();

        var w1 = (string[])words.Clone();
        var w2 = (string[])words.Clone();
        var log1 = new StepLogger();
        var log2 = new StepLogger();

        // первый алгоритм
        int idxBase = cmbBase.SelectedIndex;
        if (idxBase == 0) WordSort.QuickSort(w1, log1);
        else if (idxBase == 1) WordSort.MergeSort(w1, log1);
        else Array.Sort(w1, StringComparer.Ordinal);

        // второй алгоритм
        if (cmbFast.SelectedIndex == 0) WordSort.RadixSort(w2, log2);
        else WordSort.AbcSort(w2, log2);

        bool ok1 = w1.SequenceEqual(w2);
        lastWords = w1;
        logBox.Text =
            $"1-й алгоритм: {log1.Comparisons} сравнений, {log1.Swaps} перестановок\n" +
            $"2-й алгоритм: {log2.Comparisons} сравнений, {log2.Swaps} перестановок\n" +
            $"Результаты совпадают: {(ok1 ? "ДА" : "НЕТ")}\n";

        // частоты
        var freq = lastWords.GroupBy(w => w)
            .Select(g => (Word: g.Key, Count: g.Count()))
            .OrderByDescending(t => t.Count).ThenBy(t => t.Word);

        gridFreq.Rows.Clear();
        foreach (var f in freq) gridFreq.Rows.Add(f.Word, f.Count);
    }

    private void RunExperiment()
    {
        if (!PrepareWords(out var baseWords)) return;

        experimentResults.Clear();
        gridExp.Rows.Clear();
        var sizes = new[] { 100, 500, 1000, 2000, 5000, 10000 };
        var rnd = new Random(42);

        foreach (var n in sizes)
        {
            var sample = new string[n];
            for (int i = 0; i < n; i++) sample[i] = baseWords[rnd.Next(baseWords.Length)];

            double tA = Measure(() => QuickCloneSort(sample, cmbBase.SelectedIndex));
            double tB = Measure(() => RadixCloneSort(sample, cmbFast.SelectedIndex));
            experimentResults.Add((n, tA, tB));
            gridExp.Rows.Add(n, tA.ToString("F2"), tB.ToString("F2"));
        }
        chart.Invalidate();
    }

    private static double Measure(Action action, int repeats = 3)
    {
        double best = double.MaxValue;
        for (int i = 0; i < repeats; i++)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
        }
        return best;
    }

    private static void QuickCloneSort(string[] src, int algo)
    {
        var a = (string[])src.Clone();
        var log = new StepLogger();
        if (algo == 0) WordSort.QuickSort(a, log);
        else if (algo == 1) WordSort.MergeSort(a, log);
        else Array.Sort(a, StringComparer.Ordinal);
    }

    private static void RadixCloneSort(string[] src, int algo)
    {
        var a = (string[])src.Clone();
        var log = new StepLogger();
        if (algo == 0) WordSort.RadixSort(a, log);
        else WordSort.AbcSort(a, log);
    }

    private bool PrepareWords(out string[] words)
    {
        var text = txtInput.Text;
        var matches = Regex.Matches(text, @"\b\w+\b");
        var list = new List<string>();
        foreach (Match m in matches)
            list.Add(chkLower.Checked ? m.Value.ToLowerInvariant() : m.Value);
        words = list.ToArray();
        if (words.Length == 0) { logBox.Text = "Нет слов для сортировки."; return false; }
        return true;
    }

    private void ExportExperiment()
    {
        using var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        using var w = new StreamWriter(dlg.FileName);
        w.WriteLine("N,TimeA_ms,TimeB_ms");
        foreach (var r in experimentResults)
            w.WriteLine($"{r.n},{r.a:F2},{r.b:F2}");
    }

    private void DrawChart(object? sender, PaintEventArgs e)
    {
        if (experimentResults.Count < 2) return;
        var g = e.Graphics;
        int w = chart.Width, h = chart.Height;
        int pad = 30;
        double maxY = experimentResults.Max(r => Math.Max(r.a, r.b)) * 1.1;
        double maxX = experimentResults.Max(r => r.n) * 1.05;

        var penA = new Pen(Color.SteelBlue, 2);
        var penB = new Pen(Color.OrangeRed, 2);

        Point P(int n, double y) => new(
            pad + (int)(n / maxX * (w - 2 * pad)),
            h - pad - (int)(y / maxY * (h - 2 * pad)));

        for (int i = 0; i < experimentResults.Count - 1; i++)
        {
            var r1 = experimentResults[i];
            var r2 = experimentResults[i + 1];
            g.DrawLine(penA, P(r1.n, r1.a), P(r2.n, r2.a));
            g.DrawLine(penB, P(r1.n, r1.b), P(r2.n, r2.b));
        }
        g.DrawString("— Алгоритм A", SystemFonts.DefaultFont, Brushes.SteelBlue, pad, 4);
        g.DrawString("— Алгоритм B", SystemFonts.DefaultFont, Brushes.OrangeRed, pad + 90, 4);
    }

    private void loggerReset() { }
}