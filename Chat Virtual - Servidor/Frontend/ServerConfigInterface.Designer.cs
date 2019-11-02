namespace Chat_Virtual___Servidor {
    partial class ServerConfigInterface {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
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
            this.LTitle = new System.Windows.Forms.Label();
            this.LMaxUsers = new System.Windows.Forms.Label();
            this.LPort = new System.Windows.Forms.Label();
            this.TBPort = new System.Windows.Forms.TextBox();
            this.TBMaxUsers = new System.Windows.Forms.TextBox();
            this.BSave = new System.Windows.Forms.Button();
            this.BCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LTitle
            // 
            this.LTitle.AutoSize = true;
            this.LTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LTitle.Location = new System.Drawing.Point(44, 28);
            this.LTitle.Name = "LTitle";
            this.LTitle.Size = new System.Drawing.Size(196, 16);
            this.LTitle.TabIndex = 0;
            this.LTitle.Text = "Configuracion del Servidor.";
            // 
            // LMaxUsers
            // 
            this.LMaxUsers.AutoSize = true;
            this.LMaxUsers.Location = new System.Drawing.Point(153, 71);
            this.LMaxUsers.Name = "LMaxUsers";
            this.LMaxUsers.Size = new System.Drawing.Size(74, 13);
            this.LMaxUsers.TabIndex = 1;
            this.LMaxUsers.Text = "Max. Usuarios";
            // 
            // LPort
            // 
            this.LPort.AutoSize = true;
            this.LPort.Location = new System.Drawing.Point(16, 71);
            this.LPort.Name = "LPort";
            this.LPort.Size = new System.Drawing.Size(41, 13);
            this.LPort.TabIndex = 2;
            this.LPort.Text = "Puerto:";
            // 
            // TBPort
            // 
            this.TBPort.Location = new System.Drawing.Point(19, 100);
            this.TBPort.Name = "TBPort";
            this.TBPort.Size = new System.Drawing.Size(89, 20);
            this.TBPort.TabIndex = 3;
            // 
            // TBMaxUsers
            // 
            this.TBMaxUsers.Location = new System.Drawing.Point(156, 100);
            this.TBMaxUsers.Name = "TBMaxUsers";
            this.TBMaxUsers.Size = new System.Drawing.Size(100, 20);
            this.TBMaxUsers.TabIndex = 4;
            // 
            // BSave
            // 
            this.BSave.Location = new System.Drawing.Point(181, 150);
            this.BSave.Name = "BSave";
            this.BSave.Size = new System.Drawing.Size(75, 23);
            this.BSave.TabIndex = 5;
            this.BSave.Text = "Guardar.";
            this.BSave.UseVisualStyleBackColor = true;
            this.BSave.Click += new System.EventHandler(this.Save_Click);
            // 
            // BCancel
            // 
            this.BCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BCancel.Location = new System.Drawing.Point(19, 150);
            this.BCancel.Name = "BCancel";
            this.BCancel.Size = new System.Drawing.Size(75, 23);
            this.BCancel.TabIndex = 6;
            this.BCancel.Text = "Cancelar.";
            this.BCancel.UseVisualStyleBackColor = true;
            this.BCancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ServerConfigInterface
            // 
            this.AcceptButton = this.BSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.BCancel;
            this.ClientSize = new System.Drawing.Size(268, 185);
            this.Controls.Add(this.BCancel);
            this.Controls.Add(this.BSave);
            this.Controls.Add(this.TBMaxUsers);
            this.Controls.Add(this.TBPort);
            this.Controls.Add(this.LPort);
            this.Controls.Add(this.LMaxUsers);
            this.Controls.Add(this.LTitle);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ServerConfigInterface";
            this.Text = "Configuración del servidor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LTitle;
        private System.Windows.Forms.Label LMaxUsers;
        private System.Windows.Forms.Label LPort;
        private System.Windows.Forms.Button BSave;
        private System.Windows.Forms.Button BCancel;
        public System.Windows.Forms.TextBox TBPort;
        public System.Windows.Forms.TextBox TBMaxUsers;
    }
}