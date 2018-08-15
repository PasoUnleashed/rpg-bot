using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DiscordRpg
{
    static class AssetManager
    {
        public enum AssetType
        {
            npc,item,interaction,effect
        }
        static void CreateIDFile(string dir,AssetType type)

        {
            string filename = type.ToString() + ".id";
            string fullname = dir + "/" + filename;
            File.Create(fullname).Close() ;
            using (FileStream fs = new FileStream(fullname, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("0");
                }
                
            }

        }
        static ulong ReadIDEnums (string dir,AssetType type){
            string filename = type.ToString() + ".id";
            string fullname = dir + "/" + filename;
            if (!File.Exists(fullname))
            {
                CreateIDFile(dir, type);
            }
            ulong id = 0;
            using (FileStream fs = new FileStream(fullname, FileMode.Open))
            {
                using (StreamReader r = new StreamReader(fs))
                {
                     id = ulong.Parse(r.ReadToEnd());
                }
            }
            return id;
        } 
        static void WriteIDEnums(string dir, AssetType type, ulong value)
        {
            string filename = type.ToString() + ".id";
            string fullname = dir + "/" + filename;
            using (FileStream fs = new FileStream(fullname, FileMode.Truncate))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(value);
                }
            }
        }
        public static ulong GenerateInstanceId(AssetType type)
        {
            var id = ReadIDEnums("assets",type);
            id++;
            WriteIDEnums("assets", type, id);
            return id;
        }
        public static void Load(string dir)
        {
            Interactions.Load(dir + "/ias");
            ItemBehaviours.Load(dir + "/eas");
            NPCs.Load(dir + "/npcs.csv");
            Items.Load(dir+"/items.csv");
            
        }
        public static string InteractionScript(string dir,int id)
        {
            string fullpath = dir + "/" + id + ".js";
            using (FileStream fs = new FileStream(fullpath, FileMode.Open))
            {
                using (StreamReader r = new StreamReader(fs))
                {
                    return r.ReadToEnd();
                }
            }
        }
        public static class NPCs
        {
            private static Dictionary<int, NPC> npcs = new Dictionary<int, NPC>();

            public static Dictionary<int, NPC> Npcs { get => npcs; }

            public static void Load(string dir)
            {
                string fullcsv;
                using (FileStream f = new FileStream(dir, FileMode.Open))
                {
                    using (StreamReader r = new StreamReader(f))
                    {
                        fullcsv = r.ReadToEnd();
                        fullcsv.Trim('\r', '\n');
                        fullcsv.Trim('\n', '\r');
                    }
                }
                var lines = fullcsv.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var line in lines)
                {
                    var split = line.Split('\t');
                    int id = int.Parse(split[0]), interactionid = int.Parse(split[2]);
                    string name = split[1];
                    InteractionTree t = Interactions.InteractionSet[interactionid];
                    bool isAutoSpawn = bool.Parse(split[3]);
                    npcs.Add(id, new NPC(id, t, name, isAutoSpawn));
                }

            }
            public static NPCInstance CreateInstance(int id,Room room,RpgGuild guild)
            {
                if(NPCs.npcs.TryGetValue(id,out NPC npc))
                {
                    NPCInstance c = new NPCInstance("", AssetManager.GenerateInstanceId(AssetType.npc), npc,room, guild);
                    Console.WriteLine("Created npc instance: " + c.Id);
                    return c;
                }
                return null;
                
            }
        }
        public static class ItemBehaviours
        {
            private static Dictionary<int, ItemBehaviour> behaviours = new Dictionary<int, ItemBehaviour>();

            public static Dictionary<int, ItemBehaviour> Behaviours { get => behaviours; set => behaviours = value; }

            public static void Load(string dir)
            {
                System.IO.DirectoryInfo d = new DirectoryInfo(dir);
                var files = d.GetFiles();
                foreach (var file in files)
                {
                    Console.WriteLine(file.Extension);
                    if (file.Extension == ".edf")
                    {
                        var name = file.Name.Remove(file.Name.Length - 4);
                        if (int.TryParse(name, out int id))
                        {
                            using(FileStream fs = new FileStream(file.FullName, FileMode.Open))
                            {
                                using (StreamReader r = new StreamReader(fs))
                                {
                                    var es = ParseFile(r.ReadToEnd(),int.Parse(name));
                                    Console.WriteLine("Loaded itembh:" + es.Id);
                                    behaviours.Add(es.Id, es);
                                }
                            }
                        }
                    }
                }
            }
            public static EffectInstance CreateInstance(ulong id)
            {
                throw new NotImplementedException();
            }
            private static ItemBehaviour ParseFile(string file,int id)
            {
                string[] lines = file.Split(new string[] {"\r\n" },StringSplitOptions.RemoveEmptyEntries);
                List<Effect> effects = new List<Effect>();
                foreach(var line in lines)
                {
                    effects.Add(ParseEffect(line));
                }
                return new ItemBehaviour(effects.ToArray(), id);
            }
            private static Effect ParseEffect(string line)
            {
                string[] segments = line.Split(':');
                for(int i = 0; i < segments.Length; i++)
                {
                    segments[i] = segments[i].Trim().Trim('\n').Trim('\r').Trim('\n');
                }
                string type = segments[1];
                int id = int.Parse(segments[0]);
                Effect ret = null;
                switch (type.ToLower())
                {
                    case "d":
                        Console.WriteLine("parsed:" + int.Parse(segments[3]));
                        ret = new CombatEffect(-1 * int.Parse(segments[3]), 1, 1, id);
                        break;
                    case "h":
                        ret = new CombatEffect( int.Parse(segments[3]), 1, 1, id);
                        break;
                    case "e":
                        ret = new VoiceEntryEffect(segments[2], id);
                        break;
                    case "s":
                        ret = new ScriptEffect(int.Parse(segments[2]), id);
                        break;

                }
                return ret;
            }
            
        }
        
        public static class Items
        {
            private static Dictionary<int, Item> items = new Dictionary<int, Item>();
            public static void Load(string dir)
            {
                string fullcsv;
                using (FileStream f = new FileStream(dir, FileMode.Open))
                {
                    using (StreamReader r = new StreamReader(f))
                    {
                        fullcsv = r.ReadToEnd();
                        fullcsv.Trim('\r', '\n');
                        fullcsv.Trim('\n', '\r');
                    }
                }
                var lines = fullcsv.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var segments = line.Split('\t');
                    int id = int.Parse(segments[0]),behaviourid = int.Parse(segments[2]), mintargets = int.Parse(segments[3]), maxtargets = int.Parse(segments[4]), cooldown = int.Parse(segments[5]);
                    string name = segments[1];
                    if (ItemBehaviours.Behaviours.TryGetValue(behaviourid, out ItemBehaviour b)) {
                        items.Add(id, new Item(id, name, mintargets, maxtargets, cooldown,b));
                        Console.WriteLine("Added item:" + name);
                    }

                }
            }
            
            public static ItemInstance CreateInstance(int id,Player owner)
            {
                if (Items.items.TryGetValue(id, out Item item))
                {
                    ItemInstance ii = new ItemInstance(item, GenerateInstanceId(AssetType.item), owner);
                    owner.AddItem(ii);
                    return ii;
                }
                else
                {
                    Console.WriteLine("Tried create instance, ITEM NOT FOUND");
                }
                return null;
            }
        }
        public static class Interactions
        {

            static ulong ids = 0;
            private static Dictionary<int, InteractionTree> interactions = new Dictionary<int, InteractionTree>();

            public static Dictionary<int, InteractionTree> InteractionSet { get => interactions;  }

            public static void Load(string dir)
            {
                System.IO.DirectoryInfo d = new DirectoryInfo(dir);
                var files = d.GetFiles();
                foreach(var file in files)
                {
                    Console.WriteLine(file.Extension);
                    if(file.Extension == ".idf")
                    {
                        var name = file.Name.Remove(file.Name.Length - 4);
                        if(int.TryParse(name,out int id))
                        {
                            LoadTree(file.FullName,id);
                        }
                    }
                }
                
                
            }
            public static void LoadTree(string dir,int id)
            {
                string fullfile;
                using (FileStream f = new FileStream(dir, FileMode.Open))
                {
                    using (StreamReader r = new StreamReader(f))
                    {
                        fullfile = r.ReadToEnd();
                        fullfile.Trim('\r', '\n');
                        fullfile.Trim('\n', '\r');
                    }
                }
                Console.WriteLine("ID:" + id);
                var lines = fullfile.Split(new string[] { "\r\n" },StringSplitOptions.RemoveEmptyEntries);
                InteractionTree tree = new InteractionTree(lines.Length);
                int nodecount = lines.Length;
                foreach(var line in lines)
                {
                    var n = ParseNode(line,tree);
                    tree.Nodes[n.ID] = n;
                }
                interactions.Add(id, tree);
            }
            static InteractionNode ParseNode(string line,InteractionTree tree)
            {
                line= line.Trim().Trim('\r');
                string[] segments = line.Split(':');
                
                string type = segments[0];
                int id = int.Parse(segments[1]);
                string[] args = new string[segments.Length - 2];
                Array.Copy(segments, 2, args, 0, args.Length);
                InteractionNode ret = null;

                switch (type.ToUpper().Trim())
                {
                    case "C":
                        string[] options = new string[args.Length / 2];
                        string entry = args[0];
                        var nargs = new string[args.Length - 1];
                        Array.Copy(args, 1, nargs, 0, nargs.Length);
                        args = nargs;
                        int[] directs = new int[args.Length / 2];
                        for(int i = 0; i < args.Length; i += 2)
                        {
                            options[i / 2] = args[i];
                            if (args[i + 1] == "E")
                            {
                                directs[i / 2] = -1;
                            }
                            else
                            {
                                directs[i / 2] = int.Parse(args[i + 1]);
                            }
                        }
                        ret = new ConversationNode(entry,options,directs,id,tree);
                        break;
                    case "S":

                        int scriptid =int.Parse(args[0]);
                        ret = new ScriptNode(scriptid, id, -1, tree);
                        break;
                    case "R":
                        ret = new RewardNode(args[0],int.Parse(args[1]), int.Parse(args[2]), args[0], id, int.Parse(args[3]),tree);
                        break;
                    case "T":
                        ret = new TextNode(args[0], id, int.Parse(args[1]), tree);
                        break;
                    case "E":
                        ret = new EndNode(id, -1, tree);
                        break;
                }
                return ret;

            }
            public static InteractionInstance CreateInstance(int id)
            {
                throw new NotImplementedException();
            }
        }
    }
    
    

}
