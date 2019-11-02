using System;
using System.Globalization;

namespace Chat_Virtual___Servidor {

    public class DataBaseConnection {

        private readonly GraphicInterface GraphicInterface;
        public OracleDataBase Oracle { set; get; }
        private delegate void LogConsoleAppend(string text);

        public DataBaseConnection(GraphicInterface GraphicInterface) {
            this.GraphicInterface = GraphicInterface;
        }

        public void ConnectDataBase() {
            this.Oracle = new OracleDataBase(OracleConfigInterface.Settings.Ip, OracleConfigInterface.Settings.Port, OracleConfigInterface.Settings.Service, OracleConfigInterface.Settings.User, OracleConfigInterface.Settings.Password);
            this.ConsoleAppend("Se ha conectado correctamente a la base de datos Oracle, versión: "+ this.Oracle.getConnection().ServerVersion);
        }

        public void DisconnectDataBase() {
            this.Oracle.Disconnect();
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
