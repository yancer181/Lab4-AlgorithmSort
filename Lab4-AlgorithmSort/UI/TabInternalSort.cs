using Lab4SortingApp.Algorithms;

namespace Lab4SortingApp.UI;

public class TabInternalSort : TabPage
{
    private readonly TextBox txtArray = new() { Multiline = true, Dock = DockStyle.Top, Height = 50 };
    private readonly ComboBox cmbAlgo = new() { Dock = DockStyle.Top };
    private readonly NumericUpDown numDelay = new() { Minimum = 0, Maximum = 2000, Value = 200, Dock = DockStyle.Top };
    private readonly NumericUpDown numLen = new() { Minimum = 5, Maximum = 200, Value = 30, Dock = DockStyle.Top };
    private readonly Button btnRandom = new() { Text = "Сгенерировать массив", Dock = DockStyle.Top };
    private readonly Button btnStart = new() { Text = "Старт", Dock = DockStyle.Left, Width = 80 };
    private readonly Button btnPause = new() { Text = "Пауза", Dock = DockStyle.Left, Width = 80 };
    private readonly Button btnStop = new() { Text = "Стоп", Dock = DockStyle.Left, Width = 80 };
    private readonly Button btnSaveLog = new() { Text = "Сохранить лог", Dock = DockStyle.Left, Width = 120 };
    private readonly Panel canvas = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    private readonly RichTextBox logBox = new() { Dock = DockStyle.Bottom, Height = 180 };
    private readonly Label lblCounters = new() { Dock = DockStyle.Bottom, Height = 24 };

    private int[] currentArray = Array.Empty<int>();
    private int highlightA = -1, highlightB = -1;
    private CancellationTokenSource? cts;
    private readonly ManualResetEventSlim pauseEvent = new(true);
    private readonly StepLogger logger = new();

    public TabInternalSort()
    {
        Text = "1. Внутренняя сортировка";

        var top = new Panel { Dock = DockStyle.Top, Height = 260 };
        top.Controls.Add(numDelay);
        top.Controls.Add(numLen);
        top.Controls.Add(cmbAlgo);
        top.Controls.Add(txtArray);
        top.Controls.Add(btnRandom);
        top.Controls.Add(new Label { Text = "Задержка (мс):", Dock = DockStyle.Top, Height = 18 });
        top.Controls.Add(new Label { Text = "Длина массива:", Dock = DockStyle.Top, Height = 18 });

        var buttons = new Panel { Dock = DockStyle.Top, Height = 40 };
        buttons.Controls.Add(btnSaveLog);
        buttons.Controls.Add(btnStop);
        buttons.Controls.Add(btnPause);
        buttons.Controls.Add(btnStart);

        Controls.Add(canvas);
        Controls.Add(logBox);
        Controls.Add(lblCounters);
        Controls.Add(buttons);
        Controls.Add(top);

        cmbAlgo.Items.AddRange(new[]
        {
            "Сортировка выбором (SelectSort)",
            "Сортировка пузырьком (BubbleSort)",
            "Сортировка вставками (InsertSort)",
            "Сортировка Шелла (ShellSort)",
            "Пирамидальная сортировка (HeapSort)",
            "Быстрая сортировка (QuickSort)"
        });
        cmbAlgo.SelectedIndex = 0;

        btnRandom.Click += (_, _) => GenerateRandom();
        btnStart.Click += async (_, _) => await StartSortAsync();
        btnPause.Click += (_, _) => TogglePause();
        btnStop.Click += (_, _) => StopSort();
        btnSaveLog.Click += (_, _) => SaveLog();
        canvas.Paint += DrawCanvas;

        GenerateRandom();
    }

    private void GenerateRandom()
    {
        var rnd = new Random();
        currentArray = Enumerable.Range(0, (int)numLen.Value)
            .Select(_ => rnd.Next(1, 100)).ToArray();
        txtArray.Text = string.Join(", ", currentArray);
        highlightA = highlightB = -1;
        canvas.Invalidate();
    }

    private void TogglePause()
    {
        if (pauseEvent.IsSet) { pauseEvent.Reset(); btnPause.Text = "Продолжить"; }
        else { pauseEvent.Set(); btnPause.Text = "Пауза"; }
    }

    private void StopSort() => cts?.Cancel();

    private async Task StartSortAsync()
    {
        if (!TryParseArray(out var arr)) return;
        currentArray = arr;
        logger.Reset();
        logBox.Clear();
        lblCounters.Text = "Счётчики: 0 сравнений, 0 перестановок";

        cts = new CancellationTokenSource();
        pauseEvent.Set();
        var token = cts.Token;
        int delay = (int)numDelay.Value;
        var algo = cmbAlgo.SelectedIndex;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var generator = algo switch
        {
            0 => InternalSort.SelectSort(arr, logger),
            1 => InternalSort.BubbleSort(arr, logger),
            2 => InternalSort.InsertSort(arr, logger),
            3 => InternalSort.ShellSort(arr, logger),
            4 => InternalSort.HeapSort(arr, logger),
            _ => InternalSort.QuickSort(arr, logger)
        };

        try
        {
            foreach (var step in generator)
            {
                if (token.IsCancellationRequested) break;
                pauseEvent.Wait(token);
                currentArray = step.ArraySnapshot;
                highlightA = step.IndexA;
                highlightB = step.IndexB;
                logBox.AppendText(step.Message + Environment.NewLine);
                lblCounters.Text = $"Счётчики: {logger.Comparisons} сравнений, {logger.Swaps} перестановок";
                canvas.Invalidate();
                logBox.ScrollToCaret();
                if (delay > 0) await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            stopwatch.Stop();
            lblCounters.Text += $" | Время: {stopwatch.ElapsedMilliseconds} мс";
            highlightA = highlightB = -1;
            canvas.Invalidate();
        }
    }

    private bool TryParseArray(out int[] arr)
    {
        var parts = txtArray.Text.Split(new[] { ',', ' ', ';', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        var list = new List<int>();
        foreach (var p in parts)
            if (int.TryParse(p, out var v)) list.Add(v);
        arr = list.ToArray();
        return arr.Length > 0;
    }

    private void DrawCanvas(object? sender, PaintEventArgs e)
    {
        if (currentArray.Length == 0) return;
        var g = e.Graphics;
        int w = canvas.Width / currentArray.Length;
        int max = Math.Max(1, currentArray.Max());
        int h = canvas.Height;

        for (int i = 0; i < currentArray.Length; i++)
        {
            int barH = (int)((double)currentArray[i] / max * (h - 20));
            var brush = i == highlightA || i == highlightB
                ? Brushes.Red
                : Brushes.SteelBlue;
            g.FillRectangle(brush, i * w + 2, h - barH - 2, w - 4, barH);
            g.DrawString(currentArray[i].ToString(), SystemFonts.DefaultFont,
                Brushes.Black, i * w + 2, h - barH - 18);
        }
    }

    private void SaveLog()
    {
        using var dlg = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt" };
        if (dlg.ShowDialog() == DialogResult.OK)
            File.WriteAllText(dlg.FileName, logger.Log.ToString());
    }
}