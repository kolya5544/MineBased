using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTUDPDispatcher
{
    public class Database
    {
        public List<RegisteredUser> users = new();

        public static Database LoadDatabase(string filepath)
        {
            if (File.Exists(filepath))
            {
                var d = JsonConvert.DeserializeObject<Database>(File.ReadAllText(filepath));
                return d;
            }
            else
            {
                Database db = new Database();
                db.SaveDatabase(filepath);
                return db;
            }
        }

        public void SaveDatabase(string filepath)
        {
            File.WriteAllText(filepath + ".backup", JsonConvert.SerializeObject(this));
            File.Delete(filepath);
            File.Copy(filepath + ".backup", filepath);
        }
    }

    public class RegisteredUser
    {
        public string username;
        public byte[] verifier;
        public byte[] salt;
    }
}
