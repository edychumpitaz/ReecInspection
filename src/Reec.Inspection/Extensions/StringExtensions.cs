using System.Text.RegularExpressions;

namespace Reec.Inspection.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Agregar un espacio entre los nombres de cada mayúscula.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string AddSpacesToCamelCase(this string text)
        {
            // Usa una expresión regular para insertar un espacio antes de cada letra mayúscula, excepto la primera.
            return Regex.Replace(text, "(?<!\\s)(?<!^)([A-Z])", " $1");
        }
    }
}
