namespace Diablo2RProject
{
    partial class TBoard
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

        #region Kod wygenerowany przez Projektanta składników

        /// <summary> 
        /// Metoda wymagana do obsługi projektanta — nie należy modyfikować 
        /// jej zawartości w edytorze kodu.
        /// </summary>
        private void InitializeComponent()
        {
            this.DialogBox1 = new System.Windows.Forms.Label();
            this.DialogBox2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // DialogBox1
            // 
            this.DialogBox1.BackColor = System.Drawing.Color.Transparent;
            this.DialogBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.DialogBox1.Location = new System.Drawing.Point(0, 588);
            this.DialogBox1.Name = "DialogBox1";
            this.DialogBox1.Size = new System.Drawing.Size(967, 70);
            this.DialogBox1.TabIndex = 0;
            this.DialogBox1.Text = "label1";
            this.DialogBox1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.DialogBox1.Visible = false;
            // 
            // DialogBox2
            // 
            this.DialogBox2.BackColor = System.Drawing.Color.Transparent;
            this.DialogBox2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.DialogBox2.Location = new System.Drawing.Point(0, 518);
            this.DialogBox2.Name = "DialogBox2";
            this.DialogBox2.Size = new System.Drawing.Size(967, 70);
            this.DialogBox2.TabIndex = 1;
            this.DialogBox2.Text = "label2";
            this.DialogBox2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.DialogBox2.Visible = false;
            // 
            // TBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.DialogBox2);
            this.Controls.Add(this.DialogBox1);
            this.Name = "TBoard";
            this.Size = new System.Drawing.Size(967, 658);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label DialogBox1;
        private System.Windows.Forms.Label DialogBox2;
    }
}
