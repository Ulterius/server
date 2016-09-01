#region

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Newtonsoft.Json;

#endregion

namespace UlteriusServer.Utilities
{
    /// <summary>
    ///     Class Settings
    /// </summary>
    public class Settings : DynamicMap
    {
        /// <summary>
        ///     The singleton instance holder for Settings
        /// </summary>
        private static Settings _settings;

        public static bool Empty;
        private static bool _generating;

        /// <summary>
        ///     Gets or sets the file path.
        /// </summary>
        /// <value>The file path.</value>
        public static string FilePath { get; set; }

        /// <summary>
        ///     Gets the entire settings map
        /// </summary>
        /// <returns>dynamic.</returns>
        public static dynamic Get()
        {
            return _settings;
        }

        /// <summary>
        ///     Gets the specified header name.
        /// </summary>
        /// <param name="headerName">Name of the header.</param>
        /// <returns>dynamic.</returns>
        public static dynamic Get(string headerName)
        {
            try
            {
                return _settings[headerName];
            }
            catch (Exception)
            {
    
                if (_generating)
                {
                    _generating = false;
                    return null;
                }
                Console.WriteLine("Header does not exist. Generating new sections file.");
                _generating = true;
                Tools.GenerateSettings();
                var header = Get(headerName);
                _generating = false;
                return header;
            }
        }


        /// <summary>
        ///     Initializes the Settings singleton
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folders">The folder list that the file resides in.</param>
        public static void Initialize(string fileName, params string[] folders)
        {
            FilePath = Path.Combine(AppEnvironment.DataPath, fileName);
            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath, "{}");
            }
            var json = File.ReadAllText(FilePath);
            if (json.Equals("{}"))
            {
                Empty = true;
            }
            _settings = JsonConvert.DeserializeObject<Settings>(json);
        }

        /// <summary>
        ///     Loads the JSON file located at <see cref="FilePath" />
        /// </summary>
        public static void Load()
        {
            _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(FilePath));
        }

        /// <summary>
        ///     Returns settings file as a raw object
        /// </summary>
        /// <returns>object.</returns>
        public static object GetRaw()
        {
          
            //Thanks microsoft
            var settings = JsonConvert.DeserializeObject<IDictionary<string, object>>(File.ReadAllText(FilePath));

            return settings;
        }

        /// <summary>
        ///     Saves the Settings instance to the location contained in <see cref="FilePath" />
        /// </summary>
        public static void Save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(_settings, Formatting.Indented));
        }

        #region Nested type: Header

        /// <summary>
        ///     Class Header
        /// </summary>
        public class Header : DynamicMap
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="Header" /> class.
            /// </summary>
            /// <param name="encryptContents">if set to <c>true</c> [encrypt contents].</param>
            public Header(bool encryptContents)
            {
                Encrypt = encryptContents;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Header" /> class, values are not Encrypted.
            /// </summary>
            public Header()
                : this(false)
            {
            }

            /// <summary>
            ///     Gets a value indicating whether this <see cref="Header" /> uses Encrypted strings.
            /// </summary>
            /// <value><c>true</c> if encrypt; otherwise, <c>false</c>.</value>
            public bool Encrypt { get; private set; }

            /// <summary>
            ///     Adds the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            public new void Add(string key, object value)
            {
                base.Add(key, value);
            }

            /// <summary>
            ///     Overridden dynamic TryGetMember method to retrieve setting values and to decrypt them if need be.
            /// </summary>
            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                object outResult;
                var ret = TryGetValue(binder.Name, out outResult);
                result = outResult;
                return ret;
            }

            /// <summary>
            ///     Overridden dynamic TrySetMember method to set setting values and to encrypt them if need be.
            /// </summary>
            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                this[binder.Name] = value;
                return true;
            }
        }

        #endregion
    }
}