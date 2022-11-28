using UnityEngine;

namespace Jsonif
{
    
    public static class Json
    {
        public static string ToJson<T>(T v)
        {
            return JsonUtility.ToJson(v);
        }
        public static T FromJson<T>(string s)
        {
            return JsonUtility.FromJson<T>(s);
        }
    }
    
}
