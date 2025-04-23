using System.Windows.Forms;

namespace DiffGridDemo
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView gridOld;
        private DataGridView gridNew;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gridOld = new DataGridView();
            this.gridNew = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridOld)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridNew)).BeginInit();
            this.SuspendLayout();
            // 
            // gridOld
            // 
            this.gridOld.AllowUserToAddRows = false;
            this.gridOld.AllowUserToDeleteRows = false;
            this.gridOld.Dock = DockStyle.Left;
            this.gridOld.ReadOnly = true;
            this.gridOld.Width = 400;
            // 
            // gridNew
            // 
            this.gridNew.AllowUserToAddRows = false;
            this.gridNew.AllowUserToDeleteRows = false;
            this.gridNew.Dock = DockStyle.Fill;
            this.gridNew.ReadOnly = true;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(830, 450);
            this.Controls.Add(this.gridNew);
            this.Controls.Add(this.gridOld);
            this.Name = "MainForm";
            this.Text = "DataTable 差分デモ";
            ((System.ComponentModel.ISupportInitialize)(this.gridOld)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridNew)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
