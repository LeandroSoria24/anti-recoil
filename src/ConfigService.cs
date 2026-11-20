using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace RecoilStudio
{
    public static class ConfigService
    {
        public static RecoilConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                RecoilConfig fresh = CreateDefaultConfig();
                SaveAtomic(path, fresh);
                return fresh;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                RecoilConfig loaded = serializer.Deserialize<RecoilConfig>(json);

                if (loaded == null || loaded.profiles == null || loaded.profiles.Count == 0)
                {
                    throw new InvalidDataException("El archivo config.json esta vacio o no contiene perfiles.");
                }

                MigrateConfig(loaded);
                return loaded;
            }
            catch
            {
                try
                {
                    File.Copy(path, path + ".corrupt.bak", true);
                }
                catch
                {
                }

                RecoilConfig fallback = CreateDefaultConfig();
                SaveAtomic(path, fallback);
                return fallback;
            }
        }

        public static void SaveAtomic(string path, RecoilConfig config)
        {
            if (config == null || string.IsNullOrEmpty(path)) return;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            string rawJson = serializer.Serialize(config);
            string formattedJson = FormatJson(rawJson);

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, formattedJson, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tempPath, path, null, true);
                }
                catch
                {
                    File.Copy(tempPath, path, true);
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        public static EngineSnapshot CreateSnapshot(RecoilConfig config)
        {
            if (config == null)
            {
                return new EngineSnapshot(false, "", null, null);
            }

            WeaponProfile active = null;
            if (!string.IsNullOrEmpty(config.active_profile) &&
                config.profiles != null &&
                config.profiles.ContainsKey(config.active_profile))
            {
                active = config.profiles[config.active_profile];
            }

            WeaponProfile cloned = active != null ? active.Clone() : null;
            RecoilStep[] sorted = BuildSortedSteps(cloned);

            return new EngineSnapshot(config.engine_enabled, config.active_profile, cloned, sorted);
        }

        public static RecoilStep[] BuildSortedSteps(WeaponProfile profile)
        {
            if (profile == null || profile.secuencias == null || profile.secuencias.Count == 0)
            {
                return new RecoilStep[0];
            }

            List<RecoilStep> list = new List<RecoilStep>(profile.secuencias.Count);
            for (int i = 0; i < profile.secuencias.Count; i++)
            {
                if (profile.secuencias[i] != null)
                {
                    list.Add(profile.secuencias[i].Clone());
                }
            }

            list.Sort(delegate(RecoilStep a, RecoilStep b)
            {
                return a.tiempo_ms.CompareTo(b.tiempo_ms);
            });

            return list.ToArray();
        }

        public static RecoilConfig CreateDefaultConfig()
        {
            RecoilConfig cfg = new RecoilConfig
            {
                active_profile = "fusil_base",
                engine_enabled = true,
                toggle_key = NativeInput.VK_CAPITAL,
                profile_macro_key = 0,
                macro_profile_a = "",
                macro_profile_b = "subfusil",
                profiles = new Dictionary<string, WeaponProfile>()
            };

            cfg.profiles["fusil_base"] = new WeaponProfile
            {
                patea_arriba = 4.5,
                se_va_lado = -1.5,
                cambia_de_lado = true,
                tiempo_cambio_ms = 400.0,
                nuevo_lado = 2.0,
                suavizado_activo = true,
                humanizacion_fuerza = 1.0,
                secuencias = new List<RecoilStep>()
            };

            cfg.profiles["subfusil"] = new WeaponProfile
            {
                patea_arriba = 3.2,
                se_va_lado = 0.0,
                cambia_de_lado = false,
                tiempo_cambio_ms = 400.0,
                nuevo_lado = 0.0,
                suavizado_activo = true,
                humanizacion_fuerza = 1.0,
                secuencias = new List<RecoilStep>()
            };

            return cfg;
        }

        private static void MigrateConfig(RecoilConfig cfg)
        {
            if (cfg.toggle_key <= 0)
            {
                cfg.toggle_key = NativeInput.VK_CAPITAL;
            }
            if (cfg.macro_profile_a == null) cfg.macro_profile_a = "";
            if (cfg.macro_profile_b == null) cfg.macro_profile_b = "";

            List<string> keys = new List<string>(cfg.profiles.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                WeaponProfile p = cfg.profiles[key];
                if (p == null)
                {
                    p = new WeaponProfile();
                    cfg.profiles[key] = p;
                }

                if (p.humanizacion_fuerza == 0.0 && p.suavizado_activo)
                {
                    p.humanizacion_fuerza = 1.0;
                }
                if (p.secuencias == null)
                {
                    p.secuencias = new List<RecoilStep>();
                }
                if (p.tiempo_cambio_ms < 100.0)
                {
                    p.tiempo_cambio_ms = 400.0;
                }
            }

            if (string.IsNullOrEmpty(cfg.active_profile) || !cfg.profiles.ContainsKey(cfg.active_profile))
            {
                foreach (string k in cfg.profiles.Keys)
                {
                    cfg.active_profile = k;
                    break;
                }
            }
        }

        /// <summary>
        /// Formateador JSON ligero compatible con .NET 4.8 sin dependencias externas.
        /// </summary>
        private static string FormatJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "{}";

            StringBuilder sb = new StringBuilder(json.Length * 2);
            bool inQuotes = false;
            int indent = 0;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];

                if (ch == '"' && !IsEscapedQuote(json, i))
                {
                    inQuotes = !inQuotes;
                    sb.Append(ch);
                    continue;
                }

                if (inQuotes)
                {
                    sb.Append(ch);
                    continue;
                }

                if (ch == '{' || ch == '[')
                {
                    sb.Append(ch);
                    int next = SkipWhitespace(json, i + 1);
                    if (next < json.Length && ((ch == '{' && json[next] == '}') || (ch == '[' && json[next] == ']')))
                    {
                        sb.Append(json[next]);
                        i = next;
                    }
                    else
                    {
                        indent++;
                        sb.AppendLine();
                        AppendIndent(sb, indent);
                    }
                }
                else if (ch == '}' || ch == ']')
                {
                    indent = Math.Max(0, indent - 1);
                    sb.AppendLine();
                    AppendIndent(sb, indent);
                    sb.Append(ch);
                }
                else if (ch == ',')
                {
                    sb.Append(',');
                    sb.AppendLine();
                    AppendIndent(sb, indent);
                }
                else if (ch == ':')
                {
                    sb.Append(": ");
                }
                else if (!char.IsWhiteSpace(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static bool IsEscapedQuote(string text, int index)
        {
            int backslashes = 0;
            for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
            {
                backslashes++;
            }
            return (backslashes % 2) == 1;
        }

        private static int SkipWhitespace(string text, int start)
        {
            int i = start;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }
            return i;
        }

        private static void AppendIndent(StringBuilder sb, int level)
        {
            for (int i = 0; i < level; i++)
            {
                sb.Append("  ");
            }
        }
    }
}
