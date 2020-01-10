using System.IO;
using System.Net.Sockets;
using DataStructures;
using ShippingData;
using System.Threading;
using System;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Chat_Virtual___Servidor {
    public class User {
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }
        private readonly LinkedQueue<Data> WritingQueue;
        private readonly LinkedQueue<Data> ReadingQueue;
        private readonly Semaphore CanWrite;
        private readonly Semaphore CanRead;

        private static Semaphore Regulator;

        public User() {
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
            if (Regulator == null)
                Regulator = new Semaphore(6, 6);
        }
        public User(string Name) {
            this.Name = Name;
            this.Client = new TcpClient();
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
            if (Regulator == null)
                Regulator = new Semaphore(6, 6);
        }

        public User(TcpClient Client) {
            RefreshStreams(Client);
            this.ReadingQueue = new LinkedQueue<Data>();
            this.WritingQueue = new LinkedQueue<Data>();
            this.CanWrite = new Semaphore(1, 1);
            this.CanRead = new Semaphore(1, 1);
            if (Regulator == null)
                Regulator = new Semaphore(6, 6);
        }

        public void WritingEnqueue(Data data) {
            this.CanWrite.WaitOne();
            this.WritingQueue.Enqueue(data);
            this.CanWrite.Release();
        }

        public void RefreshStreams(TcpClient Client) {
            IsActive = false;
            this.Client = Client;
            this.Stream = Client.GetStream();
            this.Writer = new BinaryWriter(this.Stream);
            this.Reader = new BinaryReader(this.Stream);
            IsActive = true;
            Thread t = new Thread(ExecuteRequest);
            t.Start();
        }

        public void ReadingEnqueue(Data data) {
            IsActive = false;
            this.CanRead.WaitOne();
            this.ReadingQueue.Enqueue(data);
            this.CanRead.Release();
            IsActive = true;
        }

        public Data WritingDequeue() {
            this.CanWrite.WaitOne();
            Data data = this.WritingQueue.Dequeue();
            this.CanWrite.Release();
            return data;
        }

        public Data ReadingDequeue() {
            this.CanRead.WaitOne();
            Data data = this.ReadingQueue.Dequeue();
            this.CanRead.Release();
            return data;
        }

        public void Disconnect() {
            Client.Close();
            ServerConnection.Users.Remove(Name);
            IsActive = false;
        }

        /// <summary>
        /// Método de Hilo. Redirige los mensaje y responde a las peticiones de los usuarios
        /// </summary>
        private void ExecuteRequest() {
            while (IsActive) {
                Regulator.WaitOne();
                Write();
                Read();

                Data Readed = ReadingDequeue();
                if (Readed == null) {
                    Thread.Sleep(100);
                    Regulator.Release();
                    continue;
                }
                DataBaseConnection Oracle = ServerConnection.Oracle;
                if (Readed is SignIn si) {                                             // Si el objeto recibido es un inicio de sesión.
                    bool exist = false;
                    ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT * FROM INFOMRACION_INICIO");
                    while (ServerConnection.Oracle.Oracle.DataReader.Read()) {
                        if (ServerConnection.Oracle.Oracle.DataReader["USUARIO"].Equals(si.user) && ServerConnection.Oracle.Oracle.DataReader["CONTRASEÑA"].Equals(si.password)) {
                            exist = true;
                            break;
                        }
                    }
                    if (exist) {                                                    // Si la información del cliente corresponde con la de la base de datos.
                        Name = si.user;
                        WritingEnqueue(new RequestAnswer(true));
                        ServerConnection.Users.AddElement(this.Name, this);
                        //this.ConsoleAppend("El usuario [" + U.Name + " | " + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] se ha conectado satisfactoriamente.");
                        //this.InsertTable(U.Name, IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                        Profile profile = new Profile();
                        ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
                        ServerConnection.Oracle.Oracle.DataReader.Read();
                        string path = ServerConnection.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
                        ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
                        ServerConnection.Oracle.Oracle.DataReader.Read();
                        string status = ServerConnection.Oracle.Oracle.DataReader["ESTADO"].ToString();
                        using (FileStream stream = File.Open(path, FileMode.Open)) {
                            profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                        }
                        profile.Status = status;
                        profile.Name = Name;
                        WritingEnqueue(profile);

                        TreeActivities tree = new TreeActivities();
                        ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_ARBOL FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
                        ServerConnection.Oracle.Oracle.DataReader.Read();
                        string treePath = ServerConnection.Oracle.Oracle.DataReader["RUTA_ARBOL"].ToString();
                        if (treePath != "") {
                            IFormatter formatter = new BinaryFormatter();
                            using (FileStream stream = File.Open(treePath, FileMode.Open, FileAccess.Read)) {
                                tree.Node = (TreeNode[])formatter.Deserialize(stream);
                                stream.Close();
                            }
                            WritingEnqueue(tree);
                        }
                    } else {                                                                                        // Si la infomación de inicio de sesión es incorrecta.
                        WritingEnqueue(new RequestAnswer(false));
                        WritingEnqueue(new RequestError(1));
                        //this.ConsoleAppend("Se ha intentado conectar el remoto [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] con información de inicio de sesión incorrecta.");
                        Disconnect();
                    }
                } else if (Readed is SignUp su) {                                                                      // Si el objeto recibido es un nuevo registro
                    if (ServerConnection.Oracle.Oracle.ExecuteSQL("INSERT INTO USUARIOS VALUES('" + su.userName + "', '" + su.name + "', '" + su.password + "', 'Hey there! I am using SADIRI.','F:\\SADIRI\\Usuarios\\default.png', null, default)")) {
                        this.Name = su.userName;
                        WritingEnqueue(new RequestAnswer(true));
                        //this.ConsoleAppend("Se ha registrado el usuario [" + U.Name + " | " + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] correctamente.");
                        ServerConnection.Users.AddElement(Name, this);
                        //this.ConsoleAppend("El usuario [" + U.Name + " | " + U.Client.Client.RemoteEndPoint.ToString() + "] se ha conectado satisfactoriamente.");
                        //this.InsertTable(U.Name, IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()).ToString());

                        Profile profile = new Profile();
                        ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT RUTA_FOTO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
                        ServerConnection.Oracle.Oracle.DataReader.Read();
                        string path = ServerConnection.Oracle.Oracle.DataReader["RUTA_FOTO"].ToString();
                        ServerConnection.Oracle.Oracle.ExecuteSQL("SELECT ESTADO FROM USUARIOS WHERE USUARIO = '" + this.Name + "'");
                        ServerConnection.Oracle.Oracle.DataReader.Read();
                        string status = ServerConnection.Oracle.Oracle.DataReader["ESTADO"].ToString();
                        using (FileStream stream = File.Open(path, FileMode.Open)) {
                            profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                            stream.Close();
                        }
                        profile.Status = status;
                        profile.Name = this.Name;
                        WritingEnqueue(profile);

                    } else {
                        WritingEnqueue(new RequestAnswer(false));
                        WritingEnqueue(new RequestError(0));
                        Write();
                        //this.ConsoleAppend("Se ha intentado registrar el remoto [" + IPAddress.Parse(((IPEndPoint)U.Client.Client.RemoteEndPoint).Address.ToString()) + "] con un nombre de usuario ya existente.");
                        Disconnect();
                    }
                } else if (Readed is Chat ch) {                                                             // Si se desea obtener la información de un Chat privado.
                    Oracle.Oracle.ExecuteSQL("SELECT USUARIO, ESTADO, RUTA_FOTO " +
                        "FROM USUARIOS " +
                        "WHERE USUARIO = '" + ch.memberTwo.Name + "'");
                    if (Oracle.Oracle.DataReader.Read()) {
                        Profile profile = new Profile {
                            Name = Oracle.Oracle.DataReader["USUARIO"].ToString(),
                            Status = Oracle.Oracle.DataReader["ESTADO"].ToString()
                        };
                        using (FileStream stream = File.Open(Oracle.Oracle.DataReader["RUTA_FOTO"].ToString(), FileMode.Open)) {
                            profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                            stream.Close();
                        }
                        WritingEnqueue(new Chat(ch.memberOne, profile));
                    }
                } else if (Readed is ChatMessage ms) {                                                      // Si se envía un mensaje en privado.
                    ms.date = new ShippingData.Message.Date(DateTime.Now);
                    if (ms.Image == null) {
                        var a = Oracle.Oracle.ExecuteSQL("INSERT INTO MENSAJES_CHAT VALUES (DEFAULT, '" + ms.Sender + "', '" + ms.Receiver
                        + "', DEFAULT, '" + ms.Content + "', NULL)");
                        if (!a) {
                            ;
                        }
                    } else {
                        var a = Oracle.Oracle.ExecuteSQL("INSERT INTO MENSAJES_CHAT VALUES (DEFAULT, '" + ms.Sender + "', '" + ms.Receiver
                        + "', DEFAULT, '" + ms.Content + "', 'F:\\SADIRI\\MensajesChat\\')");
                        Image picture = Serializer.DeserializeImage(ms.Image);

                        Oracle.Oracle.ExecuteSQL("SELECT MAX(ID_MENSAJE) AS MAX FROM MENSAJES_CHAT");
                        Oracle.Oracle.DataReader.Read();

                        string path = "F:\\SADIRI\\MensajesChat\\" + Oracle.Oracle.DataReader["MAX"].ToString() + ".png";
                        using (FileStream stream = File.Open(path, FileMode.Create)) {
                            picture.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            stream.Close();
                        }
                    }
                    User theOther;
                    if (Name.Equals(ms.Sender)) {
                        theOther = ServerConnection.Users.Search(ms.Receiver);
                    } else {
                        theOther = ServerConnection.Users.Search(ms.Sender);
                    }
                    WritingEnqueue(ms);
                    theOther?.WritingEnqueue(ms);
                    //this.ConsoleAppend("Mensaje  [" + ms.Sender + "] a [" + ms.Receiver + "]: " + ms.Content);
                } else if (Readed is ChatGroup group) {                                                     // Si se desea obtener la información de un grupo.

                } else if (Readed is GroupMessage groupMessage) {                                           // Si se envía un mensaje en un grupo.
                                                                                                            //Carpeta: F:\\SADIRI\\MensajesGrupos\\
                } else if (Readed is Profile profile) {                                                     // Si es un cambio de perfil.
                    if (profile.Status != null) {
                        Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET ESTADO = '" + profile.Status + "' WHERE USUARIO = '" + profile.Name + "'");
                        WritingEnqueue(new RequestAnswer(true));
                    }
                    if (profile.Image != null) {
                        Image foto = Serializer.DeserializeImage(profile.Image);
                        string path = "F:\\SADIRI\\Usuarios\\" + profile.Name + ".png";
                        using (FileStream stream = File.Open(path, FileMode.Create)) {
                            foto.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            stream.Close();
                        }
                        Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET RUTA_FOTO = 'F:\\SADIRI\\Usuarios\\" + profile.Name + ".png' WHERE USUARIO = '" + profile.Name + "'");
                        WritingEnqueue(new RequestAnswer(true)); //TODO: Código respuesta.
                    }
                    //this.ConsoleAppend("Se ha cambiado satisfactoriamente el perfil del usuario  [" + User.Name + " | " + IPAddress.Parse(((IPEndPoint)User.Client.Client.RemoteEndPoint).Address.ToString()) + "]. ");
                } else if (Readed is ChangePassword changePassword) {                                       // Si es una solicitud de cambio de contraseña.
                    Oracle.Oracle.ExecuteSQL("SELECT CONTRASEÑA FROM USUARIOS WHERE USUARIO = '" + changePassword.UserName + "'");
                    Oracle.Oracle.DataReader.Read();
                    string currentPassword = Oracle.Oracle.DataReader["CONTRASEÑA"].ToString();
                    if (!changePassword.CurrentPassword.Equals(currentPassword)) {
                        WritingEnqueue(new RequestAnswer(false, 3));
                        WritingEnqueue(new RequestError(3));
                        //this.ConsoleAppend("El cambio contraseña del usuario  [" + User.Name + " | " + IPAddress.Parse(((IPEndPoint)User.Client.Client.RemoteEndPoint).Address.ToString()) + "]. no pudo ser realizado.");
                    } else {
                        Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET CONTRASEÑA = '" + changePassword.NewPassword + "' WHERE USUARIO = '" + changePassword.UserName + "'");
                        //this.ConsoleAppend("Se ha cambiado satisfactoriamente la contraseña del usuario  [" + User.Name + " | " + User.Client.Client.RemoteEndPoint.ToString() + "]. ");
                        WritingEnqueue(new RequestAnswer(true, 3));
                    }
                } else if (Readed is TreeActivities tree) {                                                 // Si es una actualización del arbol de tareas.
                    string path = "F:\\SADIRI\\ArbolesTareas\\" + Name + ".dat";
                    IFormatter formatter = new BinaryFormatter();
                    using (FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
                        formatter.Serialize(stream, tree.Node);
                        stream.Close();
                    }
                    Oracle.Oracle.ExecuteSQL("UPDATE USUARIOS SET RUTA_ARBOL = '" + path + "' WHERE USUARIO = '" + Name + "'");
                    //this.ConsoleAppend("Se ha guardado satisfactoriamente el árbol de tareas del usuario  [" + User.Name + " | " + IPAddress.Parse(((IPEndPoint)User.Client.Client.RemoteEndPoint).Address.ToString()) + "]. ");
                } else if (Readed is Search search) {
                    if (search.ToSearch == ToSearch.Chat) {
                        Oracle.Oracle.ExecuteSQL(
                            "SELECT USUARIO, ESTADO, RUTA_FOTO " +
                            "FROM USUARIOS " +
                            "WHERE LOWER(USUARIO) LIKE '%" + search.StringToSearch.ToLower() + "%'");
                        LinkedList<Profile> Results = new LinkedList<Profile>();
                        while (Oracle.Oracle.DataReader.Read()) {
                            Profile Profile = new Profile {
                                Name = Oracle.Oracle.DataReader["USUARIO"].ToString(),
                                Status = Oracle.Oracle.DataReader["ESTADO"].ToString()
                            };
                            using (FileStream stream = File.Open(Oracle.Oracle.DataReader["RUTA_FOTO"].ToString(), FileMode.Open)) {
                                Profile.Image = Serializer.SerializeImage(Image.FromStream(stream));
                                stream.Close();
                            }
                            Results.Add(Profile);
                        }
                        WritingEnqueue(new ChatsResult(Results.ToArray()));
                    } else if (search.ToSearch == ToSearch.Group) {

                    } else {

                    }
                }
                Regulator.Release();
            }
        }



        /// <summary>
        /// Escribe los datos que esten pendientes en la cola WritingQueue de un ususario.
        /// </summary>
        /// <param name="user">El ususario del que se van a intentar escribir los datos.</param>
        /// <returns>Verdadero si los datos fueron enviados, falso si almenos uno falló.</returns>
        public bool Write() {
            Data data = WritingDequeue();
            if (data == default) {
                return false;
            } else {
                try {
                    byte[] toSend = Serializer.Serialize(data);                                      // Serializa el primer objeto de la cola.
                    Writer.Write(toSend.Length);                                                // Envía el tamaño del objeto.
                    Writer.Write(toSend);                                                       // Envía el objeto.     
                    return true;
                } catch (Exception ex) {
                    //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                    //this.ConsoleAppend(ex.Message);
                    WritingEnqueue(data);
                    return false;
                }
            }
        }

        /// <summary>
        /// Lee los datos que estén pendientes para un usuario y los guarda en la cola ReadingQueue del mismo.
        /// </summary>
        /// <param name="user">EL ususario del que se van a intentar leer datos.</param>
        /// <returns>Verdadero si le leyeron todos los datos, falso si uno de ellos no pudo ser leido</returns>
        public bool Read() {
            try {
                if (Stream.DataAvailable) {                                            // Verifica si hay datos por leer.
                    int size = Reader.ReadInt32();                                     // Lee el tamaño del objeto.
                    byte[] data = new byte[size];                                           // Crea el arreglo de bytes para el objeto.
                    data = Reader.ReadBytes(size);                                     // Lee el el objeto y lo guarda en el arreglo de bytes.
                    object a = Serializer.Deserialize(data);                                // Deserializa el objeto.
                    ReadingEnqueue((Data)a);                                           // Guarda el objeto en la cola de lectura.
                }
                return true;
            } catch (Exception) {
                //this.ConsoleAppend("Se ha perdido la conexión con el usuario [" + user.Name + "] Intentando reconectar.");
                return false;
            }
        }
    }
}
