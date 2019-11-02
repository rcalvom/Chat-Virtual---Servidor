using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public partial class ServerConfigInterface : Form {

        public static ConnectionSettings Settings;

        [Serializable]
        public struct ConnectionSettings {
            public int port;
            public int maxUsers;
        }

        public ServerConfigInterface() {
            this.InitializeComponent();
            if (File.Exists("ServerSettings.config")) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("SocketSettings.config", FileMode.Open, FileAccess.Read);
                try {
                    Settings = (ConnectionSettings)formatter.Deserialize(stream);
                } catch (Exception) {

                }
                stream.Close();
            } else {
                Settings.port = 7777;
                Settings.maxUsers = 30;
            }
            this.TBPort.Text = Settings.port.ToString();
            this.TBMaxUsers.Text = Settings.maxUsers.ToString();
        }

        private void Save_Click(object sender, EventArgs e) {
            Settings.port = Int32.Parse(this.TBPort.Text);
            Settings.maxUsers = Int32.Parse(this.TBMaxUsers.Text);
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("SocketSettings.config", FileMode.Create, FileAccess.Write);
            formatter.Serialize(stream, Settings);
            stream.Close();
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}
