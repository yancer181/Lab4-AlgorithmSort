using Lab4SortingApp.Algorithms;

namespace Lab4SortingApp.UI;

public class TabExternalSort : TabPage
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly ComboBox cmbSeparator = new() { Dock = DockStyle.Top };
    private readonly ComboBox cmbKeyCol = new() { Dock = DockStyle.Top };
    private readonly ComboBox cmbMethod = new() { Dock = DockStyle.Top };
    private readonly NumericUpDown numK = new() { Minimum = 2, Maximum = 16, Value = 4, Dock = DockStyle.Top };
    private readonly NumericUpDown numDelay = new() { Minimum = 0, Maximum = 2000, Value = 200, Dock = DockStyle.Top };
    private readonly Button btnOpen = new() { Text = "Открыть файл", Dock = DockStyle.Top };
    private readonly Button btnSave = new() { Text = "Сохранить результат", Dock = DockStyle.Top };
    private readonly Button btnStart = new() { Text = "Старт", Dock = DockStyle.Left, Width = 80 };
    private readonly Button btnPause = new() { Text = "Пауза", Dock = DockStyle.Left, Width = 80 };
    private readonly Button btnStop = new() { Text = "Стоп", Dock = DockStyle.Left, Width = 80 };
    private readonly RichTextBox logBox = new() { Dock = DockStyle.Bottom, Height = 180 };

    private List<string[]> rows = new();
    private string[] headers = Array.Empty<string>();
    private CancellationTokenSource? cts;
    private readonly ManualResetEventSlim pauseEvent = new(true);
    private readonly StepLogger logger = new();

    public TabExternalSort()
    {
        Text = "2. Внешняя сортировка";

        cmbSeparator.Items.AddRange(new[] { ";", ",", "\t" });
        cmbSeparator.SelectedIndex = 0;

        cmbMethod.Items.AddRange(new[]
        {
            "Прямое слияние",
            "Естественное слияние",
            "k-путевое слияние"
        });
        cmbMethod.SelectedIndex = 0;

        var top = new Panel { Dock = DockStyle.Top, Height = 260 };
        top.Controls.Add(numDelay);
        top.Controls.Add(numK);
        top.Controls.Add(cmbMethod);
        top.Controls.Add(cmbKeyCol);
        top.Controls.Add(cmbSeparator);
        top.Controls.Add(btnSave);
        top.Controls.Add(btnOpen);
        top.Controls.Add(new Label { Text = "Задержка шага (мс):", Dock = DockStyle.Top, Height = 18 });
        top.Controls.Add(new Label { Text = "k (для k-путевого):", Dock = DockStyle.Top, Height = 18 });
        top.Controls.Add(new Label { Text = "Ключевой столбец:", Dock = DockStyle.Top, Height = 18 });
        top.Controls.Add(new Label { Text = "Разделитель:", Dock = DockStyle.Top, Height = 18 });

        var buttons = new Panel { Dock = DockStyle.Top, Height = 40 };
        buttons.Controls.Add(btnStop);
        buttons.Controls.Add(btnPause);
        buttons.Controls.Add(btnStart);

        Controls.Add(grid);
        Controls.Add(logBox);
        Controls.Add(buttons);
        Controls.Add(top);

        btnOpen.Click += (_, _) => OpenFile();
        btnSave.Click += (_, _) => SaveResult();
        btnStart.Click += async (_, _) => await StartSortAsync();
        btnPause.Click += (_, _) => TogglePause();
        btnStop.Click += (_, _) => cts?.Cancel();
    }

    private void OpenFile()
    {
        using var dlg = new OpenFileDialog
        { Filter = "CSV/TXT (*.csv;*.txt)|*.csv;*.txt|Все файлы (*.*)|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var sep = (string)cmbSeparator.SelectedItem!;
        var text = File.ReadAllLines(dlg.FileName);
        if (text.Length == 0) return;

        headers = text[0].Split(sep);
        rows = text.Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(sep))
            .ToList();

        grid.Columns.Clear();
        grid.Rows.Clear();
        foreach (var h in headers) grid.Columns.Add("", h);

        foreach (var r in rows)
        {
            // выравниваем количество полей по заголовкам
            var padded = new string[headers.Length];
            for (int i = 0; i < headers.Length; i++) padded[i] = i < r.Length ? r[i] : "";
            grid.Rows.Add(padded);
        }

        cmbKeyCol.Items.Clear();
        cmbKeyCol.Items.AddRange(headers);
        if (cmbKeyCol.Items.Count > 0) cmbKeyCol.SelectedIndex = 0;

        logBox.Clear();
        logger.Reset();
    }

    private void SaveResult()
    {
        using var dlg = new SaveFileDialog
        { Filter = "CSV (*.csv)|*.csv|Текст (*.txt)|*.txt" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var sep = (string)cmbSeparator.SelectedItem!;
        using var w = new StreamWriter(dlg.FileName);
        w.WriteLine(string.Join(sep, headers));
        foreach (DataGridViewRow r in grid.Rows)
        {
            var cells = r.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "");
            w.WriteLine(string.Join(sep, cells));
        }
    }

    private void TogglePause()
    {
        if (pauseEvent.IsSet) { pauseEvent.Reset(); btnPause.Text = "Продолжить"; }
        else { pauseEvent.Set(); btnPause.Text = "Пауза"; }
    }

    private async Task StartSortAsync()
    {
        if (rows.Count == 0 || cmbKeyCol.SelectedIndex < 0) return;
        logger.Reset();
        logBox.Clear();

        int keyCol = cmbKeyCol.SelectedIndex;
        int delay = (int)numDelay.Value;
        int k = (int)numK.Value;
        int method = cmbMethod.SelectedIndex;
        cts = new CancellationTokenSource();
        pauseEvent.Set();
        var token = cts.Token;

        IEnumerable<ExternalSort.ExternalStep> gen = method switch
        {
            0 => ExternalSort.StraightMerge(rows, keyCol, logger),
            1 => ExternalSort.NaturalMerge(rows, keyCol, logger),
            _ => ExternalSort.KWayMerge(rows, keyCol, k, logger)
        };

        try
        {
            foreach (var step in gen)
            {
                if (token.IsCancellationRequested) break;
                pauseEvent.Wait(token);
                grid.Rows.Clear();
                foreach (var r in step.CurrentData)
                {
                    var padded = new string[headers.Length];
                    for (int i = 0; i < headers.Length; i++)
                        padded[i] = i < r.Length ? r[i] : "";
                    grid.Rows.Add(padded);
                }
                logBox.AppendText(step.Description + Environment.NewLine);
                logBox.ScrollToCaret();
                if (delay > 0) await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            logBox.AppendText(
                $"Готово. Сравнений: {logger.Comparisons}, перестановок/слияний: {logger.Swaps}\n");
        }
    }
}