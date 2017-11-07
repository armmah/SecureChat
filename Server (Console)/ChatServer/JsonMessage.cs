using System.Text;
using Newtonsoft.Json;

namespace ChatServer
{
    #region Serializable JsonMessage
    public class JsonMessage
    {
        #region JsonProperties
        [JsonProperty]
        short state;
        [JsonProperty]
        string text;
        #endregion

        #region enum
        public enum ConnectionState : short
        {
            Null = 0,
            Established = 1,
            Exchanged = 2,
            Communicating = 3,
            Closed = 4
        };
        #endregion

        public short State { get { return state; } }
        public string Message { get { return text; } }
        public JsonMessage(short state, string text) { this.state = state; this.text = text; }


        #region Static Funcs for conversion
        public static byte[] GetBytes(short state, string text)
        { return GetBytes(new JsonMessage(state, text)); }
        public static byte[] GetBytes(JsonMessage jsonMessage)
        { return Encoding.UTF8.GetBytes((JsonConvert.SerializeObject(jsonMessage))); }
        public static JsonMessage GetJsonMessage(byte[] bytes)
        { return JsonConvert.DeserializeObject<JsonMessage>(Encoding.UTF8.GetString(bytes)); }
        public static void GetStateAndText(byte[] bytes, out short state, out string text)
        {
            JsonMessage jm = GetJsonMessage(bytes);
            if (jm == null) { state = (short)ConnectionState.Closed; text = ""; }
            else { state = jm.state; text = jm.text; }
        }
        #endregion
    }
    #endregion
}
