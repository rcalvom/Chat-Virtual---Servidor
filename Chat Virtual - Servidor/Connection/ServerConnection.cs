using DataStructures;
using ShippingData;
using System;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static ShippingData.Message;

namespace Chat_Virtual___Servidor {

    /// <summary>
    /// Esta Clase se encarga de todas las conexiones del servidor.
    /// </summary>
    public class ServerConnection{
        public bool Connected { get; set; }                                         // Indica si el servidor esta encendido o no.
        private TcpListener Server;                                                 // Permite la escucha de conexiones entrantesde red TCP.
        private readonly GraphicInterface GraphicInterface;                         // Referencia al conexto gráfico.
        private readonly LinkedList<User> Users;                                    // Colección que contiene los usuarios conectados.
        private readonly IPEndPoint Ip;
        private readonly DataBaseConnection Oracle;

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
            this.Oracle = new DataBaseConnection(this.GraphicInterface);
            this.Ip = new IPEndPoint(IPAddress.Any, ServerConfigInterface.Settings.port);
            this.Users = new LinkedList<User>();
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
        /// Apaga el servidor, elimina la conexión de la base de datos y finaliza la escucha de nuevas solicitudes
        /// </summary>
        public void ShutDown() {
            try {
                this.DisconnectListener();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido interrumpir escucha de nuevas conexiones: \n " + ex);
            }
            try {
                this.Oracle.DisconnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            this.Connected = false;
            this.ButtonText("Encender Servidor");
            this.MenuEnable(true);
            this.ClearTable();
            while (!this.Users.IsEmpty()) {
                this.Users.Remove(0);
            }
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
                this.Oracle.ConnectDataBase();
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
            Thread t2 = new Thread(this.ExecuteRequest) {
                IsBackground = true
            };
            t2.Start();
            Thread t3 = new Thread(this.ListenUsers) {
                IsBackground = true
            };
            t3.Start();
            Thread t4 = new Thread(this.WriteUsers) {
                IsBackground = true
            };
            t4.Start();
        }

        /// <summary>
        /// Método de Hilo. Redirige los mensaje y responde a las peticiones de los usuarios
        /// </summary>
        private void ExecuteRequest() {
            do {
                Iterator<User> i = this.Users.Iterator();
                while (i.HasNext()) {
                    User user = i.Next();
                    object Readed = user.ReadingDequeue();
                    if (Readed == default) {
                        continue;
                    } else if (Readed is Chat ch) {
                        User memberTwo;
                        if (ch.memberOne.Equals(user.Name)) {
                            memberTwo = this.SearchUser(ch.memberTwo);
                        } else {
                            memberTwo = this.SearchUser(ch.memberOne);
                        }
                        user.WritingEnqueue(ch);
                        memberTwo.WritingEnqueue(ch);
                    } else if (Readed is ChatMessage ms) {
                        ms.date = new Date(DateTime.Now);
                        User receiver = this.SearchUser(ms.Receiver);
                        receiver.WritingEnqueue(ms);
                        user.WritingEnqueue(ms);
                    }
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Devuelve el primer Usuario encontrado con el nombre correspondiente.
        /// </summary>
        private User SearchUser(string userName) {
            Iterator<User> i = this.Users.Iterator();
            while (i.HasNext()) {
                User u = i.Next();
                if (u.Name.Equals(userName)) {
                    return u;
                }
            }
            return default;
        }

        /// <summary>
        /// Método de hilo. Lee cada usuario y revisa si le ha enviado datos al servidor.
        /// </summary>
        private void ListenUsers() { 
            do {
                Iterator<User> i = this.Users.Iterator();
                while (i.HasNext()) {
                    User user = i.Next();
                    this.Read(user);                        //Intenta leer los datos que estén pendientes para ese usuario 
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Método de hilo. Escribe los datos listos en los clientes.
        /// </summary>
        private void WriteUsers() {
            do {
                Iterator<User> i = this.Users.Iterator();        //Obtiene el iterador para la lista
                while (i.HasNext()) {
                    User user = i.Next();                   //Obtiene cada elemento en la lista
                    this.Write(user);                       //Intenta escribir los datos que estén pendientes para ese usuario
                }
            } while (this.Connected);
        }

        /// <summary>
        /// Esta a la escucha de nuevos clientes y verifica su información de inicio de sesión.
        /// </summary>
        private void ListenConnection() {          
            do {
                try {
                    if (this.Server.Pending()) {
                        User U = new User (this.Server.AcceptTcpClient());
                        object obj = null;
                        for (int i = 0; i<25;i++) {
                            try {
                                this.Read(U);
                                obj = U.ReadingDequeue();
                            } catch (Exception) { }
                            if (obj == default) {
                                Thread.Sleep(125);
                            } else {
                                break;
                            }
                        }
                        if (obj is SignIn si) {
                            bool exist = false;
                            this.Oracle.Oracle.ExecuteSQL("SELECT USERNAME,CONTRASENA FROM USUARIO");
                            while (this.Oracle.Oracle.DataReader.Read()) {
                                if (this.Oracle.Oracle.DataReader["USERNAME"].Equals(si.user) && this.Oracle.Oracle.DataReader["CONTRASENA"].Equals(si.password)) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {
                                U.Name = si.user;                                    
                                U.WritingEnqueue(new RequestAnswer(true));        
                                this.Write(U);                                         
                                this.Users.Add(U);                                  
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, U.Client.Client.RemoteEndPoint.ToString());

                                /*this.Oracle.Oracle.ExecuteSQL("SELECT USERNAME FROM USUARIO");
                                DynamicArray<string> Chats = new DynamicArray<string>();
                                while (this.Oracle.Oracle.DataReader.Read()) {
                                    Chats.Add((string)this.Oracle.Oracle.DataReader["USERNAME"]);
                                }
                                for (int i = 0; i < Chats.Size(); i++) {
                                    for (int j = i + 1; j < Chats.Size(); i++) {
                                        U.WritingQueue.Enqueue(new Chat(Chats.Get(i), Chats.Get(j)));
                                        this.Write(U);
                                    }
                                }*/


                                /*ChatMessage[] ms = new ChatMessage[20];
                                for (int i = 0; i<ms.Length; i++) {
                                    ms[i] = new ChatMessage("jdiegopm","jdiegopm","Prueba "+i);
                                }

                                for (int i = 0; i<ms.Length; i++) {
                                    U.WritingEnqueue(ms[i]);
                                }*/

                                Bitmap imagen = new Bitmap(@"C:\Users\ricar\Downloads\default.jpg");
                                U.WritingEnqueue(new Profile(Serializer.SerializeImage(imagen), "Hey there! I am using SADIRI."));
                                this.Write(U);

                                /*string[] tree = { null, "Tareas","Matematicas","Programacion","Taller de Calculo",null,"Programar Sadiri","Implementar Gráfos y montículos" };
                                U.WritingEnqueue(new TreeActivities(tree));
                                this.Write(U);*/

                            } else {
                                U.WritingEnqueue(new RequestAnswer(false));
                                U.WritingEnqueue(new RequestError(1));
                                this.Write(U);
                                this.ConsoleAppend("Se ha intentado conectar el remoto [" + U.Client.Client.RemoteEndPoint.ToString() + "] con información de inicio de sesión incorrecta.");
                                U.Client.Close();
                            }
                        } else if (obj is SignUp su) {
                            if (this.Oracle.Oracle.ExecuteSQL("INSERT INTO USUARIO VALUES('" + su.userName + "','" + su.name + "','" + su.password + "',SYSDATE)")) {
                                U.Name = su.userName;
                                U.WritingEnqueue(new RequestAnswer(true));
                                this.Write(U);
                                this.ConsoleAppend("Se ha registrado el usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] correctamente.");
                                this.Users.Add(U);
                                this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(U.Name, U.Client.Client.RemoteEndPoint.ToString());
                            } else {
                                U.WritingEnqueue(new RequestAnswer(false));
                                U.WritingEnqueue(new RequestError(0));
                                this.Write(U);
                                this.ConsoleAppend("Se ha intentado registrar el remoto [" + U.Client.Client.RemoteEndPoint.ToString() + "] con un nombre de usuario ya existente.");
                                U.Client.Close();
                            }
                        } /*else if (obj is ConnectionTest ct) {
                            // ¿Se va a devolver algo?
                        } else if (obj is ReconnectRequest rr) {
                            ChainNode<User> temp = this.Users.Get(0);
                            while (temp.next != null) {
                                if (U.Name.Equals(temp.element.Name)) {
                                    // Sustutuir nodo
                                    // Actualizar tabla de usuarios.
                                    this.ConsoleAppend("Reconectado correctamente con el usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "].");

                                    break;
                                }
                                temp = temp.next;
                            }
                        }*/ else {
                            this.ConsoleAppend("No se ha recibido información de ingreso por parte del remoto. [" + U.Client.Client.RemoteEndPoint.ToString() + "] Se ha desconectado del servidor");
                            //U.Client.Close();
                        }
                    }
                } catch (Exception) { }
            } while (this.Connected);
        }

        /// <summary>
        /// Escribe los datos que esten pendientes en la cola toWrite de un ususario
        /// </summary>
        /// <param name="user">El ususario del que se van a intentar escribir los datos</param>
        /// <returns>Verdadero si los datos fueron enviados, falso si almenos uno falló</returns>
        private bool Write(User user) {
            Data data = user.WritingDequeue();
            if (data == default)
                return false;
            try {
                byte[] toSend = Serializer.Serialize(data);                                      // Serializa el primer objeto de la cola
                user.Writer.Write(toSend.Length);                                                // Envía el tamaño del objeto
                user.Writer.Write(toSend);                                                       // Envía el objeto     
                return true;
            } catch (Exception) {
                this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                user.WritingEnqueue(data);
                return false;
            }
        }

        /// <summary>
        /// Lee los datos que estén pendientes para un usuario y los guarda en la cola ReadingQueue del mismo
        /// </summary>
        /// <param name="user">EL ususario del que se van a intentar leer datos</param>
        /// <returns>Verdadero si le leyeron todos los datos, falso si uno de ellos no pudo ser leido</returns>
        private bool Read(User user) {
            try {
                if (user.Stream.DataAvailable) {                                            // Verefica si hay datos por leer
                    int size = user.Reader.ReadInt32();                                     // lee el tamaño del objeto
                    byte[] data = new byte[size];                                           // crea el arreglo de bytes para el objeto
                    data = user.Reader.ReadBytes(size);                                     // lee el el objeto y lo guarda en el arreglo de bytes
                    object a = Serializer.Deserialize(data);                                // deserializa el objeto
                    user.ReadingEnqueue((Data)a);                                           // guarda el objeto en la cola
                }
                return true;
            } catch (Exception) {
                //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                return false;
            }
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
                this.GraphicInterface.UsersTable.Invoke(d, new object[] { name});
            } else {
                int size = this.GraphicInterface.UsersTable.Rows.Count;
                for (int i = 0; i<size; i++) {
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
