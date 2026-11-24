using Lab4SortingApp.UI;

namespace Lab4SortingApp;

public class MainForm : Form
{
    public MainForm()
    {
        Text = "Лабораторная работа №4 — Сортировки (C# / WinForms)";
        Width = 1200; Height = 800;
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(new TabInternalSort());
        tabs.TabPages.Add(new TabExternalSort());
        tabs.TabPages.Add(new TabWordSort());
        Controls.Add(tabs);
    }
}