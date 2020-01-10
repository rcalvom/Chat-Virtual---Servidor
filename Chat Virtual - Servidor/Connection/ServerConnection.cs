using DataStructures;
using ShippingData;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {

    /// <summary>
    /// Esta Clase se encarga de todas las conexiones del servidor.
    /// </summary>
    public class ServerConnection {
        public bool Connected { get; set; }                                         // Indica si el servidor esta encendido o no.
        private TcpListener Server;                                                 // Permite la escucha de conexiones entrantesde red TCP.
        private readonly GraphicInterface GraphicInterface;                         // Referencia al conexto gráfico.
        private readonly IPEndPoint Ip;
        public static DataBaseConnection Oracle;
        public static HashTable<string, User> Users;                                // Colección que contiene los usuarios conectados.

        //Métodos delegados, permiten que se pueda acceder a la interfaz desde cualquier hilo.
        private delegate void DLogConsoleAppend(string text);
        private delegate void DButtonText(string text);
        private delegate void DButtonEnable(bool flag);
        private delegate void DDataGridViewPush(string name, string ip);
        private delegate void DDataGridViewPop(string name);
        private delegate void DMenuEnable(bool flag);
        private delegate void DAlterTable(string name, string ip);
        private delegate void DClearTable();

        /// <summary>
        /// Constructor. Inicializa la instancia para poder ser posteriormente utilizada como servidor.
        /// </summary>
        /// <param name="GraphicInterface">Referencia al a interfaz gráfica.</param>
        public ServerConnection(GraphicInterface GraphicInterface) {
            OracleConfigInterface.InitSettings();
            ServerConfigInterface.InitSettings();
            this.Connected = false;
            this.GraphicInterface = GraphicInterface;
            ServerConnection.Oracle = new DataBaseConnection(this.GraphicInterface);
            this.Ip = new IPEndPoint(IPAddress.Any, ServerConfigInterface.Settings.port);
            ServerConnection.Users = new HashTable<string, User>(1000);
        }

        /// <summary>
        /// Enciende el servidor, permite que se escuchen solicitudes de conexión entrantes.
        /// </summary>
        public void ShutUp() {
            new Thread(this.Connect) {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Apaga el servidor, elimina la conexión de la base de datos y finaliza la escucha de nuevas solicitudes.
        /// </summary>
        public void ShutDown() {
            try {
                this.DisconnectListener();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido interrumpir escucha de nuevas conexiones: \n " + ex);
            }
            try {
                ServerConnection.Oracle.DisconnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            this.Connected = false;
            this.ButtonText("Encender Servidor");
            this.MenuEnable(true);
            this.ClearTable();
            ServerConnection.Users.MakeEmpty();
        }

        /// <summary>
        /// Método de un hilo. Inicializa la escucha de solicitudes y la base de datos.
        /// </summary>
        private void Connect() {
            this.MenuEnable(false);
            this.ButtonEnable(false);
            this.Connected = true;
            try {
                this.ConnectListener();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha conseguido inicializar el servidor correctamente: \n" + ex + "/n");
                this.ConsoleAppend("Servidor no inicializado.");
                this.ButtonEnable(true);
                this.MenuEnable(true);
                this.Connected = false;
                return;
            }
            try {
                ServerConnection.Oracle.ConnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido conectar a la base de datos: \n " + ex + "\n");
                this.ConsoleAppend("Servidor no inicializado.\n");
                this.DisconnectListener();
                this.ButtonEnable(true);
                this.MenuEnable(true);
                this.Connected = false;
                return;
            }
            this.ConsoleAppend("Servidor inicializado correctamente.\n");
            this.ButtonText("Apagar Servidor");
            this.ButtonEnable(true);

            // TODO: CARGAR GRUPOS ACTIVOS

        }

        /// <summary>
        /// Detiene la escucha de solicitudes de conexión.
        /// </summary>
        private void DisconnectListener() {
            this.Server.Stop();
            this.ConsoleAppend("Se ha detenido el servidor correctamente.");
            this.ConsoleAppend("Se han dejado de escuchar solicitudes de conexión entrantes.");
        }

        /// <summary>
        /// Permite que las solicitudes de conexión sean escuchadas.
        /// </summary>
        private void ConnectListener() {
            this.ConsoleAppend("Iniciando conexión de los sockets.\n");
            this.Server = new TcpListener(this.Ip);
            this.Server.Start();
            this.ConsoleAppend("El servidor se a inicializado correctamente.");
            Thread t1 = new Thread(this.ListenConnection) {
                IsBackground = true
            };
            t1.Start();
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
        }

        /// <summary>
        /// Esta a la escucha de nuevos clientes y verifica su información de inicio de sesión.
        /// </summary>
        private void ListenConnection() {
            do {
                try {
                    if (this.Server.Pending()) {                                            // Si hay solicitudes de conexión entrantes.
                        User U = new User(this.Server.AcceptTcpClient());
                        object obj = null;

                        for (int i = 0; i < 25; i++) {                                      // Intenta 25 veces recibir el objeto inicial.
                            try {
                                U.Read();
                                obj = U.ReadingDequeue();
                            } catch (Exception) { }
                            if (obj == null) {
                                Thread.Sleep(125);
                            } else {
                                break;
                            }
                        }

                        if (obj is SignIn si) {                                             // Si el objeto recibido es un inicio de sesión.
                            bool exist = false;
                            ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT * FROM INFOMRACION_INICIO");
                            while (ServerConnection.Oracle.Oracle.DataReader.Read()) {
                                if (ServerConnection.Oracle.Oracle.DataReader["USUARIO"].Equals(si.user) && ServerConnection.Oracle.Oracle.DataReader["CONTRASEÑA"].Equals(si.password)) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {                                                    // Si la información del cliente corresponde con la de la base de datos.
                                U.Name = si.user;
                                U.WritingEnqueue(new RequestAnswer(true));
                                U.Write();
                                ServerConnection.Users.AddElement(U.Name, U);
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                                Profile profile = new Profile();
                                ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                ServerConnection.Oracle.Oracle.DataReader.Read();
                                string path = ServerConnection.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
                                ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                ServerConnection.Oracle.Oracle.DataReader.Read();
                                string status = ServerConnection.Oracle.Oracle.DataReader["ESTADO"].ToString();
                                using (FileStream stream = File.Open(path, FileMode.Open)) {
                                    profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                }
                                profile.Status = status;
                                profile.Name = U.Name;
                                U.WritingEnqueue(profile);

                                TreeActivities tree = new TreeActivities();
                                ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_ARBOL FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                ServerConnection.Oracle.Oracle.DataReader.Read();
                                string treePath = ServerConnection.Oracle.Oracle.DataReader["RUTA_ARBOL"].ToString();
                                if (treePath != "") {
                                    IFormatter formatter = new BinaryFormatter();
                                    using (FileStream stream = File.Open(treePath, FileMode.Open, FileAccess.Read)) {
                                        tree.Node = (TreeNode[])formatter.Deserialize(stream);
                                        stream.Close();
                                    }
                                    U.WritingEnqueue(tree);
                                }
                            } else {                                                                                        // Si la infomación de inicio de sesión es incorrecta.
                                U.WritingEnqueue(new RequestAnswer(false));
                                U.WritingEnqueue(new RequestError(1));
                                U.Write();
                                this.ConsoleAppend("Se ha intentado conectar el remoto [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] con información de inicio de sesión incorrecta.");
                                U.Client.Close();
                            }
                        } else if (obj is SignUp su) {                                                                      // Si el objeto recibido es un nuevo registro
                            if (ServerConnection.Oracle.Oracle.ExecuteSQL("INSERT INTO USUARIOS VALUES('" + su.userName + "', '" + su.name + "', '" + su.password + "', 'Hey there! I am using SADIRI.','F:\\SADIRI\\Usuarios\\default.png', null, default)")) {
                                U.Name = su.userName;
                                U.WritingEnqueue(new RequestAnswer(true));
                                U.Write();
                                this.ConsoleAppend("Se ha registrado el usuario [" + U.Name + " | " + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] correctamente.");
                                ServerConnection.Users.AddElement(U.Name, U);
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                                Profile profile = new Profile();
                                ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                ServerConnection.Oracle.Oracle.DataReader.Read();
                                string path = ServerConnection.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
                                ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + U.Name + "'");
                                ServerConnection.Oracle.Oracle.DataReader.Read();
                                string status = ServerConnection.Oracle.Oracle.DataReader["ESTADO"].ToString();
                                using (FileStream stream = File.Open(path, FileMode.Open)) {
                                    profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                    stream.Close();
                                }
                                profile.Status = status;
                                profile.Name = U.Name;
                                U.WritingEnqueue(profile);

                            } else {
                                U.WritingEnqueue(new RequestAnswer(false));
                                U.WritingEnqueue(new RequestError(0));
                                U.Write();
                                this.ConsoleAppend("Se ha intentado registrar el remoto [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] con un nombre de usuario ya existente.");
                                U.Client.Close();
                            }
                        } else if (obj is null) {                                                 // Si no llego objeto inicial.
                            this.ConsoleAppend("No se recibió informmación de ingreso por parte del remoto. [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] Se ha desconectado del servidor.");
                            U.Client.Close();
                        } else {                                                                  // Si el objeto inicial no es un tipo de dato reconocido.
                            this.ConsoleAppend("No se reconoce la información de ingreso por parte del remoto. [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] Se ha desconectado del servidor.");
                            U.Client.Close();
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Metodo para hilo. Agrega el texto a la interfaz gráfica.
        /// </summary>
        /// <param name="text">Texto a agregar a la consola.</param>
        private void ConsoleAppend(string text) {
            if (this.GraphicInterface.LogConsole.InvokeRequired) {
                DLogConsoleAppend d = new DLogConsoleAppend(this.ConsoleAppend);
                this.GraphicInterface.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

        /// <summary>
        /// Metodo para hilo. Cambia el texto del botón de la interfaz.
        /// </summary>
        /// <param name="text">Texto por el cual se va a cambiar el texto.</param>
        private void ButtonText(string text) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                DButtonText d = new DButtonText(this.ButtonText);
                this.GraphicInterface.Button.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.Button.Text = text;
            }
        }

        /// <summary>
        /// Metodo para hilo. definie si el botón esta habilitado.
        /// </summary>
        /// <param name="flag">Bandera con la cual se habilitará o deshabilitará el botón.</param>
        private void ButtonEnable(bool flag) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                DButtonEnable d = new DButtonEnable(this.ButtonEnable);
                this.GraphicInterface.Button.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.Button.Enabled = flag;
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        /// <param name="ip">Ip del usuario que se va a conectar.</param>
        private void InsertTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DDataGridViewPush d = new DDataGridViewPush(this.InsertTable);
                this.GraphicInterface.UsersTable.Invoke(d, new object[] { name, ip });
            } else {
                this.GraphicInterface.UsersTable.Rows.Add(name, ip);
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        private void DeleteTable(string name) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DDataGridViewPop d = new DDataGridViewPop(this.DeleteTable);
                this.GraphicInterface.UsersTable.Invoke(d, new object[] { name });
            } else {
                int size = this.GraphicInterface.UsersTable.Rows.Count;
                for (int i = 0; i < size; i++) {
                    if (this.GraphicInterface.UsersTable.Rows[i].Cells[0].Value.Equals(name)) {
                        this.GraphicInterface.UsersTable.Rows.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Metodo para hilo. Agrega el un usuario conectado a la tabla de la interfaz.
        /// </summary>
        /// <param name="name">Nombre del usuario que se va a conectar.</param>
        private void AlterIpTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DAlterTable d = new DAlterTable(this.AlterIpTable);
                this.GraphicInterface.UsersTable.Invoke(d, new object[] { name, ip });
            } else {
                int size = this.GraphicInterface.UsersTable.Rows.Count;
                for (int i = 0; i < size; i++) {
                    if (this.GraphicInterface.UsersTable.Rows[i].Cells[0].Value.Equals(name)) {
                        this.GraphicInterface.UsersTable.Rows[i].Cells[1].Value = ip;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Metodo para hilo. Habilita o deshabilita el menú.
        /// </summary>
        /// <param name="flag">Verdadero si se quiere habilitar.</param>
        private void MenuEnable(bool flag) {
            if (this.GraphicInterface.MenuBar.InvokeRequired) {
                DMenuEnable d = new DMenuEnable(this.MenuEnable);
                this.GraphicInterface.MenuBar.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.configuraciónToolStripMenuItem.Enabled = flag;

            }
        }

        /// <summary>
        /// Metodo para hilo. Elimina todos los elementos de la tabla.
        /// </summary>
        private void ClearTable() {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                DClearTable d = new DClearTable(this.ClearTable);
                this.GraphicInterface.UsersTable.Invoke(d, null);
            } else {
                this.GraphicInterface.UsersTable.Rows.Clear();
            }
        }
    }
}
