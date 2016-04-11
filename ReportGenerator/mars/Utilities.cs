using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturGeneralInfoParser
{
    public static class Utilities
    {

        /// <summary>
        /// Compresses the object with GZip.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] CompressGZip(byte[] obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            using (MemoryStream ms = new MemoryStream(obj))
            using (MemoryStream mso = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(mso, CompressionMode.Compress))
                    ms.CopyTo(gz);
                return mso.ToArray();
            }
        }
        /// <summary>
        /// Decompresses the object with GZip.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] DecompressGZip(byte[] obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            using (var mso = new MemoryStream())
            {
                using (var ms = new MemoryStream(obj))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                    gz.CopyTo(mso);
                return mso.ToArray();
            }
        }
        /// <summary>
        /// Causes the current thread to sleep for a while.
        /// </summary>
        /// <param name="min">Mininum value of the time.</param>
        /// <param name="max">Maximum value of the time.</param>
        public static void ThreadSleepRandom(int min, int max)
        {
            Random ran = new Random();
            var value = (int)(((double)ran.Next() / (double)int.MaxValue) * (max - min + 1) + min);
            Thread.Sleep(value);
        }
        /// <summary>
        /// Starts the task to sleep for a while.
        /// </summary>
        /// <param name="min">Mininum value of the time.</param>
        /// <param name="max">Maximum value of the time.</param>
        public static Task TaskDelay(int min, int max, CancellationToken token)
        {
            Random ran = new Random();
            var value = (int)(((double)ran.Next() / (double)int.MaxValue) * (max - min + 1) + min);
            return Task.Delay(value, token);
        }
        /// <summary>
        /// Creates a deep copy of the object
        /// (via BinaryFormatter).
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object Clone(object obj)
        {
            IFormatter formatter = new BinaryFormatter();
            object clonedObj;
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                clonedObj = formatter.Deserialize(ms);
            }
            return clonedObj;
        }
        /// <summary>
        /// Creates a deep copy of the object 
        /// (via Newtonsoft.JSON).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T Clone<T>(object obj)
        {
            return Deserialize<T>(Serialize(obj));
        }
        /// <summary>
        /// JSON Serializing with zipping to bytes.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] SerializeWithZip(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] jsonInBytes = Encoding.UTF8.GetBytes(json);
            byte[] jsonZipped = null;
            using (var ms = new MemoryStream(jsonInBytes))
            using (var mso = new MemoryStream())
            {
                using (var gz = new GZipStream(mso, CompressionMode.Compress))
                    ms.CopyTo(gz);
                jsonZipped = mso.ToArray();
            }
            return jsonZipped;
        }
        /// <summary>
        /// JSON Serializing with zipping to stream.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dest"></param>
        public static void SerializeWithZip(object obj, Stream dest)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] jsonInBytes = Encoding.UTF8.GetBytes(json);
            using (MemoryStream ms = new MemoryStream(jsonInBytes))
            using (GZipStream gz = new GZipStream(dest, CompressionMode.Compress))
                ms.CopyTo(gz);
        }
        /// <summary>
        /// JSON Deserializing with zipping from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T DeserializeWithZip<T>(Stream source)
        {
            string json = null;
            using (var mso = new MemoryStream())
            {
                using (var gz = new GZipStream(source, CompressionMode.Decompress))
                    gz.CopyTo(mso);
                json = Encoding.UTF8.GetString(mso.ToArray());
            }
            return JsonConvert.DeserializeObject<T>(json);
        }
        /// <summary>
        /// JSON Deserializing with zipping from bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonInBytes"></param>
        /// <returns></returns>
        public static T DeserializeWithZip<T>(byte[] jsonInBytes)
        {
            string json = null;
            using (var mso = new MemoryStream())
            {
                using (var ms = new MemoryStream(jsonInBytes))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                    gz.CopyTo(mso);
                json = Encoding.UTF8.GetString(mso.ToArray());
            }
            return JsonConvert.DeserializeObject<T>(json);
        }
        /// <summary>
        /// JSON Serializing.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] Serialize(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] jsonInBytes = Encoding.UTF8.GetBytes(json);
            return jsonInBytes;
        }
        /// <summary>
        /// JSON Serializing.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dest"></param>
        public static void Serialize(object obj, Stream dest)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] jsonInBytes = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream(jsonInBytes))
                ms.CopyTo(dest);
        }
        /// <summary>
        /// JSON Deserializing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Deserialize<T>(Stream source)
        {
            string json = null;
            using (var mso = new MemoryStream())
            {
                source.CopyTo(mso);
                json = Encoding.UTF8.GetString(mso.ToArray());
            }
            return JsonConvert.DeserializeObject<T>(json);
        }
        /// <summary>
        /// JSON Deserializing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonInBytes"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] jsonInBytes)
        {
            string json = null;
            using (var ms = new MemoryStream(jsonInBytes))
                json = Encoding.UTF8.GetString(ms.ToArray());
            return JsonConvert.DeserializeObject<T>(json);
        }
        /// <summary>
        /// Kill a process with the specified name.
        /// </summary>
        /// <param name="processname">Process name.</param>
        /// <param name="waitForExit">Wait until the specfied process exits.</param>
        /// <returns></returns>
        public static bool KillProcess(string processname, bool waitForExit = false)
        {
            Process[] processes = Process.GetProcessesByName(
                processname);
            try
            {
                IntPtr handle = Process.GetCurrentProcess().Handle;
                if (processes != null && processes.Length > 0)
                    foreach (Process pr in processes)
                    {
                        if (pr.ProcessName == processname && pr.Handle != handle)
                        {
                            pr.Kill();
                            if (waitForExit)
                                pr.WaitForExit();
                        }
                    }
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Checks process is alive.
        /// </summary>
        /// <param name="processname"></param>
        /// <returns></returns>
        public static bool ProcessIsAlive(string processname)
        {
            Process[] processes = Process.GetProcessesByName(
                processname);
            foreach (Process pr in processes)
            {
                if (pr.ProcessName == processname)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Starts a process with the specified name.
        /// </summary>
        /// <param name="processname">Process name.</param>
        /// <param name="keys"></param>
        public static void StartProcess(string processname, string keys = "")
        {
            try
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(processname);
                proc.StartInfo.FileName = processname;
                proc.StartInfo.Arguments = keys;
                proc.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("К сожалению, произошла непредвиденная ошибка при попытке запустить файл {0}.\nПодробнее: {1}",
                    processname, ex.Message));
                //Debug.Log(string.Format("error StartProcess message: {0}", ex.Message));
            }
        }
        /// <summary>
        /// Adds the data to the event log.
        /// </summary>
        /// <param name="source">Application source name.</param>
        /// <param name="log">Data to write to the event log.</param>
        public static void AddLog(string source, string log)
        {
            try
            {
                // Create an instance of EventLog
                System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog();

                // Check if the event source exists. If not create it.
                if (!System.Diagnostics.EventLog.SourceExists(source))
                {
                    System.Diagnostics.EventLog.CreateEventSource(source, "Application");
                }

                // Set the source name for writing log entries.
                eventLog.Source = source;

                // Create an event ID to add to the event log
                int eventID = 8;

                // Write an entry to the event log.
                eventLog.WriteEntry(log,
                                    System.Diagnostics.EventLogEntryType.Information,
                                    eventID);

                // Close the Event Log
                eventLog.Close();
            }
            catch { }
        }
        /// <summary>
        /// Gets the file binary representation.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public static byte[] ReadAllBytesFromFile(string fileName, int block = 4096)
        {
            FileInfo newfi = new FileInfo(fileName);
            byte[] binaryData = null;
            try
            {
                using (FileStream newfs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (BinaryReader newbr = new BinaryReader(newfs))
                    {
                        int count = (int)(newfi.Length / block);
                        int last = (int)(newfi.Length % block);
                        int index = 0;
                        binaryData = new byte[newfi.Length];
                        for (int i = 0; i < count; i++)
                        {
                            index += newbr.Read(binaryData, index, block);
                        }
                        if (last != 0)
                            newbr.Read(binaryData, index, last);
                    }
                }
            }
            catch (IOException ex)
            {
                //Debug.Log("ReadFileToBinary error message:" + ex.Message);
                return null;
            }
            return binaryData;
        }
        /// <summary>
        /// Opens the folder.
        /// </summary>
        /// <param name="path"></param>
        public static void ViewFolder(string path)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = path;
                p.StartInfo.ErrorDialog = true;
                p.Start();
            }
            catch (Exception exc)
            {
                Console.WriteLine("Ошибка при попытке показать директорию с изображениями! \nПодробнее: " + exc.Message);
                return;
            }
        }        /// <summary>
                 /// Creates a shortcut for the target path.
                 /// </summary>
                 /// <param name="shortcutName"></param>
                 /// <param name="targetPath"></param>
                 /// <param name="iconLocation"></param>
        public static void CreateShortcut(string shortcutName,
            string target,
            string arguments,
            string iconLocation = "shell32.dll, 169")
        {
            //Windows Script Host Shell Object
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));

            object shell = Activator.CreateInstance(t);
            try
            {
                object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                    null, shell, new object[] { shortcutName });
                try
                {
                    t.InvokeMember("TargetPath", BindingFlags.SetProperty,
                        null, lnk, new object[] { target });
                    t.InvokeMember("Arguments", BindingFlags.SetProperty,
                        null, lnk, new object[] { arguments });
                    t.InvokeMember("IconLocation", BindingFlags.SetProperty,
                        null, lnk, new object[] { iconLocation });
                    t.InvokeMember("Save", BindingFlags.InvokeMethod,
                        null, lnk, null);
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
        /// <summary>
        /// Get the embedded resource in bytes.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static byte[] GetEmbRes(string name)
        {
            var s = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            if (s.Length == 0)
                return null;
            byte[] res = null;
            foreach (string r in s)
                if (r.EndsWith(name))
                {
                    using (var mso = new MemoryStream())
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(r))
                    {
                        stream.CopyTo(mso);
                        res = mso.ToArray();
                        break;
                    }
                }
            return res;
        }
        /// <summary>
        /// Encrypt the text by AES algorithm.
        /// </summary>
        /// <param name="plainText">Text to be encrypted</param>
        /// <param name="password">Password to encrypt with</param>
        /// <param name="salt">Salt to encrypt with</param>
        /// <param name="hashAlgorithm">Can be either SHA1 or MD5</param>
        /// <param name="passwordIterations">Number of iterations to do</param>
        /// <param name="initialVector">Needs to be 16 ASCII characters long</param>
        /// <param name="keySize">Can be 128, 192, or 256</param>
        /// <returns>An encrypted string</returns>
        public static string Encrypt(string plainText, string password,
            string salt = "Kosher", string hashAlgorithm = "SHA1",
            int passwordIterations = 2, string initialVector = "OFRna73m*aze01xY",
            int keySize = 256)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            byte[] initialVectorBytes = Encoding.ASCII.GetBytes(initialVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes
             (password, saltValueBytes, hashAlgorithm, passwordIterations);

            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            byte[] cipherTextBytes = null;

            using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor
            (keyBytes, initialVectorBytes))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream
                             (memStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memStream.ToArray();
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }
            symmetricKey.Clear();
            return Convert.ToBase64String(cipherTextBytes);
        }
        /// <summary>
        /// Decrypt the text by AES algorithm.
        /// </summary>
        /// <param name="plainText">Text to be encrypted</param>
        /// <param name="password">Password to encrypt with</param>
        /// <param name="salt">Salt to encrypt with</param>
        /// <param name="hashAlgorithm">Can be either SHA1 or MD5</param>
        /// <param name="passwordIterations">Number of iterations to do</param>
        /// <param name="initialVector">Needs to be 16 ASCII characters long</param>
        /// <param name="keySize">Can be 128, 192, or 256</param>
        /// <returns>Decrypted string</returns>
        public static string Decrypt(string cipherText, string password,
            string salt = "Kosher", string hashAlgorithm = "SHA1",
            int passwordIterations = 2, string initialVector = "OFRna73m*aze01xY",
            int keySize = 256)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";

            byte[] initialVectorBytes = Encoding.ASCII.GetBytes(initialVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes
            (password, saltValueBytes, hashAlgorithm, passwordIterations);
            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int byteCount = 0;

            using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor
                     (keyBytes, initialVectorBytes))
            {
                using (MemoryStream memStream = new MemoryStream(cipherTextBytes))
                {
                    using (CryptoStream cryptoStream
                    = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read))
                    {
                        byteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }

            symmetricKey.Clear();
            return Encoding.UTF8.GetString(plainTextBytes, 0, byteCount);
        }
        /// <summary>
        /// Generate MD5 HashRequest for file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ComputeMD5Hash(object filePath)
        {
            using (var stream = new FileStream((string)filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var md5gen = new MD5CryptoServiceProvider())
                {
                    md5gen.ComputeHash(stream);
                    return BitConverter.ToString(md5gen.Hash).Replace("-", "").ToUpper();
                }
            }
        }
        /// <summary>
        /// Generate SHA1 HashRequest for file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ComputeSHA1Hash(object filePath)
        {
            using (var stream = new FileStream((string)filePath, FileMode.Open, FileAccess.Read))
            {
                using (var sha1gen = new SHA1CryptoServiceProvider())
                {
                    sha1gen.ComputeHash(stream);
                    return BitConverter.ToString(sha1gen.Hash).Replace("-", "").ToUpper();
                }
            }
        }
        public static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
        /// <summary>
        /// Gets execution directory.
        /// </summary>
        /// <returns></returns>
        public static string GetExecuteDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            //return System.IO.Path.GetDirectoryName(
            //System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
        /// <summary>
        /// Gets execution file name.
        /// </summary>
        /// <returns></returns>
        public static string GetExecutionFileName()
        {
            return Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
        }
        /// <summary>
        /// Gets the full execution file path.
        /// </summary>
        /// <returns></returns>
        public static string GetExecutionFilePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }
        /// <summary>
        /// Gets execution file name without extension.
        /// </summary>
        /// <returns></returns>
        public static string GetExecutionFileNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
        }
        /// <summary>
        /// Get a data from INI file.
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpKeyName"></param>
        /// <param name="lpDefault"></param>
        /// <param name="lpReturnString"></param>
        /// <param name="nSize"></param>
        /// <param name="lpFilename"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW",
          SetLastError = true,
          CharSet = CharSet.Unicode, ExactSpelling = true,
          CallingConvention = CallingConvention.StdCall)]
        public static extern int GetPrivateProfileString(
          string lpAppName,
          string lpKeyName,
          string lpDefault,
          string lpReturnString,
          int nSize,
          string lpFilename);

        /// <summary>
        /// Write the data to INI file.
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpKeyName"></param>
        /// <param name="lpString"></param>
        /// <param name="lpFilename"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileStringW",
          SetLastError = true,
          CharSet = CharSet.Unicode, ExactSpelling = true,
          CallingConvention = CallingConvention.StdCall)]
        public static extern int WritePrivateProfileString(
          string lpAppName,
          string lpKeyName,
          string lpString,
          string lpFilename);

        /// <summary>
        /// Gets all the categories from ini file.
        /// </summary>
        /// <param name="iniFile"></param>
        /// <returns></returns>
        public static List<string> GetCategories(string iniFile)
        {
            string returnString = new string(' ', 65536);
            GetPrivateProfileString(null, null, null, returnString, 65536, iniFile);
            List<string> result = new List<string>(returnString.Split('\0'));
            result.RemoveRange(result.Count - 2, 2);
            return result;
        }
        /// <summary>
        /// Gets all the keys for the category from ini file.
        /// </summary>
        /// <param name="iniFile"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public static List<string> GetKeys(string iniFile, string category)
        {
            string returnString = new string(' ', 32768);
            GetPrivateProfileString(category, null, null, returnString, 32768, iniFile);
            List<string> result = new List<string>(returnString.Split('\0'));
            result.RemoveRange(result.Count - 2, 2);
            return result;
        }
        /// <summary>
        /// Gets value of key from ini file.
        /// </summary>
        /// <param name="iniFile"></param>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetIniFileString(string iniFile, string category, string key, string defaultValue)
        {
            string returnString = new string(' ', 1024);
            GetPrivateProfileString(category, key, defaultValue, returnString, 1024, iniFile);
            return returnString.Split('\0')[0];
        }
        public static MemoryStream SerializeToStream(object o)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            return stream;
        }
        public static object DeserializeFromStream(MemoryStream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            object o = formatter.Deserialize(stream);
            return o;
        }
        /// <summary>
        /// Free memory for console.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        /// <summary>
        /// Allocate memory for console.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();
        public static bool IsGuid(string expression)
        {
            bool isGuid = false;
            if (!string.IsNullOrEmpty(expression))
            {
                Regex guidRegEx = new Regex(@"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$");
                isGuid = guidRegEx.IsMatch(expression);
            }
            return isGuid;
        }
        public static void AddToArchive(string archiver, string fileNames, string archiveName)
        {
            try
            {
                // USAGE EXAMPLE:
                // adds all files from folder and subfolders to archive with  max compression level: 
                // [7z a -mx9 -y -r "C:\archive.7z" "c:\dir1\dir2\*"]
                // Предварительные проверки
                if (!File.Exists(archiver))
                    throw new Exception("Архиватор 7z по пути \"" + archiver +
                "\" не найден");

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = archiver;
                startInfo.Arguments = " a -mx9 ";
                startInfo.Arguments += " -y -r ";
                startInfo.Arguments += "\"" + archiveName + "\"";
                startInfo.Arguments += " \"" + fileNames + "\"";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                int sevenZipExitCode = 0;
                using (Process sevenZip = Process.Start(startInfo))
                {
                    sevenZip.WaitForExit();
                    sevenZipExitCode = sevenZip.ExitCode;
                }
                // Если с первого раза не получилось,
                //пробуем еще раз через 1 секунду
                if (sevenZipExitCode != 0 && sevenZipExitCode != 1)
                {
                    Thread.Sleep(1000);
                    using (Process sevenZip = Process.Start(startInfo))
                    {
                        sevenZip.WaitForExit();
                        switch (sevenZip.ExitCode)
                        {
                            case 0: return; // Без ошибок и предупреждений
                            case 1: return; // Есть некритичные предупреждения
                            case 2: throw new Exception("Фатальная ошибка");
                            case 7: throw new Exception("Ошибка в командной строке");
                            case 8:
                                throw new Exception("Недостаточно памяти для выполнения операции");
                            case 225:
                                throw new Exception("Пользователь отменил выполнение операции");
                            default: throw new Exception("Архиватор 7z вернул недокументированный код ошибки: " + sevenZip.ExitCode.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Utilities.AddToArchive: " + e.Message);
            }
        }
        public static void ExtractFromArchive(string archiver, string archiveName, string outputFolder, bool extractWithFullPaths)
        {
            try
            {
                // USAGE EXAMPLE: 
                // extract all files from current folder and its subfolders with full paths, yes to all questions: [7z x -y -r archive.zip -o"C:\Doc"]
                // extract all files from current folder and its subfolders without full paths, yes to all questions:: [7z e -y -r archive.zip -o"C:\Doc"]

                // Предварительные проверки
                if (!File.Exists(archiver))
                    throw new Exception("Архиватор 7z по пути \"" + archiver +
                    "\" не найден");
                if (!File.Exists(archiveName))
                    throw new Exception("Файл архива \"" + archiveName +
                    "\" не найден");
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                // Формируем параметры вызова 7z
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = archiver;

                if (extractWithFullPaths)
                    startInfo.Arguments = " x";
                else
                    startInfo.Arguments = " e";
                startInfo.Arguments += " -y -r";
                startInfo.Arguments += " " + "\"" + archiveName + "\"";
                startInfo.Arguments += " -o" + "\"" + outputFolder + "\"";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                int sevenZipExitCode = 0;
                using (Process sevenZip = Process.Start(startInfo))
                {
                    sevenZip.WaitForExit();
                    sevenZipExitCode = sevenZip.ExitCode;
                }
                // Если с первого раза не получилось,
                //пробуем еще раз через 1 секунду
                if (sevenZipExitCode != 0 && sevenZipExitCode != 1)
                {
                    Thread.Sleep(1000);
                    using (Process sevenZip = Process.Start(startInfo))
                    {
                        sevenZip.WaitForExit();
                        switch (sevenZip.ExitCode)
                        {
                            case 0: return; // Без ошибок и предупреждений
                            case 1: return; // Есть некритичные предупреждения
                            case 2: throw new Exception("Фатальная ошибка");
                            case 7: throw new Exception("Ошибка в командной строке");
                            case 8:
                                throw new Exception("Недостаточно памяти для выполнения операции");
                            case 225:
                                throw new Exception("Пользователь отменил выполнение операции");
                            default: throw new Exception("Архиватор 7z вернул недокументированный код ошибки: " + sevenZip.ExitCode.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Utilities.ExtractFromArchive: " + e.Message);
            }
        }
        /// <summary>
        /// Deletes all files and directories recursively from the specified folder.
        /// </summary>
        /// <param name="dir"></param>
        public static void DeleteAllFromFolder(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            if (!dirInfo.Exists)
                return;
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
            {
                fileInfo.Delete();
            }
            foreach (DirectoryInfo folderInfo in dirInfo.GetDirectories())
            {
                folderInfo.Delete(true);
            }
        }
        /// <summary>
        /// Starts the action in a separate thread with the specified properties.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        /// <returns>Task with long runinng thread.</returns>
        public static Task ActionAsync(Action function,
            bool isBackground = false,
            ThreadPriority priority = ThreadPriority.Normal)
        {
            var tcs = new TaskCompletionSource<object>();
            Thread tr = new Thread(() =>
            {
                try
                {
                    function();
                    tcs.SetResult(null);
                }
                catch (Exception exc)
                {
                    tcs.SetException(exc);
                }
            });
            tr.IsBackground = isBackground;
            tr.Priority = priority;
            tr.Start();
            return tcs.Task;
        }
        /// <summary>
        /// Gets a string from GZip byte array with the specified encoding.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="enc"></param>
        /// <returns></returns>
        public static string GetStringFromGZip(this byte[] bytes, Encoding enc)
        {
            using (MemoryStream mso = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                    gz.CopyTo(mso);
                return enc.GetString(mso.ToArray());
            }
        }
        public static string GetStringFromGZip(this string str, Encoding enc)
        {
            byte[] bytes = enc.GetBytes(str);
            using (MemoryStream mso = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                    gz.CopyTo(mso);
                return enc.GetString(mso.ToArray());
            }
        }
        /// <summary>
        /// Returns a time in unix style.
        /// </summary>
        /// <returns></returns>
        public static double GetUnixTimeStamp()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        /// <summary>
        /// Multiplies a timespan by an integer value.
        /// </summary>
        public static TimeSpan Multiply(this TimeSpan multiplicand, int multiplier)
        {
            return TimeSpan.FromTicks(multiplicand.Ticks * multiplier);
        }
        /// <summary>
        /// Multiplies a timespan by a double value.
        /// </summary>
        public static TimeSpan Multiply(this TimeSpan multiplicand, double multiplier)
        {
            return TimeSpan.FromTicks((long)(multiplicand.Ticks * multiplier));
        }
        /// <summary>
        /// Reads all lines from the file with Read/Write FileShare.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[] ReadAllLinesSafe(String path, Encoding enc)
        {
            using (var csv = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(csv, enc))
            {
                List<string> file = new List<string>();
                while (!sr.EndOfStream)
                {
                    file.Add(sr.ReadLine());
                }

                return file.ToArray();
            }
        }
    }
}
