namespace HotelBooking
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblDay;
        private System.Windows.Forms.Button btnNextDay;

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabRooms;
        private System.Windows.Forms.TabPage tabOrders;
        private System.Windows.Forms.TabPage tabStats;

        private System.Windows.Forms.DataGridView dgvRooms;
        private System.Windows.Forms.DataGridView dgvOrders;
        private System.Windows.Forms.DataGridView dgvStatistics;

        private System.Windows.Forms.RichTextBox rtbLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblDay = new System.Windows.Forms.Label();
            this.btnNextDay = new System.Windows.Forms.Button();

            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabRooms = new System.Windows.Forms.TabPage();
            this.tabOrders = new System.Windows.Forms.TabPage();
            this.tabStats = new System.Windows.Forms.TabPage();

            this.dgvRooms = new System.Windows.Forms.DataGridView();
            this.dgvOrders = new System.Windows.Forms.DataGridView();
            this.dgvStatistics = new System.Windows.Forms.DataGridView();

            this.rtbLog = new System.Windows.Forms.RichTextBox();

            this.tabControl1.SuspendLayout();
            this.tabRooms.SuspendLayout();
            this.tabOrders.SuspendLayout();
            this.tabStats.SuspendLayout();

            ((System.ComponentModel.ISupportInitialize)(this.dgvRooms)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatistics)).BeginInit();

            this.SuspendLayout();

            // ===== LABEL DAY =====
            this.lblDay.AutoSize = true;
            this.lblDay.Location = new System.Drawing.Point(12, 9);
            this.lblDay.Name = "lblDay";
            this.lblDay.Size = new System.Drawing.Size(100, 13);
            this.lblDay.Text = "Дата: ";

            // ===== BUTTON NEXT DAY =====
            this.btnNextDay.Location = new System.Drawing.Point(120, 5);
            this.btnNextDay.Name = "btnNextDay";
            this.btnNextDay.Size = new System.Drawing.Size(130, 30);
            this.btnNextDay.Text = "Следующий день";
            this.btnNextDay.Click += new System.EventHandler(this.btnNextDay_Click);

            // ===== TAB CONTROL =====
            this.tabControl1.Location = new System.Drawing.Point(12, 45);
            this.tabControl1.Size = new System.Drawing.Size(960, 400);

            // TAB 1 - Номера
            this.tabRooms.Text = "Номера";
            this.tabRooms.Controls.Add(this.dgvRooms);

            // TAB 2 - Заказы
            this.tabOrders.Text = "Заказы";
            this.tabOrders.Controls.Add(this.dgvOrders);

            // TAB 3 - Статистика
            this.tabStats.Text = "Статистика";
            this.tabStats.Controls.Add(this.dgvStatistics);

            this.tabControl1.Controls.Add(this.tabRooms);
            this.tabControl1.Controls.Add(this.tabOrders);
            this.tabControl1.Controls.Add(this.tabStats);

            // ===== DATA GRIDS =====
            this.dgvRooms.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOrders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStatistics.Dock = System.Windows.Forms.DockStyle.Fill;

            // ===== LOG =====
            this.rtbLog.Location = new System.Drawing.Point(12, 460);
            this.rtbLog.Size = new System.Drawing.Size(960, 180);
            this.rtbLog.ReadOnly = true;

            // ===== FORM =====
            this.ClientSize = new System.Drawing.Size(984, 661);
            this.Controls.Add(this.lblDay);
            this.Controls.Add(this.btnNextDay);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.rtbLog);

            this.Text = "Бронирование гостиницы";

            this.tabControl1.ResumeLayout(false);
            this.tabRooms.ResumeLayout(false);
            this.tabOrders.ResumeLayout(false);
            this.tabStats.ResumeLayout(false);

            ((System.ComponentModel.ISupportInitialize)(this.dgvRooms)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatistics)).EndInit();

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}