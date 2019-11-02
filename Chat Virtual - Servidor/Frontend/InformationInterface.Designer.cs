namespace Chat_Virtual___Servidor.Frontend {
    partial class InformationInterface {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.BExit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // BExit
            // 
            this.BExit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BExit.Location = new System.Drawing.Point(315, 200);
            this.BExit.Name = "BExit";
            this.BExit.Size = new System.Drawing.Size(75, 23);
            this.BExit.TabIndex = 0;
            this.BExit.Text = "Salir.";
            this.BExit.UseVisualStyleBackColor = true;
            // 
            // InformationInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.BExit;
            this.ClientSize = new System.Drawing.Size(402, 235);
            this.ControlBox = false;
            this.Controls.Add(this.BExit);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InformationInterface";
            this.Text = "Información";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button BExit;
    }
}