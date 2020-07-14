using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace TimedAssembler.Tests.Utils
{
    public class Emulator_MyAPIGateway
    {
        private static Emulator_MyAPIGateway_Utilities _singletonUtilities;
        public static Emulator_MyAPIGateway_Utilities Utilities
        {
            get
            {
                if (_singletonUtilities == null)
                    _singletonUtilities = new Emulator_MyAPIGateway_Utilities();

                return _singletonUtilities;
            }
        }

    }

    public class Emulator_MyAPIGateway_Utilities
    {
        public string SerializeToXML<T>(T data)
        {
            XmlSerializer x = new System.Xml.Serialization.XmlSerializer(data.GetType());
            StringWriter textWriter = new StringWriter();
            x.Serialize(textWriter, data);
            return textWriter.ToString();
        }

        public T SerializeFromXML<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader textReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(textReader))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }

        public bool FileExistsInLocalStorage(string path,Type type)
        {
            return File.Exists(path);
        }

        public TextWriter WriteFileInLocalStorage(string file, Type type)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }

            var path = Path.Combine("./", file);
            var stream = File.Open(path,FileMode.Create, FileAccess.Write, FileShare.Read);
            if (stream != null)
            {
                return new StreamWriter(stream);
            }

            throw new FileNotFoundException();
        }

        public TextReader ReadFileInLocalStorage(string file, Type type)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine("./", file);

            var stream = File.OpenRead(path);
            if (stream != null)
            {
                return new StreamReader(stream);
            }
            throw new FileNotFoundException();
        }

        public void ShowMessage(string sender,string msg)
        {
            Console.WriteLine(sender+": "+msg);
        }
    }
}
