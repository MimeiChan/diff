using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DataTableDiff;

namespace DiffGridDemo
{
    public partial class MainForm : Form
    {
        private readonly System.Collections.Generic.IReadOnlyList<RowDiff> _diffs;

        public MainForm()
        {
            InitializeComponent();

            // テストデータ
            var oldTable = BuildOld();
            var newTable = BuildNew();

            gridOld.DataSource = oldTable;
            gridNew.DataSource = newTable;

            // 複合キー(BOM_SID, シーケンス) で差分計算
            _diffs = DataTableDiffUtil.Diff(
                oldTable, newTable,
                keyColumns: new[] { "BOM_SID", "シーケンス" },
                charLevel: false);

            this.Shown += (_, __) => ApplyHighlight(_diffs);
        }

        //― ハイライト処理 ―--------------------------------------------------
        private void ApplyHighlight(System.Collections.Generic.IReadOnlyList<RowDiff> diffs)
        {
            foreach (var d in diffs)
            {
                switch (d.ChangeType)
                {
                    case RowChangeType.Added:
                        PaintRow(gridNew, d.RowKey, Color.LightGreen);
                        break;
                    case RowChangeType.Removed:
                        PaintRow(gridOld, d.RowKey, Color.LightCoral);
                        break;
                    case RowChangeType.Modified:
                        foreach (var c in d.CellDiffs)
                        {
                            PaintCell(gridOld, d.RowKey, c.ColumnName, Color.Khaki);
                            PaintCell(gridNew, d.RowKey, c.ColumnName, Color.Khaki);
                        }
                        break;
                }
            }
        }

        private static void PaintRow(DataGridView grid, string key, Color color)
        {
            var row = grid.Rows.Cast<DataGridViewRow>()
                               .FirstOrDefault(r =>
                                   BuildCompositeKey(r).Equals(key, StringComparison.Ordinal));
            if (row != null) row.DefaultCellStyle.BackColor = color;
        }

        private static void PaintCell(DataGridView grid, string key, string col, Color color)
        {
            var row = grid.Rows.Cast<DataGridViewRow>()
                               .FirstOrDefault(r =>
                                   BuildCompositeKey(r).Equals(key, StringComparison.Ordinal));
            if (row != null) row.Cells[col].Style.BackColor = color;
        }

        private static string BuildCompositeKey(DataGridViewRow r)
            => $"{r.Cells["BOM_SID"].Value}||{r.Cells["シーケンス"].Value}";

        //― テストデータ ―----------------------------------------------------
        private static DataTable BuildOld()
        {
            var dt = NewSchema();
            dt.Rows.Add(5000, "01", 100, 50);
            dt.Rows.Add(5000, "02", 300, 70);  // Delete
            dt.Rows.Add(5000, "03", 500, 10);
            return dt;
        }

        private static DataTable BuildNew()
        {
            var dt = NewSchema();
            dt.Rows.Add(5000, "01", 100, 50);
            dt.Rows.Add(5000, "03", 700, 10);  // Modified
            dt.Rows.Add(5000, "04", 500, 10);  // Added
            return dt;
        }

        private static DataTable NewSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add("BOM_SID", typeof(int));
            dt.Columns.Add("シーケンス", typeof(string)); // "01" など文字列
            dt.Columns.Add("当期金額", typeof(decimal));
            dt.Columns.Add("出品数", typeof(int));
            return dt;
        }
    }
}
