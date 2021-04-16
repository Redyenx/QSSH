using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IniLib;
using QB_SSH.Classes;
using QB_SSH.Properties;
using Renci.SshNet;

namespace QB_SSH
{
    public partial class Main : Form
    {
        private static readonly Inifile _iniFile = new Inifile(Application.StartupPath + @"\Setup.ini");
        private static readonly IniRead IniRead = new IniRead();
        public Main()
        {
            InitializeComponent();
        }

        public void Test_Execute_SingleCommand()
        {
            var host = IniRead.Host;
            var port = IniRead.Port;
            var username = IniRead.User;
            var password = IniRead.Password;

            using (var client = new SshClient(host,2222, username, password))
            {
                #region Example SshCommand CreateCommand Execute
                client.Connect();

                var testValue = Guid.NewGuid().ToString();
                var command = $"echo {testValue}";
                var cmd = client.CreateCommand(command);
                var result = cmd.Execute();
                result = result.Substring(0, result.Length - 1);    //  Remove \n character returned by command

                client.Disconnect();
                #endregion
                Console.WriteLine(result.Equals(testValue));
                //Assert.IsTrue(result.Equals(testValue));
            }
        }

        public void SendCommand(string hostName, int portval, string user, string pass, string commandIn)
        {
            var host = hostName;
            var port = portval;
            var username = user; 
            var password = pass;
            var command = commandIn;

            using (var client = new SshClient(host, port, username, password))
            {

                client.Connect();

                var cmd = client.CreateCommand(command);   //  very long list
                var asynch = cmd.BeginExecute();
    
                var reader = new StreamReader(cmd.OutputStream);

                while (!asynch.IsCompleted)
                {
                    var result = reader.ReadToEnd();
                    if (string.IsNullOrEmpty(result))
                        continue;
                    Console.Write(result);
                    OutputTxt.AppendText(Environment.NewLine + " " + DateTime.Now + " " + " " + user + "@" + hostName + ":-$ " + result);
                    OutputTxt.ScrollToCaret();
                }
                cmd.EndExecute(asynch);

                client.Disconnect();

                //Console.WriteLine();

            }
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            Test_Execute_SingleCommand();
        }

        private void SendBtn_Click(object sender, EventArgs e)
        {
            SendCommand(HostNameTxt.Text, int.Parse(PortTxt.Text), UserTxt.Text, PasswordTxt.Text, CommandTxt.Text);
        }

        private void Main_Load(object sender, EventArgs e)
        {
            UserTxt.Text = IniRead.User;
            PasswordTxt.Text = IniRead.Password;
            HostNameTxt.Text = IniRead.Host;
            PortTxt.Text = IniRead.Port.ToString();
            DelayTxt.Text = IniRead.Delay.ToString();
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            _iniFile.WriteString("SSH", "User", UserTxt.Text);
            _iniFile.WriteString("SSH", "Password", PasswordTxt.Text);
            _iniFile.WriteString("SSH", "Host", HostNameTxt.Text);
            _iniFile.WriteString("SSH", "Port", PortTxt.Text);
            _iniFile.WriteString("Options", "Delay", DelayTxt.Text);
        }

        
    }
}