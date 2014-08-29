using CCM.Volunteer.ApprovalProcess.Core.Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core
{
    public static class Extensions
    {

        public static string ToJson<T>(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T Deserialize<T>(this string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

        public static string APIEncode(this string toEncode)
        {
            if (string.IsNullOrEmpty(toEncode))
                return string.Empty;

            string toReturn = toEncode;
            toReturn = toReturn.Replace("&", "dp_Amp");
            toReturn = toReturn.Replace("=", "dp_Equal");
            toReturn = toReturn.Replace("?", "dp_Qmark");
            return toReturn.Trim();

        }

        public static int APIEncode(this bool toEncode)
        {
            if (toEncode)
                return 1;
            return 0;
        }

        public static string UrlEncode(this string str)
        {
            str = System.Uri.EscapeDataString(str);
            //str = str.Replace("+", "%20");
            //str = str.Replace("!", _strExclamationEncoding);
            //str = str.Replace("*", _strAsterikEncoding);
            //str = str.Replace(".", _strPeriodEncoding);
            //str = str.Replace("'", _strApostropheEncoding);
            return str;
        }

        public static string UrlDecode(this string str)
        {
            return System.Uri.UnescapeDataString(str);
        }

        public static TValue GetAttributeValue<TAttribute, TValue>(
        this Type type,
        Func<TAttribute, TValue> valueSelector)
        where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(
                typeof(TAttribute), true
            ).FirstOrDefault() as TAttribute;
            if (att != null)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }


        public static string Encrypt(this string str, Symmetric.Provider provider, string key)
        {
            Symmetric algorithm = new Symmetric(provider, true);

            Data encryptedData = algorithm.Encrypt(new Data(str), new Data(key));
            return encryptedData.ToBase64();
        }

        public static string Encrypt(this string str, Symmetric.Provider provider)
        {
            return str.Encrypt(provider, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static string Encrypt(this string str)
        {
            return str.Encrypt(Symmetric.Provider.Rijndael, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static string Decrypt(this string str, Symmetric.Provider provider, string key)
        {
            Symmetric algorithm = new Symmetric(provider, true);

            Data encryptedData = new Data();
            encryptedData.Base64 = str;

            return algorithm.Decrypt(encryptedData, new Data(key)).ToString();
        }

        public static string Decrypt(this string str, Symmetric.Provider provider)
        {
            return str.Decrypt(provider, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static string Decrypt(this string str)
        {
            return str.Decrypt(Symmetric.Provider.Rijndael, ConfigurationManager.AppSettings["symmetricKey"]);
        }

        public static string ToBase64(this string str)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(str);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string FromBase64(this string str)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(str);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string ToRelativeDateTime(this DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            double delta = Math.Abs(timeSpan.TotalSeconds);

            if (delta < 60)
            {
                return timeSpan.Seconds == 1 ? "one second ago" : timeSpan.Seconds + " seconds ago";
            }
            if (delta < 120)
            {
                return "a minute ago";
            }
            if (delta < 2700) // 45 * 60
            {
                return timeSpan.Minutes + " minutes ago";
            }
            if (delta < 5400) // 90 * 60
            {
                return "an hour ago";
            }
            if (delta < 86400) // 24 * 60 * 60
            {
                return timeSpan.Hours + " hours ago";
            }
            if (delta < 172800) // 48 * 60 * 60
            {
                return "yesterday";
            }
            if (delta < 2592000) // 30 * 24 * 60 * 60
            {
                return timeSpan.Days + " days ago";
            }
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Round((double)timeSpan.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            int years = Convert.ToInt32(Math.Round((double)timeSpan.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }
}
