namespace Chat_Virtual___Servidor {
    partial class OracleConfigInterface {
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
            this.BCancel = new System.Windows.Forms.Button();
            this.LTitle = new System.Windows.Forms.Label();
            this.LIp = new System.Windows.Forms.Label();
            this.LPort = new System.Windows.Forms.Label();
            this.LService = new System.Windows.Forms.Label();
            this.BSave = new System.Windows.Forms.Button();
            this.LUser = new System.Windows.Forms.Label();
            this.LPassword = new System.Windows.Forms.Label();
            this.TBIp = new System.Windows.Forms.TextBox();
            this.TBPort = new System.Windows.Forms.TextBox();
            this.TBService = new System.Windows.Forms.TextBox();
            this.TBUser = new System.Windows.Forms.TextBox();
            this.TBPassword = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // BCancel
            // 
            this.BCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BCancel.Location = new System.Drawing.Point(12, 215);
            this.BCancel.Name = "BCancel";
            this.BCancel.Size = new System.Drawing.Size(75, 23);
            this.BCancel.TabIndex = 0;
            this.BCancel.Text = "Cancelar";
            this.BCancel.UseVisualStyleBackColor = true;
            this.BCancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // LTitle
            // 
            this.LTitle.AutoSize = true;
            this.LTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LTitle.Location = new System.Drawing.Point(35, 29);
            this.LTitle.Name = "LTitle";
            this.LTitle.Size = new System.Drawing.Size(250, 16);
            this.LTitle.TabIndex = 1;
            this.LTitle.Text = "Configuración de la base de datos.";
            // 
            // LIp
            // 
            this.LIp.AutoSize = true;
            this.LIp.Location = new System.Drawing.Point(18, 70);
            this.LIp.Name = "LIp";
            this.LIp.Size = new System.Drawing.Size(19, 13);
            this.LIp.TabIndex = 2;
            this.LIp.Text = "Ip:";
            // 
            // LPort
            // 
            this.LPort.AutoSize = true;
            this.LPort.Location = new System.Drawing.Point(18, 95);
            this.LPort.Name = "LPort";
            this.LPort.Size = new System.Drawing.Size(41, 13);
            this.LPort.TabIndex = 3;
            this.LPort.Text = "Puerto:";
            // 
            // LService
            // 
            this.LService.AutoSize = true;
            this.LService.Location = new System.Drawing.Point(18, 119);
            this.LService.Name = "LService";
            this.LService.Size = new System.Drawing.Size(48, 13);
            this.LService.TabIndex = 4;
            this.LService.Text = "Servicio:";
            // 
            // BSave
            // 
            this.BSave.Location = new System.Drawing.Point(225, 215);
            this.BSave.Name = "BSave";
            this.BSave.Size = new System.Drawing.Size(75, 23);
            this.BSave.TabIndex = 5;
            this.BSave.Text = "Guardar";
            this.BSave.UseVisualStyleBackColor = true;
            this.BSave.Click += new System.EventHandler(this.Save_Click);
            // 
            // LUser
            // 
            this.LUser.AutoSize = true;
            this.LUser.Location = new System.Drawing.Point(18, 143);
            this.LUser.Name = "LUser";
            this.LUser.Size = new System.Drawing.Size(46, 13);
            this.LUser.TabIndex = 6;
            this.LUser.Text = "Usuario:";
            // 
            // LPassword
            // 
            this.LPassword.AutoSize = true;
            this.LPassword.Location = new System.Drawing.Point(18, 170);
            this.LPassword.Name = "LPassword";
            this.LPassword.Size = new System.Drawing.Size(64, 13);
            this.LPassword.TabIndex = 7;
            this.LPassword.Text = "Contraseña:";
            // 
            // TBIp
            // 
            this.TBIp.Location = new System.Drawing.Point(185, 67);
            this.TBIp.Name = "TBIp";
            this.TBIp.Size = new System.Drawing.Size(100, 20);
            this.TBIp.TabIndex = 8;
            // 
            // TBPort
            // 
            this.TBPort.Location = new System.Drawing.Point(185, 95);
            this.TBPort.Name = "TBPort";
            this.TBPort.Size = new System.Drawing.Size(100, 20);
            this.TBPort.TabIndex = 9;
            // 
            // TBService
            // 
            this.TBService.Location = new System.Drawing.Point(185, 121);
            this.TBService.Name = "TBService";
            this.TBService.Size = new System.Drawing.Size(100, 20);
            this.TBService.TabIndex = 10;
            // 
            // TBUser
            // 
            this.TBUser.Location = new System.Drawing.Point(185, 147);
            this.TBUser.Name = "TBUser";
            this.TBUser.Size = new System.Drawing.Size(100, 20);
            this.TBUser.TabIndex = 11;
            // 
            // TBPassword
            // 
            this.TBPassword.Location = new System.Drawing.Point(185, 173);
            this.TBPassword.Name = "TBPassword";
            this.TBPassword.PasswordChar = '*';
            this.TBPassword.Size = new System.Drawing.Size(100, 20);
            this.TBPassword.TabIndex = 12;
            this.TBPassword.UseSystemPasswordChar = true;
            // 
            // OracleConfigInterface
            // 
            this.AcceptButton = this.BSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.BCancel;
            this.ClientSize = new System.Drawing.Size(312, 250);
            this.Controls.Add(this.TBPassword);
            this.Controls.Add(this.TBUser);
            this.Controls.Add(this.TBService);
            this.Controls.Add(this.TBPort);
            this.Controls.Add(this.TBIp);
            this.Controls.Add(this.LPassword);
            this.Controls.Add(this.LUser);
            this.Controls.Add(this.BSave);
            this.Controls.Add(this.LService);
            this.Controls.Add(this.LPort);
            this.Controls.Add(this.LIp);
            this.Controls.Add(this.LTitle);
            this.Controls.Add(this.BCancel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OracleConfigInterface";
            this.Text = "Configuración de la Base de datos.";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BCancel;
        private System.Windows.Forms.Label LTitle;
        private System.Windows.Forms.Label LIp;
        private System.Windows.Forms.Label LPort;
        private System.Windows.Forms.Label LService;
        private System.Windows.Forms.Button BSave;
        private System.Windows.Forms.Label LUser;
        private System.Windows.Forms.Label LPassword;
        public System.Windows.Forms.TextBox TBIp;
        public System.Windows.Forms.TextBox TBPort;
        public System.Windows.Forms.TextBox TBService;
        public System.Windows.Forms.TextBox TBUser;
        public System.Windows.Forms.TextBox TBPassword;
    }
}