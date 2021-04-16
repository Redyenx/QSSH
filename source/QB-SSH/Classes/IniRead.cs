using System.Windows.Forms;
using IniLib;

namespace QB_SSH.Classes
{
    public class IniRead
    {
        private readonly Inifile _inifile = new Inifile(Application.StartupPath + @"\Setup.ini");

        public string User => _inifile.GetString("SSH", "User", "");
        public string Password => _inifile.GetString("SSH", "Password", "");
        public string Host => _inifile.GetString("SSH", "Host", "");
        public int Port => _inifile.GetInteger("SSH", "Port", 0);
        public int Delay => _inifile.GetInteger("SSH", "Delay", 0);

        public string OutputFile;
    }
}