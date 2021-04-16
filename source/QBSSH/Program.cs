using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace QBSSH
{
    internal class Program
    {
        private static string _setupLocation = Application.StartupPath + @"\Setup.json";
        private static string _scriptFile = Application.StartupPath + @"\bash.txt";
        private static string _profile;
        private static string _user;
        private static string _password;
        private static string _host;
        private static int _port;
        private static bool _log;
        private static string _delay;
        private static string _filePath;
        private static bool _count;
        private static bool _view;

        private static void Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult
                .WithParsed<Options>(Run)
                .WithNotParsed(errs => DisplayHelp(parserResult, errs));
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = true; //eliminar la nueva línea adicional entre las opciones
                h.Heading = "QBayes SHH + BOT 0.0.3-beta"; //cabecera
                h.Copyright = "Copyright (c) 2021 Quantic"; //texto copyrigt

                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

        private static void Run(Options options)
        {
            _view = options.View;
            _log = options.Log;

            if (!string.IsNullOrEmpty(options.Load))
            {
                if (File.Exists(_setupLocation))
                {
                    LoadConfig(options.Load);
                }
                else
                {
                    Console.WriteLine("No existe el archivo de configuración");
                }
            }

            if (options.Logging)
            {
                if (!string.IsNullOrEmpty(options.User))
                {
                    _user = options.User;
                    if (!string.IsNullOrEmpty(options.Password))
                    {
                        _password = options.Password;
                    }
                    else
                    {
                        Console.WriteLine("Contraseña requerida");
                    }
                }
                else
                {
                    Console.WriteLine("Usuario requerido");
                }

                if (!string.IsNullOrEmpty(options.Host))
                {
                    _host = options.Host;
                    if (options.Port >= 0)
                    {
                        _port = options.Port;
                    }
                    else
                    {
                        Console.WriteLine("Puerto requerido");
                    }
                }
                else
                {
                    Console.WriteLine("Host requerido");
                }

                if (!string.IsNullOrEmpty(options.Profile)) _profile = options.Profile;
            }
            if (options.UpLoad.Any()) UploadFileToSFTP(options.UpLoad);
            if (options.Command.Any()) SendCommands(options.Command);
            if (options.Sudo.Any()) SendSudoByStream(options.Sudo);
            if (options.Script) ReadBashFile();
            if (!string.IsNullOrEmpty(options.Service)) ManageServices(options.Service, options.Status);
            if (options.Save) SaveConfig();
        }

        private static void ManageServices(string service, int value)
        {
            var modes = new Dictionary<TerminalModes, uint>();

            try
            {
                using var client = new SshClient(_host, _port, _user, _password);
                client.Connect();
                client.KeepAliveInterval = TimeSpan.FromSeconds(5);

                if (!client.IsConnected)
                    throw new SshConnectionException("No se pudo conectar al cliente, falló la conexión SSH");
                var i = client.ConnectionInfo;
                Console.WriteLine($"Conectado a: {i.Host}");
                if (_log) Logger.Logger.i($"Conectado a: {i.Host}");

                Console.WriteLine("Servicio: " + service);
                var stream = client.CreateShellStream(@"xterm", 80, 24, 800, 600, 2048, modes);

                var paramList = new List<string> { "stop", "start", "restart", "is-active" };

                stream.WriteLine($"sudo systemctl {paramList[value]} {service}");
                stream.Expect("password");
                stream.WriteLine(_password);

                var output = stream.Expect(new Regex(@"\][a-zA-Z0-9_:.#$/-]"), new TimeSpan(0, 0, 3));
                //var input = Regex.Replace(output, @"\][a-zA-Z0-9_:.#$/-]", "");// Encontrar el patrón para filtrar la salida del comando

                if (output.Contains("\nactive")) output = "Activo";
                if (output.Contains("inactive")) output = "Inactivo";
                if (output.Contains("Failed"))
                {
                    output = $"No se encuentra el servicio {service}";
                }
                else
                {
                    if (paramList[value] == "start") output = $"El servicio {service} se ha iniciado.";
                    if (paramList[value] == "stop") output = $"El servicio {service} se ha detenido.";
                    if (paramList[value] == "restart") output = $"El servicio {service} se ha reiniciado.";
                }

                Console.WriteLine($"Estado: {output}");
                client.Disconnect();
                Console.WriteLine($"Desconectado de: {i.Host}");
                if (_log) Logger.Logger.i($"Desconectado de: {i.Host}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void SendSudoByStream(IEnumerable<string> command)
        {
            var modes = new Dictionary<TerminalModes, uint>();
            try
            {
                using var client = new SshClient(_host, _port, _user, _password);
                client.Connect();
                client.KeepAliveInterval = TimeSpan.FromSeconds(5);

                if (!client.IsConnected)
                    throw new SshConnectionException("No se pudo conectar al cliente, falló la conexión SSH.");
                var i = client.ConnectionInfo;
                Console.WriteLine($"Conectado a: {i.Host}");
                if (_log) Logger.Logger.i($"Conectado a: {i.Host}");
                foreach (var arg in command)
                {
                    Console.WriteLine($"Comando enviado: {arg}");
                    if (_log) Logger.Logger.i($"Comando enviado: {arg}");
                    var stream = client.CreateShellStream(@"xterm", 80, 24, 800, 600, 2048, modes);

                    stream.WriteLine($"sudo {arg}");
                    stream.Expect("password", TimeSpan.FromSeconds(3));
                    stream.WriteLine(_password);
                    var output = stream.Expect(new Regex(@"\][a-zA-Z0-9_:.#$/-]"), TimeSpan.FromSeconds(3));

                    Console.WriteLine($"Retorno: {output}");
                    if (_log) Logger.Logger.i($"Retorno: {output}");
                }
                client.Disconnect();
                Console.WriteLine($"Desconectado de: {i.Host}");
                if (_log) Logger.Logger.i($"Desconectado de: {i.Host}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void SendCommands(IEnumerable<string> command)
        {
            try
            {
                using var client = new SshClient(_host, _port, _user, _password);
                client.Connect();
                client.KeepAliveInterval = TimeSpan.FromSeconds(5);
                if (!client.IsConnected)
                    throw new SshConnectionException("No se pudo conectar al cliente, falló la conexión SSH");
                var i = client.ConnectionInfo;
                Console.WriteLine($"Conectado a: {i.Host}");
                if (_log) Logger.Logger.i($"Conectado a: {i.Host}");

                foreach (var arg in command)
                {
                    Console.WriteLine($"Comando enviado: {arg}");
                    if (_log) Logger.Logger.i($"Comando enviado: {arg}");
                    var cmd = client.CreateCommand(arg);

                    var asynch = cmd.BeginExecute();
                    var reader = new StreamReader(cmd.OutputStream);
                    var result = "";

                    while (!asynch.IsCompleted)
                    {
                        result = reader.ReadToEnd();
                        if (string.IsNullOrEmpty(result))
                            continue;
                        Console.WriteLine($"{i.Username} @ {i.Host} _:$ {RemoveEmptyLines(result)}");
                        if (_log) Logger.Logger.i($"{i.Username} @ {i.Host} _:$ {RemoveEmptyLines(result)}");
                    }
                }
                client.Disconnect();
                Console.WriteLine($"Desconectado de: {i.Host}");
                if (_log) Logger.Logger.i($"Desconectado de: {i.Host}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new SshConnectionException("No se pudo conectar al cliente, falló la conexión SSH");
            }
        }

        private static string RemoveEmptyLines(string lines)
        {
            return Regex.Replace(lines, @"^\s*$\n\t|\r", string.Empty, RegexOptions.Multiline).TrimEnd();
        }

        private static void Sleep(int value)
        {
            Thread.Sleep(value);
        }

        private static void UploadFileToSFTP(IEnumerable<string> filePaths)
        {
            try
            {
                using var client = new SftpClient(_host, _port, _user, _password);
                client.Connect();
                var i = client.ConnectionInfo;
                if (client.IsConnected)
                {
                    Console.WriteLine($"Conectado a: {i.Host}");
                    if (_log) Logger.Logger.i($"Conectado a: {i.Host}");
                    foreach (var filePath in filePaths)
                    {
                        using var fileStream = new FileStream(filePath, FileMode.Open);
                        client.BeginUploadFile(fileStream,  Path.GetFileName(filePath), null, null);
                        Console.WriteLine("Archivo enviado: {0} ({1:N0} bytes)", filePath, fileStream.Length);
                        if (_log) Logger.Logger.i($"Archivo enviado: {filePath}");
                        client.BufferSize = 4 * 1024; // bypass Payload error large files
                        client.UploadFile(fileStream, Path.GetFileName(filePath));
                    }
                    client.Disconnect();
                    Console.WriteLine($"Desconectado de: {i.Host}");
                    if (_log) Logger.Logger.i($"Desconectado de: {i.Host}");
                }
                else
                {
                    Debug.WriteLine("No se pudo conectar al cliente.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ReadBashFile() //Parte de función sin testear
        {
            var counter = 0;
            string line;
            // Lee el archivo y muestra línea por línea

            var file = new StreamReader(_scriptFile);

            while ((line = file.ReadLine()) != null)
            {
                switch (line)
                {
                    case "#":
                    {
                        break;
                    }
                    case "sleep":
                        {
                            var delay = 100;
                            Console.WriteLine($"Esperando {delay} milisegundos...");
                            Sleep(delay);
                            break;
                        }
                    case "sudo":
                        {
                            SendSudoByStream(line.SplitArgs());//No testeado
                            break;
                        }
                    default:
                        SendCommands(line.SplitArgs());
                        counter++;
                        break;
                }
            }

            file.Close();
            Console.WriteLine($"Se ejecutaron {counter} líneas.");
        }

        public class Profile
        {
            [JsonProperty("Name")] public string Name { get; set; }

            [JsonProperty("User")] public string User { get; set; }

            [JsonProperty("Password")] public string Password { get; set; }

            [JsonProperty("Host")] public string Host { get; set; }

            [JsonProperty("Port")] public int Port { get; set; }
        }

        private static IList<Profile> ListProfilesFile = new List<Profile>();

        private static void SaveConfig()
        {
            if (!File.Exists(_setupLocation))
            {
                using var initArray = new StreamWriter(_setupLocation, true);
                initArray.WriteLine("[]");
            }

            using var jsonStream = File.OpenText(_setupLocation);
            var json = jsonStream.ReadToEnd();

            jsonStream.Close();
            json.Replace("\n", "");
            json.Replace("\r", "");
            json = json.TrimStart('\"');
            json = json.TrimEnd('\"');
            json = json.Replace("\\", "");

            var root = JToken.Parse(json);
            var values = root.Where(t => (string)t["Name"] == _profile).ToList();

            ListProfilesFile = JsonConvert.DeserializeObject<List<Profile>>(root.ToString());
            var pro = new Profile
            {
                Name = _profile,
                Host = _host,
                Port = _port,
                User = _user,
                Password = _password
            };
            if (values.Count == 1)
            {
                Console.WriteLine($"Ya existe un perfil con el nombre {_profile}, reintente con otro nombre.");
            }
            else
            {
                ListProfilesFile?.Add(pro); //Nuevo valor de Objeto
            }

            var output = JsonConvert.SerializeObject(ListProfilesFile);
            using var file = File.CreateText(_setupLocation);
            var serializer = new JsonSerializer();
            serializer.Serialize(file, output);
        }

        private static void LoadConfig(string profileName)
        {
            try
            {
                if (File.Exists(_setupLocation))
                {
                    using var jsonStream = File.OpenText(_setupLocation);
                    var json = jsonStream.ReadToEnd();

                    json.Replace("\n", "");
                    json.Replace("\r", "");
                    json = json.TrimStart('\"');
                    json = json.TrimEnd('\"');
                    json = json.Replace("\\", "");

                    var root = JToken.Parse(json);
                    var values = root.Where(t => (string)t["Name"] == profileName).ToList();

                    foreach (var value in values.Select(val => JsonConvert.DeserializeObject<Profile>(val.ToString())))
                    {
                        _profile = value.Name;
                        _host = value.Host;
                        _port = value.Port;
                        _user = value.User;
                        _password = value.Password;

                        Console.WriteLine($"\nPerfil: {value.Name}");
                    }

                    if (!_view) return;
                    if (_profile == null)
                    {
                        Console.WriteLine("Perfil no encontrado");
                    }
                    else if (_profile != null)
                    {
                        Console.WriteLine($"IP: {_host}\nPuerto: {_port}\nUsuario: {_user}\nContraseña: {_password}");
                    }
                }
                else
                {
                    Console.WriteLine("No se encuentra el archivo de configuración.");
                }
            }
            catch (SshConnectionException e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Clase para establecer opciones personalizadas de parámetros.
        /// </summary>
        private class Options
        {
            [Option('c',"command",
                Required = false,
                HelpText = "--command [Comando UNIX, Comando ...]")]
            public IEnumerable<string> Command { get; set; }

            [Option('h', "host",
                Required = false,
                HelpText = "--host | Dirección IP o nombre del host.")]
            public string Host { get; set; }

            [Option(
                Required = false,
                HelpText = "--log | Imprime el primer mensaje en salida estándar a un archivo de texto.")]
            public bool Log { get; set; }

            [Option(
                Required = false,
                HelpText =
                    "--logging --user 'usuario' --password 'contraseña' --host 'ip' --port 'puerto' | Establece el inicio de sesión por medio de parámetros.")]
            public bool Logging { get; set; }

            [Option('p', "password",
                Required = false,
                HelpText = "--password 'Contraseña' | Contraseña de usuario.")]
            public string Password { get; set; }

            [Option(
                Required = false,
                HelpText = "--port 'Puerto' | Puerto de conexión del host.")]
            public int Port { get; set; }

            [Option(
                Required = false,
                HelpText = "--profile 'Nombre' | Nombre del perfil de configuración.")]
            public string Profile { get; set; }

            [Option('s',"save",
                Required = false,
                HelpText = "--save | Guarda el perfil de configuración en un archivo.")]
            public bool Save { get; set; }

            [Option('u', "upload",
                Required = false,
                HelpText = "--send ['Ruta del archivo'] | Enviar archivo a través de ssh.")]
            public IEnumerable<string> UpLoad { get; set; }

            [Option(
                Required = false,
                HelpText = "--script | Usa archivo de entradas a procesar.")]
            public bool Script { get; set; }

            [Option(
                Required = false,
                HelpText = "--service 'sevicio' --status [opción] | Estado del servicio.")]
            public string Service { get; set; }

            [Option(
                Required = false,
                HelpText =
                    "--status [0 : Detener || 1 : Iniciar || 2 : Reiniciar || 3 : Estado | Verifica el estado de un servicio.")]
            public int Status { get; set; }

            [Option(
                Required = false,
                HelpText =
                    "--Sudo [Comando UNIX, Comando ...] | Envía los comandos con elevación hacia la terminal remota en modo interactivo.")]
            public IEnumerable<string> Sudo { get; set; }

            [Option('u', "user",
                Required = false,
                HelpText = "--user 'Nombre de usuario'")]
            public string User { get; set; }

            [Option(
                Required = false,
                HelpText = "--load 'perfil'| Carga una configuración guardada en el archivo de configuración.")]
            public string Load { get; set; }

            [Option(
                Required = false,
                HelpText = "--view | Muestra los perfiles almacenados cuando se usa el parámetro --load --view")]
            public bool View { get; set; }
        }
    }
}