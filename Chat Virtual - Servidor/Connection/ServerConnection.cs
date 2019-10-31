using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ShippingData;
using DataStructures;

namespace Chat_Virtual___Servidor{

    public class ServerConnection{

        private readonly DataBaseConnection Oracle;
        private ConnectionSettings settings;
        public bool Connected { get; set; }
        private bool threads;
        public TcpListener Server { get; set; }
        public GraphicInterface GraphicInterface { get; set; }
        public LinkedList<User> Users { get; set; }
        public LinkedList<User> toInitialize { get; set; }
        public LinkedList<Group> Groups { get; set; }
        public IPEndPoint Ip { get; }

        private delegate void DLogConsoleAppend(string text);
        private delegate void DButtonText(string text);
        private delegate void DButtonEnable(bool flag);
        private delegate void DDataGridViewRow(string name, string ip);

        [Serializable]
        private struct ConnectionSettings {
            public int port;
            public int maxUsers;
        }
        public ServerConnection(GraphicInterface GraphicInterface) {
            this.Connected = false;
            this.GraphicInterface = GraphicInterface;
            this.Oracle = new DataBaseConnection(this.GraphicInterface);
            if (File.Exists("SocketSettings.config")) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("SocketSettings.config", FileMode.Open, FileAccess.Read);
                this.settings = (ConnectionSettings)formatter.Deserialize(stream);
                stream.Close();
            } else {
                this.settings.port = 7777;
                this.settings.maxUsers = 30;
            }
            this.Ip = new IPEndPoint(IPAddress.Any, this.settings.port);
            this.Users = new LinkedList<User>();
            this.toInitialize = new LinkedList<User>();
            this.Groups = new LinkedList<Group>();
            threads = false;
        }

        public void ShutUp() {
            new Thread(this.Connect) {
                IsBackground = true
            }.Start();
        }

        public void Connect() {
            this.ButtonEnable(false);
            try {
                this.ConnectSockets();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha conseguido inicializar el servidor correctamente: \n" + ex + "/n");
                this.ConsoleAppend("Servidor no inicializado.");
                this.ButtonEnable(true);
                return;
            }
            try {
                this.Oracle.ConnectDataBase();
            } catch (Exception ex) {
                this.ConsoleAppend("No se ha podido conectar a la base de datos: \n " + ex + "\n");
                this.ConsoleAppend("Servidor no inicializado.\n");
                this.DisconnectSockets();
                this.ButtonEnable(true);
                return;
            }
            this.Connected = true;
            this.ConsoleAppend("Servidor inicializado correctamente.\n");
            this.ButtonText("Apagar Servidor");
            this.ButtonEnable(true);
        }

        private void DisconnectSockets() {
            threads = false;
            this.Server.Stop();
            this.ConsoleAppend("Se ha detenido el servidor correctamente.");
            this.ConsoleAppend("Se han dejado de escuchar solicitudes de conexión entrantes.");
        }

        public void ConnectSockets() {
            this.ConsoleAppend("Iniciando conexión de los sockets.\n");
            this.Server = new TcpListener(this.Ip);
            this.Server.Start();
            this.ConsoleAppend("El servidor se a inicializado correctamente.");
            Thread t1 = new Thread(this.ListenConnection) {
                IsBackground = true
            };
            t1.Start();
            this.ConsoleAppend("Se han comenzado a escuchar solicitudes de conexión entrantes.\n");
            threads = true;
            Thread request = new Thread(this.ExecuteRequest);
            request.Start();
            Thread listenUsers = new Thread(this.ListenUsers);
            listenUsers.Start();
            Thread writeTo = new Thread(this.WriteUsers);
            writeTo.Start();
        }

