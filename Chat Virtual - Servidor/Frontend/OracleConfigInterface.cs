using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public partial class OracleConfigInterface : Form {

        public static ConnectionSettings Settings;

        [Serializable]
        public struct ConnectionSettings {
            public string Ip;
            public string Port;
            public string Service;
            public string User;
            public string Password;
        }

        public OracleConfigInterface() {
            this.InitializeComponent();
            if (File.Exists("OracleSettings.config")) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("OracleSettings.config", FileMode.Open, FileAccess.Read);
                try {
                    Settings = (ConnectionSettings)formatter.Deserialize(stream);
                } catch (Exception) {

                }
                stream.Close();
            } else {
                Settings.Ip = "localhost";
                Settings.Port = "1521";
                Settings.Service = "orcl";
                Settings.User = "Sadiri";
                Settings.Password = "123456";
            }
            this.TBIp.Text = Settings.Ip;
            this.TBPort.Text = Settings.Port;
            this.TBService.Text = Settings.Service;
            this.TBUser.Text = Settings.User;
            this.TBPassword.Text = Settings.Password;
        }

        private void Save_Click(object sender, EventArgs e) {
            Settings.Ip = this.TBIp.Text;
            Settings.Port = this.TBPort.Text;
            Settings.Service = this.TBService.Text;
            Settings.User = this.TBUser.Text;
            Settings.Password = this.TBPassword.Text;
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("OracleSettings.config", FileMode.Create, FileAccess.Write);
            formatter.Serialize(stream, Settings);
            stream.Close();
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}
