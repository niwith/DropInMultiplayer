namespace DropInMultiplayer
{
    public static class HelperExtensions
    {
        public static void SetPrivateFieldValue(this object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(instance, value);
        }

        public static string ReplaceOnce(this string source, string replacetoken, string replacewith)
        {
            int index = source.IndexOf(replacetoken);
            if (index > -1)
            {
                return source.Substring(0, index) + replacewith + source.Substring(index + replacetoken.Length);
            }
            return source;
        }
    }
}
