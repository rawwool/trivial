using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TrivialWPF.Model
{
    public static class DataProvider
    {
        public static async Task<string> GetActionsAsync()
        {
            // ... Target page. 
            string page = "http://localhost:4680/TrivialService/ITravailService/GetFutureActions/100";// "http://localhost:4680/TravailsServiceLibrary/ITravailService/GetFutureActions/100";
            try
            {
                // ... Use HttpClient.
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(page))
                using (HttpContent content = response.Content)
                {
                    // ... Read the string.
                    string result = await content.ReadAsStringAsync();

                    //// ... Display the result.
                    //if (result != null &&
                    //result.Length >= 50)
                    //{
                    //    Console.WriteLine(result.Substring(0, 50) + "...");
                    //}
                    return result;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static async Task ShowAction(TrivialModel.ActionData actionData)
        {
            await ShowAction(actionData.DateLogged.Ticks, actionData.Name);
        }

        internal static async Task ShowAction(long dateInTicks, string title)
        {
            title = Sanitise(title);
            // ... Target page.
            //http://stackoverflow.com/questions/3840762/how-do-you-urlencode-without-using-system-web
            string page = WebUtility.UrlDecode(string.Format("http://localhost:4680/TrivialService/ITravailService/ShowAction/{0}/{1}", 
                dateInTicks.ToString(), (title)));
            try
            {
                // ... Use HttpClient.
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(page))
                using (HttpContent content = response.Content)
                {
                    // ... Read the string.
                    string result = await content.ReadAsStringAsync();

                    //// ... Display the result.
                    //if (result != null &&
                    //result.Length >= 50)
                    //{
                    //    Console.WriteLine(result.Substring(0, 50) + "...");
                    //}
                }
            }
            catch
            {
            }
        }

        private static string Sanitise(string title)
        {
            return title.Replace('/', '֎').Replace('#', 'Φ');
        }
    }
}
