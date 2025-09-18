namespace Diablo2RProject
{
    partial class DiabloForm
    {
        /// <summary>
        /// Wymagana zmienna projektanta.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Wyczyść wszystkie używane zasoby.
        /// </summary>
        /// <param name="disposing">prawda, jeżeli zarządzane zasoby powinny zostać zlikwidowane; Fałsz w przeciwnym wypadku.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Kod generowany przez Projektanta formularzy systemu Windows

        /// <summary>
        /// Metoda wymagana do obsługi projektanta — nie należy modyfikować
        /// jej zawartości w edytorze kodu.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DiabloForm));
            this.Button1 = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.PlayTimer = new System.Windows.Forms.Timer(this.components);
            this.MapView = new System.Windows.Forms.PictureBox();
            this.tBoard1 = new Diablo2RProject.TBoard();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MapView)).BeginInit();
            this.SuspendLayout();
            // 
            // Button1
            // 
            this.Button1.Location = new System.Drawing.Point(102, 458);
            this.Button1.Margin = new System.Windows.Forms.Padding(0);
            this.Button1.Name = "Button1";
            this.Button1.Size = new System.Drawing.Size(75, 23);
            this.Button1.TabIndex = 0;
            this.Button1.Text = "LoadMap";
            this.Button1.UseVisualStyleBackColor = true;
            this.Button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.MapView);
            this.panel1.Controls.Add(this.Button1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(828, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(263, 673);
            this.panel1.TabIndex = 2;
            // 
            // PlayTimer
            // 
            this.PlayTimer.Enabled = true;
            this.PlayTimer.Interval = 40;
            this.PlayTimer.Tick += new System.EventHandler(this.PlayTimer_Tick);
            // 
            // MapView
            // 
            this.MapView.Location = new System.Drawing.Point(59, 31);
            this.MapView.Name = "MapView";
            this.MapView.Size = new System.Drawing.Size(153, 135);
            this.MapView.TabIndex = 1;
            this.MapView.TabStop = false;
            // 
            // tBoard1
            // 
            this.tBoard1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tBoard1.Game = null;
            this.tBoard1.Location = new System.Drawing.Point(0, 0);
            this.tBoard1.Name = "tBoard1";
            this.tBoard1.ScrollPos = ((System.Drawing.PointF)(resources.GetObject("tBoard1.ScrollPos")));
            this.tBoard1.Size = new System.Drawing.Size(828, 673);
            this.tBoard1.TabIndex = 3;
            this.tBoard1.Zoom = 0.5F;
            // 
            // DiabloForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(1091, 673);
            this.ControlBox = false;
            this.Controls.Add(this.tBoard1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DiabloForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.DiabloForm_Load);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MapView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Button1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Panel panel1;
        private TBoard tBoard1;
        private System.Windows.Forms.Timer PlayTimer;
        private System.Windows.Forms.PictureBox MapView;
    }
}

