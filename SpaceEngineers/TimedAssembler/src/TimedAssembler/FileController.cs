using Nado.Logs;
//using Sandbox.ModAPI;
using MyAPIGateway = TimedAssembler.Tests.Utils.Emulator_MyAPIGateway;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VRage;

namespace TimedAssembler.IO
{
    public sealed class FileController
    {
        private const string Format = "Cfg_{0}.xml";

        public static void SaveFile<T>(string filename, T data)
        {
            try
            {
                FastResourceLock ExecutionLock = new FastResourceLock();

                using (ExecutionLock.AcquireExclusiveUsing())
                {
                    string xml = MyAPIGateway.Utilities.SerializeToXML(data);

                    string fileName;

                    if (!string.IsNullOrEmpty(filename))
                        fileName = string.Format(Format, filename);
                    else
                        fileName = string.Format(Format, "NewCfg");

                    Log.Write("Saving file \"" + fileName + "\"...");

                    TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(T));
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(data));
                    writer.Flush();
                    writer.Close();

                    Log.Write("Done!");
                }
            }
            catch(Exception e)
            {
                Log.Write("There was an error while saving:");
                Log.Write(" - " + e.Message);

                if(e.InnerException != null)
                    Log.Write(" - " + e.InnerException.Message);
            }
        }

        public static T LoadFile<T>(string fileName) where T : class
        {
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(fileName, typeof(T)))
            {
                TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(fileName, typeof(T));
                var text = reader.ReadToEnd();
                reader.Close();

                if (!string.IsNullOrEmpty(text))
                {
                    try
                    {
                        Log.Write("Loading file \"" + fileName + "\"...");

                        return MyAPIGateway.Utilities.SerializeFromXML<T>(text);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(string.Format("An error occuring loading the file '{0}'. Begining with the text \"{1}\". {2}", fileName, text.Substring(0, Math.Min(text.Length, 100)), ex.Message));
                    }
                }
            }

            return null;
        }
    }
}
