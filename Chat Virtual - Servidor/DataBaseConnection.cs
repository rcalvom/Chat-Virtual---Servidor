using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {

    public class DataBaseConnection {

        private readonly GraphicInterface GraphicInterface;
        private OracleDataBase Oracle;
        private readonly ConnectionSettings Settings;
        private delegate void LogConsoleAppend(string text);

        [Serializable]
        private struct ConnectionSettings {
            public string Ip;
            public string Port;
            public string Service;
            public string User;
            public string Password;
        }

        public DataBaseConnection(GraphicInterface GraphicInterface) {
            this.GraphicInterface = GraphicInterface;
            if(File.Exists("OracleSettings.config")) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("OracleSettings.config", FileMode.Open, FileAccess.Read);
                this.Settings = (ConnectionSettings)formatter.Deserialize(stream);
                stream.Close();
            } else {
                this.Settings.Ip = "localhost";
                this.Settings.Port = "1521";
                this.Settings.Service = "orcl";
                this.Settings.User = "Sadiri";
                this.Settings.Password = "123456";

                /*
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("OracleSettings.config", FileMode.Create, FileAccess.Write);
                formatter.Serialize(stream, Settings);
                stream.Close();
                */

            }
        }

        public void ConnectDataBase() {
            this.Oracle = new OracleDataBase(this.Settings.Ip, this.Settings.Port, this.Settings.Service, this.Settings.User, this.Settings.Password);
            this.ConsoleAppend("Se ha conectado correctamente a la base de datos Oracle, versión: "+ this.Oracle.getConnection().ServerVersion);
        }

        public void DisconnectDataBase() {
            this.Oracle.Disconnect();
        }

        public OracleDataBase GetOracleDataBase() {
            return this.Oracle;
        }

        private void ConsoleAppend(string text) {
            if(this.GraphicInterface.LogConsole.InvokeRequired) {
                var d = new LogConsoleAppend(this.ConsoleAppend);
                this.GraphicInterface.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

    }
}
