namespace DropInMultiplayer
{
    public static class HelperExtensions
    {
        public static void SetPrivateFieldValue(this object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(instance, value);
        }
    }
}
