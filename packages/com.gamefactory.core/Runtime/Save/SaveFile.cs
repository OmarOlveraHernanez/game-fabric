using System;
using System.IO;
using UnityEngine;

namespace GameFactory.Core.Save
{
    /// <summary>
    /// Guarda y carga un objeto [Serializable] como JSON en Application.persistentDataPath.
    /// - Escritura segura: primero a "nombre.json.tmp" y luego se copia; si la app muere a medias,
    ///   al cargar se recupera desde el .tmp.
    /// - Si el archivo esta corrupto, se respalda como ".corrupt" y se devuelve un objeto nuevo
    ///   (el juego nunca se cae por un guardado danado).
    /// Uso: var file = new SaveFile&lt;MisDatos&gt;("partida"); var d = file.Load(); ... file.Save(d);
    /// </summary>
    public sealed class SaveFile<T> where T : class, new()
    {
        readonly string _path;

        /// <param name="name">Nombre sin extension (solo letras, numeros, guiones).</param>
        /// <param name="directory">Carpeta; por defecto Application.persistentDataPath (util para pruebas).</param>
        public SaveFile(string name, string directory = null)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"Nombre de guardado invalido: '{name}'", nameof(name));
            _path = System.IO.Path.Combine(directory ?? Application.persistentDataPath, name + ".json");
        }

        public string Path => _path;
        string TempPath => _path + ".tmp";
        public bool Exists => File.Exists(_path) || File.Exists(TempPath);

        public T Load()
        {
            T data = TryRead(_path) ?? TryRead(TempPath);
            if (data != null) return data;

            if (File.Exists(_path))
            {
                try { File.Copy(_path, _path + ".corrupt", overwrite: true); } catch (Exception) { /* sin respaldo, no es critico */ }
                Debug.LogWarning($"[SaveFile] Guardado corrupto, se respaldo y se reinicia: {_path}");
            }
            return new T();
        }

        public void Save(T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            string dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(TempPath, JsonUtility.ToJson(data));
            File.Copy(TempPath, _path, overwrite: true);
            File.Delete(TempPath);
        }

        public void Delete()
        {
            if (File.Exists(_path)) File.Delete(_path);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }

        static T TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