        /// <summary>
        /// Redirige los mensaje y responde a las peticiones de los usuarios
        /// </summary>
        private void ExecuteRequest() {
            ChainNode<User> Node = null;
            do {
                if (this.Users.IsEmpty()) {                     //Verifica que la lista de ususarios no está vacia
                    continue;                                   //En caso de que lo está continúa con la ejecucion
                } else if (Node == null) {                      //Verifia si el nodo que tengo es un apuntador nulo
                    Node = this.Users.GetNode(0);               //En ese caso obtiene el primer nodo de la lista
                }

                object data = Node.element.toRead.Dequeue();
                if (data is ChatMessage ms) {
                    //mirar el ususario que recibe para reenviarselo y hacer un insert en la tabla
                }

                Node = Node.next;
            } while (threads);
        }


        // Hilo listo con la serializacion implementada
        private void ListenUsers() { 
            ChainNode<User> Node = null;
            do {
                if (this.Users.IsEmpty()) {                     //Verifica que la lista de ususarios no está vacia
                    continue;                                   //En caso de que lo está continúa con la ejecucion
                } else if (Node == null) {                      //Verifia si el nodo que tengo es un apuntador nulo
                    Node = this.Users.GetNode(0);               //En ese caso obtiene el primer nodo de la lista
                }

                Read(Node.element, false);                      //Intenta leer los datos que estén pendientes para ese usuario
                Node = Node.next;                               //Avanza al siguiente nodo
            } while (threads);
        }

        // Hilo listo con la serializacion implementada
        private void WriteUsers() {
            ChainNode<User> Node = null;
            do {
                if (this.Users.IsEmpty()) {                     //Verifica que la lista de ususarios no está vacia
                    continue;                                   //En caso de que lo está continúa con la ejecucion
                } else if (Node == null) {                      //Verifia si el nodo que tengo es un apuntador nulo
                    Node = this.Users.GetNode(0);               //En ese caso obtiene el primer nodo de la lista
                }

                Write(Node.element, false);                     //Intenta escribir los datos que estén pendientes para ese usuario
                Node = Node.next;                               //Avanza al siguiente nodo
            } while (threads);
        }

