using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Jint.Native.Json;
using Discord;

namespace DiscordRpg
{
    public abstract class Machine
    {
        protected Engine engine;
        protected JSBaseApi api;
        public Machine(JSBaseApi api)
        {
            this.engine = new Engine();
            this.api = api;
        }
        public void Reload()
        {
            engine = new Engine();
            api.Attach(this);
            OnReload();

        }
        protected abstract void OnReload();
        public Engine Engine { get => engine; set => engine = value; }
    }
    public class ISMachine:Machine
    {
        int scriptID;
        string script;
        int nodeid;
        DateTime lastmodified;
        string state;
        InteractionInstance interaction;
        Model model;

        static string header = @"var npstate = JSON.parse('{0}');
var state =JSON.parse('{1}');
var pstate = JSON.parse('{2}');
var gstate = JSON.parse('{3}');";
        static string footer = @"
            function saveStates(){
                return {npstate:JSON.stringify(npstate),state:JSON.stringify(state),pstate:JSON.stringify(pstate),gstate:JSON.stringify(gstate)}
            }
            saveStates();

        ";
        public ISMachine(JSBaseApi api,int nodeid, int scriptId, InteractionInstance interaction):base (api)
        {
            this.nodeid = nodeid;
            this.scriptID = scriptId;           
            this.interaction = interaction;
            script = AssetManager.InteractionScript("assets/ins", scriptID);
            Reload();
            
        }
        
        protected override void OnReload()
        {
            try
            {
                LoadStates();
                engine.SetValue("roomid", interaction.Room.Channel.Id.ToString());
                engine.Execute(script);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error Reloading machine: " + e.Message+" "+e.StackTrace);
            }
        }
        public void LoadStates()
        {
            var npstate = "{}";
            if (!String.IsNullOrEmpty(interaction.Npc.State))
            {
                npstate = interaction.Npc.State;
            }
            var state = "{}";
            if (!String.IsNullOrEmpty(interaction.State))
            {
                state = interaction.State;
            }
            var gstate = "{}";
            if (!String.IsNullOrEmpty(interaction.Room.Guild.State))
            {
                gstate = interaction.Room.Guild.State;
            }
            var pstate = "{}";
            if (!String.IsNullOrEmpty(interaction.Player.State))
            {
                pstate = interaction.Player.State;
            }
            var header = String.Format(ISMachine.header, npstate, state,pstate,gstate);
            try
            {
                engine.Execute(header);
            }
            catch (Jint.Runtime.JavaScriptException e)
            {
                Console.WriteLine("Error Reloading machine (JSRt): " + e.Message+"\nHeader was:"+header);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Reloading machine: " + e.Message+ "\nHeader was:" + header);
            }

        }
        public void SaveStates()
        {
            try
            {
                engine.Execute(footer);
                var comp = engine.GetCompletionValue().AsObject();
                interaction.State = comp.GetProperty("state").Value.AsString();
                interaction.Npc.State = comp.GetProperty("npstate").Value.AsString();
            }
            catch (Jint.Runtime.JavaScriptException e)
            {
                Console.WriteLine("Error Saving machine (JSRt): " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Saving machine: " + e.Message);
            }
        }
        public int GetNext(IMessage message)
        {
            try
            {              
                var ret = engine.Invoke("onInteractMessage", message).AsNumber();
                return (int)ret;
            }
            catch (Jint.Runtime.JavaScriptException e)
            {
                Console.WriteLine("Error Getting Next (JSRt): " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Getting Next: " + e.Message);
            }
            return 0;
        }
        public string GetEntry(Player p)
        {
            try
            {
                var ret = engine.Invoke("getEntry", p).AsString();
                return ret;
            }
            catch (Jint.Runtime.JavaScriptException e)
            {
                Console.WriteLine("Error Getting Entry (JSRt): " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Getting Entry: " + e.Message);
            }
            return "error";

        }
        public bool RequiresInput()
        {
            return engine.Execute("requiresInput()").GetCompletionValue().AsBoolean();
        }
       
        void SendMessage(string message)
        {
            interaction.Room.Channel.SendMessageAsync(message);
        }
    }
    public class JSBaseApi
    {
        Model model;
        
        public JSBaseApi(Model model)
        {
            this.model = model;
        }
        public void Attach(Machine machine)
        {
            machine.Engine.SetValue("SendMessage", new Action<string, string>(SendMessage));
            machine.Engine.SetValue("DamagePlayer", new Action<string, string>(DamagePlayer));
            machine.Engine.SetValue("EditMessage", new Action<string, string>(EditMessage));
            machine.Engine.SetValue("GiveItem", new Action<string, string>(GiveItem));
            machine.Engine.SetValue("GetInventory", new Func<string, string>(GetInventory));
            machine.Engine.SetValue("GetEffects", new Func<string, string>(GetEffects));
        }
        public void SendMessage(string room,string message)
        {
            var channel = (model.Client.GetChannel(ulong.Parse(room)) as ITextChannel);
            if (channel != null)
            {
                channel.SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine("API_MESSAGE_CHANNEL_NOT_FOUND");
            }
        }
        public void DamagePlayer(string id, string amount)
        {

        }
        public void EditMessage(string id, string newmessage)
        {
        }
        public void SpawnNpc(string room, string npc0)
        {

        }
        public void GiveItem(string player, string item)
        {

        }
        public string GetInventory(string id)
        {
            throw new NotImplementedException();
        }
        public string GetEffects(string id)
        {
            throw new NotImplementedException();
        }

    }

    public class ESMachine
    {
        int scriptID;
        string script;
        DateTime lastmodified;
        string state;
        DiscordSocketClient client;
        EffectInstance effect;
        void Subscribe()
        {

        }
        void Unsubscribe()
        {

        }

    }
}
