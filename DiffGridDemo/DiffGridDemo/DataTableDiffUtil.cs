/**********************************************************************
* DataTableDiffUtil.cs
* -------------------------------------------------------------
* ２つの DataTable を比べて
*   ① 行が追加された／削除された
*   ② 既存行のセル値が変わった
* を調べるための超シンプルクラス。
*
* ■使い方
*   var diffs = SimpleDataTableDiff.Diff(oldTable, newTable,
*                                        new[] {"BOM_SID", "シーケンス"},
*                                        charLevel: false);
*********************************************************************/

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using DiffPlex.DiffBuilder;          // ※文字レベル差分で利用（初心者は無視しても可）
using DiffPlex.DiffBuilder.Model;

namespace DataTableDiff
{
    /*==============================================================
     * 1. 列挙型とデータ用クラス
     *============================================================*/

    /// <summary>行がどう変わったかを表す。</summary>
    public enum RowChangeType { Added, Removed, Modified }

    /// <summary>１セル分の変化をまとめるクラス。</summary>
    public sealed class CellDiff
    {
        public string ColumnName;            // 列名
        public string OldValue;              // 旧値
        public string NewValue;              // 新値
        public DiffPaneModel CharDiff;       // 文字レベルの差分（使いたい人向け）

        public CellDiff(string column, string oldVal, string newVal, DiffPaneModel charDiff = null)
        {
            ColumnName = column;
            OldValue = oldVal;
            NewValue = newVal;
            CharDiff = charDiff;
        }
    }

    /// <summary>１行分の変化をまとめるクラス。</summary>
    public sealed class RowDiff
    {
        public RowChangeType ChangeType;       // 追加 / 削除 / 変更
        public string RowKey;                  // 複合キー文字列
        public List<CellDiff> CellDiffs;       // 変更セル一覧（追加・削除なら null）

        public RowDiff(RowChangeType type, string key, List<CellDiff> cells = null)
        {
            ChangeType = type;
            RowKey = key;
            CellDiffs = cells;
        }
    }

    /*==============================================================
     * 2. メインユーティリティクラス
     *============================================================*/
    public static class DataTableDiffUtil
    {
        /// <summary>
        /// 差分を取るメソッド（初心者向けバージョン）。
        /// 
        /// keyColumns : 行を一意に決める列名配列（複合キー可）
        /// charLevel  : true で文字単位差分を取りたいときだけオンにする
        /// </summary>
        public static List<RowDiff> Diff(
            DataTable oldTable,
            DataTable newTable,
            string[] keyColumns,
            bool charLevel = false,
            bool ignoreWhiteSpace = true)
        {
            //--------------------------------------------------
            // 1) 旧・新テーブルを辞書に格納
            //--------------------------------------------------
            //    辞書キー = 複合キー文字列
            //    値      = DataRow
            //--------------------------------------------------
            var oldDict = BuildLookup(oldTable, keyColumns);
            var newDict = BuildLookup(newTable, keyColumns);

            // 返却用リスト
            var result = new List<RowDiff>();

            //--------------------------------------------------
            // 2) 旧テーブル基準で Removed と Modified を抽出
            //--------------------------------------------------
            foreach (var pair in oldDict)
            {
                string rowKey = pair.Key;
                DataRow oldRow = pair.Value;

                // (a) 新テーブルに存在しなければ削除
                if (!newDict.ContainsKey(rowKey))
                {
                    result.Add(new RowDiff(RowChangeType.Removed, rowKey));
                    continue;
                }

                // (b) 行はあるのでセル比較して変更検知
                DataRow newRow = newDict[rowKey];
                List<CellDiff> changedCells = GetCellDiffs(oldRow, newRow,
                                                           charLevel, ignoreWhiteSpace);

                if (changedCells.Count > 0)
                    result.Add(new RowDiff(RowChangeType.Modified, rowKey, changedCells));
            }

            //--------------------------------------------------
            // 3) 新テーブルにしか無い行は Added
            //--------------------------------------------------
            foreach (var pair in newDict)
            {
                string rowKey = pair.Key;
                if (!oldDict.ContainsKey(rowKey))
                    result.Add(new RowDiff(RowChangeType.Added, rowKey));
            }

            return result; // 完成！
        }

        /*----------------------------------------------------------
         * 以下は内部ヘルパーメソッド
         *--------------------------------------------------------*/

        /// <summary>
        /// DataTable から「複合キー文字列 → DataRow」の辞書を作る。
        /// </summary>
        private static Dictionary<string, DataRow> BuildLookup(DataTable table, string[] keyColumns)
        {
            var dict = new Dictionary<string, DataRow>();

            foreach (DataRow row in table.Rows)
            {
                // キーを「BOM_SID||シーケンス」のように連結して作成
                string key = BuildCompositeKey(row, keyColumns);

                // ※同じキーが二行あると例外になる。
                dict[key] = row;
            }
            return dict;
        }

        /// <summary>
        /// DataRow から複合キー文字列を作るヘルパー。
        /// </summary>
        private static string BuildCompositeKey(DataRow row, string[] keyColumns)
        {
            // 例: ["BOM_SID","シーケンス"] → "5000||01"
            string[] keyParts = new string[keyColumns.Length];

            for (int i = 0; i < keyColumns.Length; i++)
            {
                keyParts[i] = ToInvariantString(row[keyColumns[i]]);
            }
            return string.Join("||", keyParts);
        }

        /// <summary>
        /// ２つの DataRow を全列走査して違いを探す。
        /// </summary>
        private static List<CellDiff> GetCellDiffs(DataRow oldRow, DataRow newRow,
                                                   bool charLevel, bool ignoreWs)
        {
            var diffList = new List<CellDiff>();

            foreach (DataColumn col in oldRow.Table.Columns)
            {
                string colName = col.ColumnName;

                string oldVal = ToInvariantString(oldRow[colName]);
                string newVal = newRow.Table.Columns.Contains(colName)
                              ? ToInvariantString(newRow[colName])
                              : "<MISSING>";               // 新行側に列が無い場合

                if (oldVal == newVal) continue;           // 値が同じならスキップ

                // 文字レベル差分を取りたい場合だけ DiffPlex を呼ぶ
                DiffPaneModel charDiff = null;
                if (charLevel)
                    charDiff = InlineDiffBuilder.Instance.BuildDiffModel(oldVal, newVal, ignoreWs);

                diffList.Add(new CellDiff(colName, oldVal, newVal, charDiff));
            }

            return diffList;
        }

        /// <summary>
        /// NULL / DBNull を安全に文字列化。数字も文化非依存で揃える。
        /// </summary>
        private static string ToInvariantString(object value)
        {
            if (value == null || value == DBNull.Value)
                return "<NULL>";

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