        private void ListenConnection() {          
            do {
                try { 
                    if (this.Server.Pending()) {
                        User newUser = new User();
                        newUser.Client = this.Server.AcceptTcpClient();
                        newUser.SetStreams();
                        Read(newUser, true);
                        object obj = newUser.toRead.Dequeue();
                        if (obj is SignIn si) {
                            bool exist = false;
                            this.Oracle.GetOracleDataBase().ExecuteSQL("SELECT USERNAME,CONTRASENA FROM USUARIO");
                            while (this.Oracle.GetOracleDataBase().getDataReader().Read()) {
                                if (this.Oracle.GetOracleDataBase().getDataReader()["USERNAME"].Equals(si.user) && this.Oracle.GetOracleDataBase().getDataReader()["CONTRASENA"].Equals(si.password)) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {
                                newUser.Name = si.user;                                 //Inicializa el nombre de ususario
                                newUser.toWrite.Enqueue(new RequestAnswer(true));       //Agrega la respuesta a la cola de envío
                                Write(newUser, true);                                   //Envía la respuesta
                                this.Users.AddLast(newUser);                            //Agrega el ususario a la de inicialización
                                /*Thread initialize = new Thread(InitializeUser);
                                initialize.Start();*/
                                this.ConsoleAppend("El usuario [" + newUser.Name + " | " + newUser.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                this.InsertTable(newUser.Name, newUser.Client.Client.RemoteEndPoint.ToString());
                            } else {
                                newUser.toWrite.Enqueue(new RequestAnswer(false));       //Agrega la respuesta a la cola de envío
                                newUser.toWrite.Enqueue(new RequestError(1));            //Especifica el error del fallo
                                Write(newUser, true);                                    //Envía la respuesta
                                this.ConsoleAppend("Se ha intentado conectar el remoto [" + newUser.Client.Client.RemoteEndPoint.ToString() + "] con información de inicio de sesión incorrecta.");
                                newUser.Client.Close();
                            }
                        } else if (obj is SignUp su) {
                            RequestAnswer answer;
                            if (this.Oracle.GetOracleDataBase().ExecuteSQL("INSERT INTO USUARIOS VALUES('" + su.name + "','" + /*su.Name*/"b" +"','" + /*su.Password*/"c" + "',SYSDATE)")) {
                                answer = new RequestAnswer(true);
                                newUser.toWrite.Enqueue(answer);
                                Write(newUser, true);
                                this.ConsoleAppend("Se ha registrado el usuario [" + newUser.Name + " | " + newUser.Client.Client.RemoteEndPoint.ToString() + "] correctamente.");
                                this.Users.AddLast(newUser);
                                this.ConsoleAppend("El usuario [" + newUser.Name + " | " + newUser.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                                // TODO: Actualizar Tabla.
                            } else {
                                answer = new RequestAnswer(false);
                                newUser.toWrite.Enqueue(answer);
                                newUser.toWrite.Enqueue(new RequestError(0));
                                Write(newUser, true);
                                this.ConsoleAppend("Se ha intentado registrar el remoto [" + newUser.Client.Client.RemoteEndPoint.ToString() + "] con un nombre de usuario ya existente.");
                                newUser.Client.Close();
                            }
                        } else {
                            this.ConsoleAppend("No corresponde a ninguna clase conocida.");
                        }
                    }
                } catch (Exception) {
                    Console.WriteLine("Con tanto que pudo haber salido mal... pues algo salió mal");
                }
            } while (true);
        }

        /// <summary>
        /// Esta funcion se usa para que cuando un usuario inicie sesión el servidor le envíe unas mensajes, chats y publicaciones para mostrarlos en la interfaz
        /// </summary>
        /*private void InitializeUser() {
            while (!toInitialize.IsEmpty()) {
                User user = toInitialize.Remove(0);
                this.Oracle.GetOracleDataBase().ExecuteSQL("SELECT * FROM MENSAJE_CHAT WHERE USERNAME = " + user.Name + " OR DESTINATARIO = " + user.Name);
                while (this.Oracle.GetOracleDataBase().getDataReader().Read()) {
                    ChatMessage ms = new ChatMessage();
                    ms.Sender = (string)this.Oracle.GetOracleDataBase().getDataReader()["USERNAME"];
                    ms.Receiver = (string)this.Oracle.GetOracleDataBase().getDataReader()["DESTINATARIO"];
                    ms.Content = (string)this.Oracle.GetOracleDataBase().getDataReader()["MENSAJE"];
                    //falta mirar cosikas de cosikas para que no cargue todos los mensajes alv
                    user.toWrite.Enqueue(ms);
                }
                this.Oracle.GetOracleDataBase().ExecuteSQL("SELECT ID_GRUPO FROM PERTENENCIA_GRUPO WHERE USERNAME = " + user.Name);
                LinkedList<int> idGroups = new LinkedList<int>();
                while (this.Oracle.GetOracleDataBase().getDataReader().Read()) {
                    idGroups.Add((int)this.Oracle.GetOracleDataBase().getDataReader()["ID_GRUPO"]);             //Selecciona y añade a la lista todos los id de los grupos a los que pertenezca el usuario
                }
                while (!idGroups.IsEmpty()) {
                    this.Oracle.GetOracleDataBase().ExecuteSQL("SELECT GROUPNAME FROM GRUPO WHERE ID_GRUPO = " + idGroups.Remove(0));
                    while (this.Oracle.GetOracleDataBase().getDataReader().Read()) {

                    }
                }

                Write(user, true);
            }
        }*/

        public void ShutDown() {
            try {
                this.DisconnectSockets();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la conexión de los sockets: \n " + ex);
            }
            try {
                this.Oracle.DisconnectDataBase();
            } catch(Exception ex) {
                this.ConsoleAppend("No se ha podido desconectar la base de datos: \n " + ex);
            }
            this.Connected = false;
            this.ButtonText("Encender Servidor");
        }

        private void ConsoleAppend(string text) {
            if (this.GraphicInterface.LogConsole.InvokeRequired) {
                var d = new DLogConsoleAppend(this.ConsoleAppend);
                this.GraphicInterface.LogConsole.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.LogConsole.AppendText("[" + DateTime.Now.ToString(new CultureInfo("en-GB")) + "] " + text + "\n");
            }
        }

        private void ButtonText(string text) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                var d = new DButtonText(this.ButtonText);
                this.GraphicInterface.Button.Invoke(d, new object[] { text });
            } else {
                this.GraphicInterface.Button.Text = text;
            }
        }

        private void ButtonEnable(bool flag) {
            if (this.GraphicInterface.Button.InvokeRequired) {
                var d = new DButtonEnable(this.ButtonEnable);
                this.GraphicInterface.Button.Invoke(d, new object[] { flag });
            } else {
                this.GraphicInterface.Button.Enabled = flag;
            }
        }

        private void InsertTable(string name, string ip) {
            if (this.GraphicInterface.UsersTable.InvokeRequired) {
                var d = new DDataGridViewRow(this.InsertTable);
                this.GraphicInterface.Button.Invoke(d, new object[] { name, ip });
            } else {
                this.GraphicInterface.UsersTable.Rows.Add(name, ip);
            }
        }

        /// <summary>
        /// Escribe los datos que esten pendientes en la cola toWrite de un ususario
        /// </summary>
        /// <param name="user">El ususario del que se van a intentar escribir los datos</param>
        /// <param name="sendAll">Verdadero si se quiere que se envíen todos los datos en la cola</param>
        /// <returns>Verdadero si los datos fueron enviados, falso si almenos uno falló</returns>
        private bool Write(User user, bool sendAll) {
            try {
                if (!user.toWrite.IsEmpty()) {                                                      //Verifica que la cola no este vacía
                    while (sendAll && !user.toWrite.IsEmpty()) {                                    //Si sendAll es verdadero se ejecuta hasta que la cola está vacía
                        Byte[] toSend = Serializer.Serialize(user.toWrite.GetFrontElement());       //Serializa el primer objeto de la cola
                        user.Writer.Write(toSend.Length);                                           //Envía el tamaño del objeto
                        user.Writer.Write(toSend);                                                  //Envía el objeto
                        user.toWrite.Dequeue();                                                     //Saca el objeto de la cola
                    }
                }
                return true;
            } catch (Exception) {
                user.toWrite.Enqueue(user.toWrite.Dequeue());                                       //En caso de excepción manda el objeto al final de la cola
                return false;
            }
        }

        /// <summary>
        /// Lee los datos que estén pendientes para un usuario y los guarda en la cola toRead del mismo
        /// </summary>
        /// <param name="user">EL ususario del que se van a intentar leer datos</param>
        /// <param name="sendAll">Verdadero si se quiere que se lean todos los datos en disponible</param>
        /// <returns>Verdadero si le leyeron todos los datos, falso si uno de ellos no pudo ser leido</returns>
        private bool Read(User user, bool sendAll) {
            try {
                if (user.Stream.DataAvailable) {                                                //Verefica si hay datos por leer
                    while (sendAll && user.Stream.DataAvailable) {                              //si sendAll es verdadero lee mientras siga habiendo datos por leer
                        int size = user.Reader.ReadInt32();                                     //lee el tamaño del objeto
                        byte[] data = new byte[size];                                           //crea el arreglo de bytes para el objeto
                        data = user.Reader.ReadBytes(size);                                     //lee el el objeto y lo guarda en el arreglo de bytes
                        object a = Serializer.Deserialize(data);                                //deserializa el objeto
                        user.toRead.Enqueue((Data)a);                                           //guarda el objeto en la cola
                    }
                }
                return true;
            } catch (Exception) {
                return false;
            }
        }
    }
}
