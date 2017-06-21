using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextRuler
{
    public class MSWordReader
    {
        public static List<string> ReadDocument(string documentPath)
        {
            try
            {
                Application word = new Application();
                Document doc = new Document();

                object fileName = documentPath;
                // Define an object to pass to the API for missing parameters
                object missing = System.Type.Missing;
                doc = word.Documents.Open(ref fileName,
                        ref missing, ref missing, ref missing, ref missing,
                        ref missing, ref missing, ref missing, ref missing,
                        ref missing, ref missing, ref missing, ref missing,
                        ref missing, ref missing, ref missing);

                String read = string.Empty;
                List<string> data = new List<string>();
                for (int i = 0; i < doc.Paragraphs.Count; i++)
                {
                    string temp = doc.Paragraphs[i + 1].Range.Text.Trim();
                    if (temp != string.Empty)
                        data.Add(temp);
                }
                ((_Document)doc).Close();
                ((_Application)word).Quit();

                return data;
            }
            catch
            {
                return null;
            }
        }
    }
}
